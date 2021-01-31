open HashUtil.Checksum
open HashUtil.FS
open Microsoft.FSharp.Reflection
open System.CommandLine
open System.CommandLine.Invocation
open System.IO


type Opt(item, tree, includeHiddenFiles, skipEmptyDir, algorithm) =
    // Arguments
    member val items: string[] = item

    // Options
    member val printTree: bool = tree
    member val includeHiddenFiles: bool = includeHiddenFiles
    member val skipEmptyDir: bool = skipEmptyDir
    member val algorithm: string = algorithm


let cmdHandler(opt: Opt) =
    // Parse requested algorithm. System.CommandLine should have already verified.
    let algorithmMaybe = parseHashType opt.algorithm
    assert algorithmMaybe.IsSome
    let hashAlgorithm = algorithmMaybe.Value

    for item in opt.items do
        let optHashStructure =
            makeHashStructure hashAlgorithm opt.includeHiddenFiles (not opt.skipEmptyDir) item

        let strWriter = new StringWriter()

        match optHashStructure with
        | Error e -> printfn "Error: %s" e
        | Ok hashStructure ->
            printHashStructure hashStructure opt.printTree strWriter
            printf "%s" (strWriter.ToString())


[<EntryPoint>]
let main args =
    let root = new RootCommand("A command-line utility to checksum directories and files.")

    // ARGS
    let itemArg = new Argument<string[]>("item", "Directory or file to hash.")
    itemArg.Arity <- ArgumentArity.OneOrMore
    root.AddArgument itemArg

    // OPTIONS
    root.AddOption (Option<bool>([|"-t"; "--tree"|], "Print directory tree."))
    root.AddOption (Option<bool>([|"-i"; "--include-hidden-files"|], "Include hidden files."))
    root.AddOption (Option<bool>([|"-e"; "--skip-empty-dir"|], "Skip empty directories."))
    // Hash Algorithm
    let hashAlgOption = new Option<string>([|"-a"; "--algorithm"|], (fun () -> "sha256"), "The hash function to use.")
    let allHashTypes =
        typeof<HashType>
            |> FSharpType.GetUnionCases
            |> Array.map (fun info -> info.Name.ToLower())
    hashAlgOption.FromAmong(allHashTypes) |> ignore
    root.AddOption hashAlgOption

    root.Handler <- CommandHandler.Create(cmdHandler)
    root.Invoke args
