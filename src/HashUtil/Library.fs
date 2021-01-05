namespace HashUtil

open System.Security.Cryptography
open System.Text
open System.IO


module Checksum =
    let computeHashString (input : string) =
        input
            |> Encoding.ASCII.GetBytes
            |> SHA256.Create().ComputeHash
            |> Seq.map (fun c -> c.ToString("x2"))
            |> Seq.reduce (+)

    let computeHashStringFromFile filePath =
        assert File.Exists filePath
        use file = File.OpenRead filePath
        file
            |> SHA256.Create().ComputeHash
            |> Seq.map (fun c -> c.ToString("x2"))
            |> Seq.reduce (+)


module FS =
    type ItemHash =
        | File of path : string * hash : string
        | Dir of path : string * hash : string * children : List<ItemHash>

    let getHash itemHash =
        match itemHash with
            | File (_, hash) ->
                hash
            | Dir (_, hash, _) ->
                hash

    let rec makeDirHashStructure includeHiddenFiles includeEmptyDir dirPath =
        assert Directory.Exists(dirPath)
        let getResult (x : Result<ItemHash,string>) =
            match x with
                | Ok v -> Some v
                | Error _ -> None
        let children =
            dirPath
                |> Directory.EnumerateFileSystemEntries
                |> Seq.toList
                |> List.sort
                |> List.map (makeHashStructure includeHiddenFiles includeEmptyDir)
                |> List.choose getResult

        if children.IsEmpty && not includeEmptyDir then
            Error("Excluding dir because it is empty")
        else
            let childrenHash =
                children
                    |> List.map getHash
                    |> fun x -> "" :: x // Add empty string as a child to compute hash of empty dir.
                    |> List.reduce (+)
                    |> Checksum.computeHashString
            Ok(Dir(path = dirPath, hash = childrenHash, children = children))

    and makeHashStructure includeHiddenFiles includeEmptyDir path =
        if File.Exists(path) then
            if ((not includeHiddenFiles) &&
                (File.GetAttributes(path) &&& FileAttributes.Hidden).Equals(FileAttributes.Hidden)) then
                Error("Not including hidden file")
            else
                Ok(File(path = path, hash = (Checksum.computeHashStringFromFile path)))
        else if Directory.Exists(path) then
            makeDirHashStructure includeHiddenFiles includeEmptyDir path
        else
            Error("Path is invalid")

    let makeLeftSpacer levels =
        match levels with
            | [] -> ""
            | lastLevelActive :: parentsActive ->
                let parentSpacer =
                    parentsActive
                        |> List.rev
                        |> List.map (fun isActive -> if isActive then "│   " else "    ")
                        |> System.String.Concat

                let curSpacer = if lastLevelActive then "├── " else "└── "
                parentSpacer + curSpacer

    let rec printHashStructureHelper structure (levels:List<bool>) =
        match structure with
            | File (path, hash) ->
                printfn "%s%s %s" (makeLeftSpacer levels) hash (Path.GetFileName path)
            | Dir (path, hash, children) ->
                printfn "%s%s %c%s" (makeLeftSpacer levels) hash
                    Path.DirectorySeparatorChar (DirectoryInfo(path).Name)
                let allButLastChild = List.take (children.Length - 1) children
                let lastChild = List.last children
                for child in allButLastChild do
                    printHashStructureHelper child (true :: levels)
                printHashStructureHelper lastChild (false :: levels)

    let rec printHashStructure structure =
        printHashStructureHelper structure []
