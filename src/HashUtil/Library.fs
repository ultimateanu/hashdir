namespace HashUtil

open Microsoft.FSharp.Reflection
open System
open System.IO
open System.Security.Cryptography
open System.Text

module Checksum =
    type HashType =
        | MD5
        | SHA1
        | SHA256
        | SHA384
        | SHA512

    let allHashTypes : HashType[] =
        typeof<HashType>
        |> FSharpType.GetUnionCases
        |> Array.map(fun info -> FSharpValue.MakeUnion(info,[||]) :?> HashType)

    let parseHashType (input: string) =
        let hashTypeStr = input.ToUpper().Trim()

        match hashTypeStr with
        | "MD5" -> Some MD5
        | "SHA1" -> Some SHA1
        | "SHA256" -> Some SHA256
        | "SHA384" -> Some SHA384
        | "SHA512" -> Some SHA512
        | _ -> None

    let getHashAlgorithm hashType: HashAlgorithm =
        match hashType with
        | MD5 -> upcast MD5.Create()
        | SHA1 -> upcast SHA1.Create()
        | SHA256 -> upcast SHA256.Create()
        | SHA384 -> upcast SHA384.Create()
        | SHA512 -> upcast SHA512.Create()

    let computeHashOfString (hashAlg: HashAlgorithm) (str: string) =
        str
        |> Encoding.ASCII.GetBytes
        |> hashAlg.ComputeHash
        |> Seq.map (fun c -> c.ToString("x2"))
        |> Seq.reduce (+)

    let computeHashOfFile (hashAlg: HashAlgorithm) filePath =
        assert File.Exists filePath
        use file = File.OpenRead filePath

        file
        |> hashAlg.ComputeHash
        |> Seq.map (fun c -> c.ToString("x2"))
        |> Seq.reduce (+)


module FS =
    let private bSpacer = "    "
    let private iSpacer = "│   "
    let private tSpacer = "├── "
    let private lSpacer = "└── "

    type ItemHash =
        | File of path: string * hash: string
        | Dir of path: string * hash: string * children: List<ItemHash>

    let getHash itemHash =
        match itemHash with
        | File (_, hash) -> hash
        | Dir (_, hash, _) -> hash

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


    let rec private makeDirHashStructure (hashAlg: HashAlgorithm) includeHiddenFiles includeEmptyDir dirPath =
        assert Directory.Exists(dirPath)

        let getResult (x: Result<ItemHash, string>) =
            match x with
            | Ok v -> Some v
            | Error _ -> None

        let children =
            dirPath
            |> Directory.EnumerateFileSystemEntries
            |> Seq.toList
            |> List.sort
            |> List.map (makeHashStructureHelper hashAlg includeHiddenFiles includeEmptyDir)
            |> List.choose getResult

        if children.IsEmpty && not includeEmptyDir then
            Error("Excluding dir because it is empty")
        else
            let getNameAndHashString (x: ItemHash): string =
                match x with
                | File (path, hash) -> hash + (Path.GetFileName path)
                | Dir (path, hash, _) -> hash + (Util.getDirName path)

            let childrenHash =
                children
                |> List.map getNameAndHashString
                |> fun x -> "" :: x // Add empty string as a child to compute hash of empty dir.
                |> List.reduce (+)
                |> Checksum.computeHashOfString hashAlg

            Ok(Dir(path = dirPath, hash = childrenHash, children = children))

    and private makeHashStructureHelper (hashAlg: HashAlgorithm) includeHiddenFiles includeEmptyDir path =
        if File.Exists(path) then
            if ((not includeHiddenFiles)
                && (File.GetAttributes(path) &&& FileAttributes.Hidden)
                    .Equals(FileAttributes.Hidden)) then
                Error("Not including hidden file")
            else
                Ok(File(path = path, hash = (Checksum.computeHashOfFile hashAlg path)))
        else if Directory.Exists(path) then
            makeDirHashStructure hashAlg includeHiddenFiles includeEmptyDir path
        else
            Error(sprintf "%s is not a valid path" path)

    let private makeLeftSpacer levels =
        match levels with
        | [] -> ""
        | lastLevelActive :: parentsActive ->
            let parentSpacer =
                parentsActive
                |> List.rev
                |> List.map (fun isActive -> if isActive then iSpacer else bSpacer)
                |> System.String.Concat

            let curSpacer =
                if lastLevelActive then
                    tSpacer
                else
                    lSpacer

            parentSpacer + curSpacer

    let rec private printHashStructureHelper structure printTree levels (outputWriter: TextWriter) =
        match structure with
        | File (path, hash) ->
            let fileLine =
                sprintf "%s%s  %s" (makeLeftSpacer levels) hash (Path.GetFileName path)
            // Append "\n" rather than use WriteLine() to avoid system line endings (e.g. "\r\n")
            outputWriter.Write(sprintf "%s\n" fileLine)
        | Dir (path, hash, children) ->
            let dirLine =
                sprintf "%s%s  %c%s" (makeLeftSpacer levels) hash '/' (DirectoryInfo(path).Name)
            // Append "\n" rather than use WriteLine() to avoid system line endings (e.g. "\r\n")
            outputWriter.Write(sprintf "%s\n" dirLine)

            if printTree && not children.IsEmpty then
                let allButLastChild = List.take (children.Length - 1) children
                let lastChild = List.last children

                for child in allButLastChild do
                    printHashStructureHelper child printTree (true :: levels) outputWriter

                printHashStructureHelper lastChild printTree (false :: levels) outputWriter

    let rec printHashStructure structure printTree outputWriter =
        printHashStructureHelper structure printTree [] outputWriter

    let makeHashStructure (hashType: Checksum.HashType) includeHiddenFiles includeEmptyDir path =
        let hashAlg = Checksum.getHashAlgorithm hashType
        makeHashStructureHelper hashAlg includeHiddenFiles includeEmptyDir path

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


    let verifyHashAndItem (hashType: Checksum.HashType) (basePath:string) (expectedHash:string) (path:string): Result<VerificationResult, string> =
        let fullPath =
            Path.Join(basePath,
                if path.StartsWith('/') then path.[1..] else path)
        // TODO: pass up these options in check also.
        let itemHashResult = makeHashStructure hashType true true fullPath

        match itemHashResult with
            | Error err -> Error err
            | Ok itemHash ->
                let actualHash = getHash itemHash
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

    let printVerificationResults
        (results : seq<Result<VerificationResult,string>>) =

        let printSuccess path =
            Util.printColor ConsoleColor.DarkGreen "MATCHES"
            printfn "%s%s" bSpacer path

        let printDiffer path =
            Util.printColor ConsoleColor.DarkYellow "DIFFERS"
            printfn "%s%s" bSpacer path

        let printError path =
            Util.printColor ConsoleColor.DarkRed "ERROR  "
            printfn "%s%s" bSpacer path

        for result in results do
            match result with
            | Error err -> printError err
            | Ok verificationResult ->
                match verificationResult with
                | Matches(path, hash) -> printSuccess path
                | Differs(path, expectedHash, actualHash) -> printDiffer path
