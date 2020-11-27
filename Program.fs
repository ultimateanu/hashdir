open CommandLine
open System.IO
open System.Security.Cryptography
open System.Text


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


type ItemHash =
    | File of path : string * hash : string
    | Dir of path : string * hash : string * children : seq<ItemHash>


let getHash itemHash =
    match itemHash with
        | File (_, hash) ->
            hash
        | Dir (_, hash, _) ->
            hash


let rec makeDirHashStructure dirPath =
    assert Directory.Exists(dirPath)
    let children =
        dirPath
            |> Directory.EnumerateFileSystemEntries
            |> Seq.choose makeHashStructure
    let childrenHash =
        children
            |> Seq.map getHash
            |> Seq.reduce (+)
            |> computeHashString
    Dir(path = dirPath, hash = childrenHash, children = children)


and makeHashStructure path =
    if File.Exists(path) then
        Some(File(path = path, hash = (computeHashStringFromFile path)))
    else if Directory.Exists(path) then
        Some(makeDirHashStructure path)
    else
        None

let makeLeftSpacer level =
    assert (0 <= level)
    match level with
        | 0 -> ""
        | 1 -> "├── "
        | n ->  (String.replicate (n-1) "│   ") + "├── "

let rec printHashStructureHelper structure level =
    match structure with
        | File (path, hash) ->
            printfn "%s%s %s" (makeLeftSpacer level) hash (Path.GetFileName path)
        | Dir (path, hash, children) ->
            printfn "%s%s %c%s" (makeLeftSpacer level) hash
                Path.DirectorySeparatorChar (DirectoryInfo(path).Name)
            for child in children do
                printHashStructureHelper child (level + 1)


let rec printHashStructure structure =
    printHashStructureHelper structure 0


type Options = {
  [<Option('i', "include-hidden-files", Default = false, HelpText = "Include hidden files.")>] IncludeHiddenFiles : bool;
  [<Value(0, Required = true, MetaName="input", HelpText = "Input directories or files.")>] Input : seq<string>;
}


let run (o : Options)  =
    for item in o.Input do
        let optHashStructure = makeHashStructure item
        match optHashStructure with
            | None -> printfn "%s is not a valid path" item
            | Some(hashStructure) -> printHashStructure hashStructure


[<EntryPoint>]
let main argv =
    let parsedResult = CommandLine.Parser.Default.ParseArguments<Options>(argv)
    match parsedResult with
        | :? Parsed<Options> as parsed -> run parsed.Value
        | _ -> ()

    0