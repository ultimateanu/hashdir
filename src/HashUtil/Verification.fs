namespace HashUtil

open System
open System.IO
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection
open Hashing

module Verification =
    let hashLengths =
        let hashTypesAndLengths =
            Checksum.allHashTypes
            |> Array.map (fun t ->
                (t,
                 Util.computeHashOfString (Checksum.getHashAlgorithm t) "str"))
            |> Array.map (fun (t, hash) -> (t, hash.Length))

        let uniqueLengths =
            hashTypesAndLengths |> Array.map snd |> Array.distinct

        hashTypesAndLengths

    type VerificationResult =
        | Matches of path: string * hash: string
        | Differs of path: string * expectedHash: string * actualHash: string

    type PrintVerbosity =
        | Quiet
        | Normal
        | Detailed

    let allPrintVerbosity: PrintVerbosity[] =
        typeof<PrintVerbosity>
        |> FSharpType.GetUnionCases
        |> Array.map (fun info ->
            FSharpValue.MakeUnion(info, [||]) :?> PrintVerbosity)

    let parsePrintVerbosity (input: string) =
        let printVerbosityStr = input.Trim().ToLower()

        match printVerbosityStr with
        | "quiet" -> Some Quiet
        | "normal" -> Some Normal
        | "detailed" -> Some Detailed
        | _ -> None

    let verifyHashAndItem
        (progressObserver: IObserver<HashingUpdate>)
        (hashType: Checksum.HashType)
        includeHiddenFiles
        includeEmptyDir
        basePath
        expectedHash
        (path: string)
        : Result<VerificationResult, string> =
        let fullPath =
            Path.Join(
                basePath,
                if path.StartsWith('/') then path.[1..] else path
            )

        let itemHashResult =
            Async.RunSynchronously
            <| Hashing.makeHashStructureObservable
                progressObserver
                hashType
                includeHiddenFiles
                includeEmptyDir
                fullPath

        match itemHashResult with
        | Error err -> Error err
        | Ok itemHash ->
            let actualHash = FS.getHash itemHash

            if actualHash = expectedHash then
                Ok(VerificationResult.Matches(path, actualHash))
            else
                Ok(VerificationResult.Differs(path, expectedHash, actualHash))

    let verifyHashAndItemByGuessing
        (progressObserver: IObserver<HashingUpdate>)
        (hashType: Checksum.HashType option)
        includeHiddenFiles
        includeEmptyDir
        (path: string)
        hash
        itemPath
        : Result<VerificationResult, string> =

        let basePath = Path.GetDirectoryName path

        match hashType with
        | Some t ->
            // Use specified hashType
            verifyHashAndItem
                progressObserver
                t
                includeHiddenFiles
                includeEmptyDir
                basePath
                hash
                itemPath
        | None ->
            // Try to guess the hashtype based on hash length
            let matchedHashType =
                hashLengths
                |> Array.filter (fun (_, len) -> len = hash.Length)
                |> Array.map (fun (t, _) -> t)

            if matchedHashType.Length = 1 then
                // Only one HashType matches the hash lengths.
                verifyHashAndItem
                    progressObserver
                    matchedHashType.[0]
                    includeHiddenFiles
                    includeEmptyDir
                    basePath
                    hash
                    itemPath
            else
                // Try to determine HashType based on file extension.
                let m = Regex.Match(path, "\.([a-zA-Z0-9]+?).txt")

                if m.Success then
                    let fileGuess = Checksum.parseHashType m.Groups[1].Value

                    verifyHashAndItem
                        progressObserver
                        fileGuess.Value
                        includeHiddenFiles
                        includeEmptyDir
                        basePath
                        hash
                        itemPath
                else
                    Error("Cannot determine which hash algorithm to use")


    let verifyHashFile
        (progressObserver: IObserver<HashingUpdate>)
        (hashType: Checksum.HashType option)
        includeHiddenFiles
        includeEmptyDir
        origPath
        =

        let verifyValidHashFile (path: string) =
            let baseDirPath = Path.GetDirectoryName path

            let isTopLevelItem (line: string) : bool =
                match line with
                | txt when txt.StartsWith(Common.bSpacer) -> false
                | txt when txt.StartsWith(Common.iSpacer) -> false
                | txt when txt.StartsWith(Common.tSpacer) -> false
                | txt when txt.StartsWith(Common.lSpacer) -> false
                | _ -> true

            let getHashAndItem (line: string) =
                let pieces = line.Split "  "
                assert (pieces.Length = 2)
                (pieces.[0], pieces.[1])

            let topLevelHashes =
                path
                |> File.ReadLines
                |> Seq.filter isTopLevelItem
                |> Seq.map getHashAndItem
                |> List.ofSeq

            let allVerificationResults =
                topLevelHashes
                |> List.map (fun (hash, itemPath) ->
                    verifyHashAndItemByGuessing
                        progressObserver
                        hashType
                        includeHiddenFiles
                        includeEmptyDir
                        path
                        hash
                        itemPath)

            allVerificationResults

        async {
            return
                if File.Exists(origPath) then
                    Ok(verifyValidHashFile origPath)
                else
                    Error(sprintf "'%s' is not a valid hash file" origPath)
        }


    let printVerificationResults
        verbosity
        colorize
        (result: Result<VerificationResult, string>)
        =

        let printSuccess path hash =
            match verbosity with
            | Quiet -> ()
            | Normal ->
                Util.printColor colorize ConsoleColor.Green "MATCHES"
                printfn "%s%s" Common.bSpacer path
            | Detailed ->
                Util.printColor colorize ConsoleColor.Green "MATCHES"
                printfn "%s%s (%s)" Common.bSpacer path hash

        let printDiffer path actualHash expectedHash =
            match verbosity with
            | Quiet -> ()
            | Normal ->
                Util.printColor colorize ConsoleColor.DarkYellow "DIFFERS"
                printfn "%s%s" Common.bSpacer path
            | Detailed ->
                Util.printColor colorize ConsoleColor.DarkYellow "DIFFERS"

                printfn
                    "%s%s (%s, expected: %s)"
                    Common.bSpacer
                    path
                    actualHash
                    expectedHash

        let printError path =
            match verbosity with
            | Quiet -> ()
            | _ ->
                Util.printColor colorize ConsoleColor.Red "ERROR  "
                printfn "%s%s" Common.bSpacer path

        match result with
        | Error err -> printError err
        | Ok verificationResult ->
            match verificationResult with
            | Matches(path, hash) -> printSuccess path hash
            | Differs(path, expectedHash, actualHash) ->
                printDiffer path actualHash expectedHash
