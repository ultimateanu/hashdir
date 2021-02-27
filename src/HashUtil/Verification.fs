namespace HashUtil

open System
open System.IO

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

    let allItemsMatch (results: seq<Result<VerificationResult, string>>) : bool =
        let resultMatches result =
            match result with
                | Ok r ->
                    match r with
                        | VerificationResult.Matches _ -> true
                        | _ -> false
                | Error _ -> false

        Seq.forall resultMatches results

    let verifyHashAndItem (hashType: Checksum.HashType) (basePath:string) (expectedHash:string) (path:string): Result<VerificationResult, string> =
        let fullPath =
            Path.Join(basePath,
                if path.StartsWith('/') then path.[1..] else path)
        // TODO: pass up these options in check also.
        let itemHashResult = FS.makeHashStructure hashType true true fullPath

        match itemHashResult with
            | Error err -> Error err
            | Ok itemHash ->
                let actualHash = FS.getHash itemHash
                if actualHash = expectedHash then
                    Ok (VerificationResult.Matches(path, actualHash))
                else
                    Ok (VerificationResult.Differs(path, expectedHash, actualHash))

    // TODO: change from tuple to seperate args
    let verifyHashAndItemByGuessing (hashType: Checksum.HashType option) (basePath:string) (hashAndItem:string * string): Result<VerificationResult, string> =
        match hashType with
        | Some t ->
            // Use specified hashType
            verifyHashAndItem t basePath (fst hashAndItem) (snd hashAndItem)
        | None ->
            // Try to guess the hashtype based on hash length
            let matchedHashType =
                hashLengths
                |> Array.filter(fun (t,len) -> len = fst(hashAndItem).Length)
            assert (matchedHashType.Length <= 1)
            if matchedHashType.Length = 1 then
                verifyHashAndItem (fst matchedHashType.[0]) basePath (fst hashAndItem) (snd hashAndItem)
            else
                Error("Cannot determine which hash algorithm to use")


    let verifyHashFile (hashType: Checksum.HashType option) (path:string) : Result<seq<Result<VerificationResult,string>>, string> =
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
                |> Seq.map (verifyHashAndItemByGuessing hashType baseDirPath)
            Ok allVerificationResults
        else
            Error(sprintf "'%s' is not a valid hash file" path)


    let printVerificationResults //results =
        (results : seq<Result<VerificationResult,string>>) =

        let printSuccess path =
            Util.printColor ConsoleColor.Green "MATCHES"
            printfn "%s%s" bSpacer path

        let printDiffer path =
            Util.printColor ConsoleColor.DarkYellow "DIFFERS"
            printfn "%s%s" bSpacer path

        let printError path =
            Util.printColor ConsoleColor.Red "ERROR  "
            printfn "%s%s" bSpacer path

        for result in results do
            match result with
            | Error err -> printError err
            | Ok verificationResult ->
                match verificationResult with
                | Matches(path, hash) -> printSuccess path
                | Differs(path, expectedHash, actualHash) -> printDiffer path
