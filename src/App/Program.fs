open HashUtil.Checksum
open HashUtil.FS
open Microsoft.FSharp.Reflection
open System.CommandLine
open System.CommandLine.Invocation
open System.IO


type Opt(item, tree, includeHiddenFiles, skipEmptyDir, algorithm) =
    // Arguments
    member val Items: string [] = item

    // Options
    member val PrintTree: bool = tree
    member val IncludeHiddenFiles: bool = includeHiddenFiles
    member val SkipEmptyDir: bool = skipEmptyDir
    member val Algorithm: string = algorithm


let cmdHandler (opt: Opt) =
    // Parse requested algorithm. System.CommandLine should have already verified.
    let algorithmMaybe = parseHashType opt.Algorithm
    assert algorithmMaybe.IsSome
    let hashAlgorithm = algorithmMaybe.Value

    for item in opt.Items do
        let optHashStructure =
            makeHashStructure hashAlgorithm opt.IncludeHiddenFiles (not opt.SkipEmptyDir) item

        let strWriter = new StringWriter()

        match optHashStructure with
        | Error e -> printfn "Error: %s" e
        | Ok hashStructure ->
            printHashStructure hashStructure opt.PrintTree strWriter
            printf "%s" (strWriter.ToString())


[<EntryPoint>]
let main args =
    let root =
        RootCommand("A command-line utility to checksum directories and files.")

    // ARGS
    let itemArg =
        Argument<string []>("item", "Directory or file to hash.")

    itemArg.Arity <- ArgumentArity.OneOrMore
    root.AddArgument itemArg

    // OPTIONS
    root.AddOption(Option<bool>([| "-t"; "--tree" |], "Print directory tree."))
    root.AddOption(Option<bool>([| "-i"; "--include-hidden-files" |], "Include hidden files."))
    root.AddOption(Option<bool>([| "-e"; "--skip-empty-dir" |], "Skip empty directories."))
    // Hash Algorithm
    let hashAlgOption =
        Option<string>([| "-a"; "--algorithm" |], (fun () -> "sha1"), "The hash function to use.")

    let allHashTypes =
        typeof<HashType>
        |> FSharpType.GetUnionCases
        |> Array.map (fun info -> info.Name.ToLower())

    hashAlgOption.FromAmong(allHashTypes) |> ignore
    root.AddOption hashAlgOption

    root.Handler <- CommandHandler.Create(cmdHandler)
    root.Invoke args
