open HashUtil.Checksum
open HashUtil.FS
open HashUtil.Util
open Microsoft.FSharp.Reflection
open System.CommandLine
open System.CommandLine.Invocation
open System.Diagnostics
open System.IO


type PrintVerbosity =
    | Quiet
    | Normal
    | Detailed


let allPrintVerbosity : PrintVerbosity[] =
    typeof<PrintVerbosity>
    |> FSharpType.GetUnionCases
    |> Array.map(fun info -> FSharpValue.MakeUnion(info,[||]) :?> PrintVerbosity)


type RootOpt(item, tree, includeHiddenFiles, skipEmptyDir, algorithm) =
    // Arguments
    member val Items: string [] =
        match item with
            | null -> Debug.Assert(false, "Root command not given item(s)") ; [||]
            | _ -> item

    // Options
    member val PrintTree: bool = tree
    member val IncludeHiddenFiles: bool = includeHiddenFiles
    member val SkipEmptyDir: bool = skipEmptyDir
    member val Algorithm: string = algorithm

    override x.ToString() =
        sprintf "RootOpt[Items:%A PrintTree:%A IncludeHiddenFiles:%A SkipEmptyDir:%A Algorithm:%A]"
            x.Items x.PrintTree x.IncludeHiddenFiles x.SkipEmptyDir x.Algorithm


type CheckOpt(item, includeHiddenFiles, skipEmptyDir, algorithm, verbosity) =
    // Arguments
    member val Items: string [] =
        match item with
            | null -> Debug.Assert(false, "Check command not given item(s)") ; [||]
            | _ -> item

    // Options
    member val IncludeHiddenFiles: bool = includeHiddenFiles
    member val SkipEmptyDir: bool = skipEmptyDir
    member val Algorithm: string = algorithm
    member val Verbosity: string = verbosity


    override x.ToString() =
        sprintf "VerifyOpt[Items:%A IncludeHiddenFiles:%A SkipEmptyDir:%A Algorithm:%A]"
            x.Items x.IncludeHiddenFiles x.SkipEmptyDir x.Algorithm


let rootHandler (opt: RootOpt) =
    // Parse requested algorithm. System.CommandLine should have already verified.
    let algorithmMaybe = parseHashType opt.Algorithm
    assert algorithmMaybe.IsSome
    let hashAlgorithm = algorithmMaybe.Value

    for item in opt.Items do
        let optHashStructure =
            makeHashStructure hashAlgorithm opt.IncludeHiddenFiles (not opt.SkipEmptyDir) item

        let strWriter = new StringWriter()

        match optHashStructure with
        | Error err -> printfn "Error: %s" err
        | Ok hashStructure ->
            printHashStructure hashStructure opt.PrintTree strWriter
            printf "%s" (strWriter.ToString())


let checkHandler (opt: CheckOpt) =
    let algorithm =
        match opt.Algorithm with
        | null -> None
        | str ->
            let algorithmMaybe = parseHashType str
            assert algorithmMaybe.IsSome
            Some algorithmMaybe.Value

    let mutable allMatches = true
    for item in opt.Items do
        let verifyResult = verifyHashFile algorithm item
        let strWriter = new StringWriter()
        match verifyResult with
        | Error err ->
            allMatches <- false
            printfn "Error: %s" err
            exit 1
        | Ok itemResults ->
            if not (allItemsMatch itemResults) then allMatches <- false
            printVerificationResults itemResults

    // Return error code 2, if anything is different than expected hash.
    if not allMatches then
        exit 2


let itemArg =
    let arg =
        Argument<string []>("item", "Directory or file to hash/check")
    arg.Arity <- ArgumentArity.OneOrMore
    arg


let algorithmOpt forCheck =
    let hashAlgOption =
        match forCheck with
        | true ->
            Option<string>([| "-a"; "--algorithm" |],
                "The hash function to use. If unspecified, will try to " +
                "use the appropriate function based on hash length")
        | false ->
            Option<string>([| "-a"; "--algorithm" |], (fun () -> "sha1"),
                "The hash function to use")

    let allHashTypesStr =
        allHashTypes |> Array.map toStrLower
    hashAlgOption.FromAmong(allHashTypesStr) |> ignore
    hashAlgOption

let hiddenFilesOpt =
    Option<bool>([| "-i"; "--include-hidden-files" |], "Include hidden files")

let skipEmptyOpt =
    Option<bool>([| "-e"; "--skip-empty-dir" |], "Skip empty directories")

let verbosityOpt =
    let opt =
        Option<string>(
            [| "-v"; "--verbosity" |],
            (fun () -> toStrLower PrintVerbosity.Normal),
            "Sets the verbosity level for the output")

    opt.FromAmong(allPrintVerbosity |> Array.map toStrLower) |> ignore
    opt

let verifyCmd =
    let verifyCmd = Command("check", "Verify that the specified hash is valid for the corresponding items")

    // ARGS
    verifyCmd.AddArgument itemArg

    // OPTIONS
    verifyCmd.AddOption hiddenFilesOpt
    verifyCmd.AddOption skipEmptyOpt
    verifyCmd.AddOption (algorithmOpt true)
    verifyCmd.AddOption verbosityOpt
    verifyCmd.Handler <- CommandHandler.Create(checkHandler)

    verifyCmd

let rootCmd =
    let root =
        RootCommand("A command-line utility to checksum directories and files.")

    // Verify Command
    root.AddCommand verifyCmd

    // ARGS
    root.AddArgument itemArg

    // OPTIONS
    root.AddOption(Option<bool>([| "-t"; "--tree" |], "Print directory tree"))
    root.AddOption hiddenFilesOpt
    root.AddOption skipEmptyOpt
    root.AddOption (algorithmOpt false)

    root.Handler <- CommandHandler.Create(rootHandler)
    root


[<EntryPoint>]
let main args =
    rootCmd.Invoke args
