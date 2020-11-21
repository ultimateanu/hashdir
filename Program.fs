open System
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


[<EntryPoint>]
let main argv =
    for arg in argv do
        let hashStructure = makeHashStructure arg
        printfn "%A" hashStructure

    0