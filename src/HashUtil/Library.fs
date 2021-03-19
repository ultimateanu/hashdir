namespace HashUtil

open System.IO
open System.Security.Cryptography
open System.Text.RegularExpressions
open System

module FS =
    type ItemHash =
        | File of path: string * hash: string
        | Dir of path: string * hash: string * children: List<ItemHash>

    let getPath itemHash =
        match itemHash with
        | File (path, _) -> path
        | Dir (path, _, _) -> path

    let getHash itemHash =
        match itemHash with
        | File (_, hash) -> hash
        | Dir (_, hash, _) -> hash

    let rec private makeDirHashStructure
        (hashAlg: HashAlgorithm)
        includeHiddenFiles
        includeEmptyDir
        dirPath
        =
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
            |> List.map (
                makeHashStructureHelper
                    hashAlg
                    includeHiddenFiles
                    includeEmptyDir
            )
            |> List.choose getResult

        if children.IsEmpty && not includeEmptyDir then
            Error("Excluding dir because it is empty")
        else
            let getNameAndHashString (x: ItemHash) : string =
                match x with
                | File (path, hash) -> hash + (Path.GetFileName path)
                | Dir (path, hash, _) -> hash + (Util.getChildName path)

            let childrenHash =
                children
                |> List.map getNameAndHashString
                |> fun x -> "" :: x // Add empty string as a child to compute hash of empty dir.
                |> List.reduce (+)
                |> Checksum.computeHashOfString hashAlg

            Ok(Dir(path = dirPath, hash = childrenHash, children = children))

    and private makeHashStructureHelper
        (hashAlg: HashAlgorithm)
        includeHiddenFiles
        includeEmptyDir
        path
        =
        if File.Exists(path) then
            if ((not includeHiddenFiles)
                && (File.GetAttributes(path) &&& FileAttributes.Hidden)
                    .Equals(FileAttributes.Hidden)) then
                Error("Not including hidden file")
            else
                Ok(
                    File(
                        path = path,
                        hash = (Checksum.computeHashOfFile hashAlg path)
                    )
                )
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
                |> List.map
                    (fun isActive ->
                        if isActive then
                            Common.iSpacer
                        else
                            Common.bSpacer)
                |> System.String.Concat

            let curSpacer =
                if lastLevelActive then
                    Common.tSpacer
                else
                    Common.lSpacer

            parentSpacer + curSpacer

    let rec private printHashStructureHelper
        structure
        printTree
        levels
        (outputWriter: TextWriter)
        =
        match structure with
        | File (path, hash) ->
            let fileLine =
                sprintf
                    "%s%s  %s"
                    (makeLeftSpacer levels)
                    hash
                    (Path.GetFileName path)
            // Append "\n" rather than use WriteLine() to avoid system line endings (e.g. "\r\n")
            outputWriter.Write(sprintf "%s\n" fileLine)
        | Dir (path, hash, children) ->
            let dirLine =
                sprintf
                    "%s%s  %c%s"
                    (makeLeftSpacer levels)
                    hash
                    '/'
                    (DirectoryInfo(path).Name)
            // Append "\n" rather than use WriteLine() to avoid system line endings (e.g. "\r\n")
            outputWriter.Write(sprintf "%s\n" dirLine)

            if printTree && not children.IsEmpty then
                let allButLastChild = List.take (children.Length - 1) children
                let lastChild = List.last children

                for child in allButLastChild do
                    printHashStructureHelper
                        child
                        printTree
                        (true :: levels)
                        outputWriter

                printHashStructureHelper
                    lastChild
                    printTree
                    (false :: levels)
                    outputWriter

    let rec printHashStructure structure printTree outputWriter =
        printHashStructureHelper structure printTree [] outputWriter

    let makeHashStructure
        (hashType: Checksum.HashType)
        includeHiddenFiles
        includeEmptyDir
        path
        =
        let hashAlg = Checksum.getHashAlgorithm hashType
        makeHashStructureHelper hashAlg includeHiddenFiles includeEmptyDir path

    let saveHashStructure structure printTree hashAlgorithm =
        let hashAlgName = hashAlgorithm.ToString().ToLower()
        let itemPath = getPath structure
        let parentDir = Directory.GetParent(itemPath).FullName
        let child = Util.getChildName itemPath

        let searchPattern = sprintf "%s.*.%s.txt" child hashAlgName
        let matchPattern = sprintf "%s\.(\d+)\.%s\.txt" child hashAlgName

        // Find largest existing id.
        let extractVersionNum hashFilePath =
            match hashFilePath with
                | Util.Regex matchPattern [ number ] ->
                    match Int32.TryParse number with
                        | true, out -> Some out
                        | false, _ -> None
                | _ ->
                    None
        let largestId =
            Directory.GetFiles(parentDir, searchPattern)
                |> Array.choose extractVersionNum
                |> Array.append [|0|]   // Ensures we have id of at least 0.
                |> Array.max

        // Write to next id.
        let newVersion = largestId + 1
        let hashFilePath =
            sprintf "%s.%d.%s.txt" itemPath newVersion (hashAlgorithm.ToString().ToLower())
        use fileStream = new StreamWriter(hashFilePath)
        printHashStructure structure printTree fileStream
        fileStream.Flush()
