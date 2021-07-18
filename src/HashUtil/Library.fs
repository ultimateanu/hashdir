namespace HashUtil

open System
open System.IO

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

    let private makeLeftSpacer levels =
        match levels with
        | [] -> ""
        | lastLevelActive :: parentsActive ->
            let isActive x =
                if x then
                    Common.iSpacer
                else
                    Common.bSpacer

            let parentSpacer =
                parentsActive
                |> List.rev
                |> List.map isActive
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
                    "%s%s  %s\n"
                    (makeLeftSpacer levels)
                    hash
                    (Path.GetFileName path)
            // Use "\n" rather than use WriteLine() to avoid system line endings (e.g. "\r\n")
            outputWriter.Write(fileLine)

        | Dir (path, hash, children) ->
            // Print dir line, with optional colors.
            // TODO: make the colors optional via cmd line flag
            Util.printColorToWriter
                None
                (makeLeftSpacer levels)
                outputWriter

            Util.printColorToWriter (None) hash outputWriter
            Util.printColorToWriter (Some ConsoleColor.Cyan) "  /" outputWriter

            Util.printColorToWriter
                (Some ConsoleColor.Cyan)
                (DirectoryInfo(path).Name)
                outputWriter
            // Append "\n" rather than use WriteLine() to avoid system line endings (e.g. "\r\n")
            Util.printColorToWriter (None) "\n" outputWriter

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

    let saveHashStructure structure printTree hashAlgorithm =
        let hashAlgName = hashAlgorithm.ToString().ToLower()
        let itemPath = getPath structure
        let parentDir = Directory.GetParent(itemPath).FullName
        let child = Util.getChildName itemPath

        let searchPattern = sprintf "%s.*.%s.txt" child hashAlgName

        let matchPattern =
            sprintf "%s\.(\d+)\.%s\.txt" child hashAlgName

        // Find largest existing id.
        let extractVersionNum hashFilePath =
            match hashFilePath with
            | Util.Regex matchPattern [ number ] ->
                match Int32.TryParse number with
                | true, out -> Some out
                | false, _ -> None
            | _ -> None

        let largestId =
            Directory.GetFiles(parentDir, searchPattern)
            |> Array.choose extractVersionNum
            |> Array.append [| 0 |]
            |> Array.max

        // Write to next id.
        let newVersion = largestId + 1

        let hashFilePath =
            sprintf
                "%s.%d.%s.txt"
                itemPath
                newVersion
                (hashAlgorithm.ToString().ToLower())

        use fileStream = new StreamWriter(hashFilePath)
        printHashStructure structure printTree fileStream
        fileStream.Flush()
