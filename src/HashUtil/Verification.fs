namespace HashUtil

open System
open System.IO
open Microsoft.FSharp.Reflection

module Verification =
    // TODO: put in common place
    let private bSpacer = "    "
    let private iSpacer = "│   "
    let private tSpacer = "├── "
    let private lSpacer = "└── "

    let hashLengths =
        let hashTypesAndLengths =
            Checksum.allHashTypes
            |> Array.map(fun t ->
                (t,Checksum.computeHashOfString (Checksum.getHashAlgorithm t) "str"))
            |> Array.map(fun (t,hash) -> (t,hash.Length))
        let uniqueLengths =
            hashTypesAndLengths
            |> Array.map snd
            |> Array.distinct
        assert (uniqueLengths.Length = Checksum.allHashTypes.Length)
        hashTypesAndLengths

    type VerificationResult =
        | Matches of path: string * hash: string
        | Differs of path: string * expectedHash: string * actualHash: string

    type PrintVerbosity =
        | Quiet
        | Normal
        | Detailed

    let allPrintVerbosity : PrintVerbosity[] =
        typeof<PrintVerbosity>
        |> FSharpType.GetUnionCases
        |> Array.map(fun info -> FSharpValue.MakeUnion(info,[||]) :?> PrintVerbosity)

    let parsePrintVerbosity (input: string) =
        let printVerbosityStr = input.Trim().ToLower()

        match printVerbosityStr with
        | "quiet" -> Some Quiet
        | "normal" -> Some Normal
        | "detailed" -> Some Detailed
        | _ -> None

    let allItemsMatch (results: seq<Result<VerificationResult, string>>) =
        let resultMatches result =
            match result with
                | Ok r ->
                    match r with
                        | VerificationResult.Matches _ -> true
                        | _ -> false
                | Error _ -> false

        Seq.forall resultMatches results

    let verifyHashAndItem (hashType: Checksum.HashType) includeHiddenFiles
        includeEmptyDir basePath expectedHash (path:string): Result<VerificationResult, string> =
        let fullPath =
            Path.Join(basePath,
                if path.StartsWith('/') then path.[1..] else path)
        let itemHashResult = FS.makeHashStructure hashType includeHiddenFiles includeEmptyDir fullPath

        match itemHashResult with
            | Error err -> Error err
            | Ok itemHash ->
                let actualHash = FS.getHash itemHash
                if actualHash = expectedHash then
                    Ok (VerificationResult.Matches(path, actualHash))
                else
                    Ok (VerificationResult.Differs(path, expectedHash, actualHash))

    // TODO: change from tuple to seperate args
    let verifyHashAndItemByGuessing (hashType: Checksum.HashType option) includeHiddenFiles
        includeEmptyDir basePath (hashAndItem:string * string): Result<VerificationResult, string> =
        match hashType with
        | Some t ->
            // Use specified hashType
            verifyHashAndItem t includeHiddenFiles includeEmptyDir basePath (fst hashAndItem) (snd hashAndItem)
        | None ->
            // Try to guess the hashtype based on hash length
            let matchedHashType =
                hashLengths
                |> Array.filter(fun (t,len) -> len = fst(hashAndItem).Length)
            assert (matchedHashType.Length <= 1)
            if matchedHashType.Length = 1 then
                verifyHashAndItem (fst matchedHashType.[0]) includeHiddenFiles includeEmptyDir basePath (fst hashAndItem) (snd hashAndItem)
            else
                Error("Cannot determine which hash algorithm to use")


    let verifyHashFile (hashType: Checksum.HashType option)
        includeHiddenFiles
        includeEmptyDir
        path : Result<seq<Result<VerificationResult,string>>, string> =
        if File.Exists(path) then
            let baseDirPath = Path.GetDirectoryName path

            let isTopLevelItem (line:string): bool =
                match line with
                | txt when txt.StartsWith(bSpacer) -> false
                | txt when txt.StartsWith(iSpacer) -> false
                | txt when txt.StartsWith(tSpacer) -> false
                | txt when txt.StartsWith(lSpacer) -> false
                | _ -> true

            let getHashAndItem (line:string) =
                let pieces = line.Split "  "
                assert (pieces.Length = 2)
                (pieces.[0], pieces.[1])

            let topLevelHashes =
                path
                |> File.ReadLines
                |> Seq.filter isTopLevelItem
                |> Seq.map getHashAndItem

            let allVerificationResults =
                topLevelHashes
                |> Seq.map (verifyHashAndItemByGuessing hashType includeHiddenFiles includeEmptyDir baseDirPath)
            Ok allVerificationResults
        else
            Error(sprintf "'%s' is not a valid hash file" path)


    let printVerificationResults
        verbosity
        (results : seq<Result<VerificationResult,string>>) =

        let printSuccess path hash =
            match verbosity with
                | Quiet -> ()
                | Normal ->
                    Util.printColor ConsoleColor.Green "MATCHES"
                    printfn "%s%s" bSpacer path
                | Detailed ->
                    Util.printColor ConsoleColor.Green "MATCHES"
                    printfn "%s%s (%s)" bSpacer path hash

        let printDiffer path actualHash expectedHash =
            match verbosity with
            | Quiet -> ()
            | Normal ->
                Util.printColor ConsoleColor.DarkYellow "DIFFERS"
                printfn "%s%s" bSpacer path
            | Detailed ->
                Util.printColor ConsoleColor.DarkYellow "DIFFERS"
                printfn "%s%s (%s, expected: %s)" bSpacer path actualHash expectedHash

        let printError path =
            match verbosity with
            | Quiet -> ()
            | _ ->
                Util.printColor ConsoleColor.Red "ERROR  "
                printfn "%s%s" bSpacer path

        for result in results do
            match result with
            | Error err -> printError err
            | Ok verificationResult ->
                match verificationResult with
                | Matches(path, hash) -> printSuccess path hash
                | Differs(path, expectedHash, actualHash) -> printDiffer path actualHash expectedHash
