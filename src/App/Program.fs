open HashUtil.Checksum
open HashUtil.FS
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
    printfn "In handler()"
    printfn "arg Name:%A" opt.items
    printfn "option --printTree:%A" opt.printTree
    printfn "option --includeHiddenFiles:%A" opt.includeHiddenFiles
    printfn "option --skipEmptyDir:%A" opt.skipEmptyDir
    printfn "option --algorithm:%A" opt.algorithm

    for item in opt.items do
        let optHashStructure =
            makeHashStructure SHA256 opt.includeHiddenFiles (not opt.skipEmptyDir) item

        let strWriter = new StringWriter()

        match optHashStructure with
        | Error e -> printfn "Error: %s" e
        | Ok hashStructure ->
            printHashStructure hashStructure opt.printTree strWriter
            printf "%s" (strWriter.ToString())


[<EntryPoint>]
let main args =
    let root = new RootCommand()

    // File or dir arguments
    let itemArg = new Argument<string[]>("item", "Directory or file to hash.")
    itemArg.Arity <- ArgumentArity.OneOrMore
    root.AddArgument itemArg

    // Options
    root.AddOption (Option<bool>([|"-t"; "--tree"|], "Print directory tree."))
    root.AddOption (Option<bool>([|"-i"; "--include-hidden-files"|], "Include hidden files."))
    root.AddOption (Option<bool>([|"-e"; "--skip-empty-dir"|], "Skip empty directories."))
    // Hash Algorithm Option
    let hashAlgOption = new Option<string>([|"-a"; "--algorithm"|], (fun () -> "sha256"), "The hash function to use.")
    //let b = new Option<string>()
    hashAlgOption.FromAmong([|"md5"; "sha1"; "sha256"|]) |> ignore
    root.AddOption hashAlgOption

    root.Handler <- CommandHandler.Create(cmdHandler)
    root.Invoke args
