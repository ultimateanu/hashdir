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

type VerifyOpt(item, algorithm) =
    // Arguments
    member val Items: string [] = item

    // Options
    member val Algorithm: string = algorithm

let private allHashTypesStr = allHashTypes |> Array.map(fun hashType -> hashType.ToString().ToLower())

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
        | Error err -> printfn "Error: %s" err
        | Ok hashStructure ->
            printHashStructure hashStructure opt.PrintTree strWriter
            printf "%s" (strWriter.ToString())


let verifyCmdHandler (opt: VerifyOpt) =
    printfn "verifyCmdHandler items:%A" (opt.Items)

    let algorithm =
        match opt.Algorithm with
        | null -> None
        | str ->
            let algorithmMaybe = parseHashType str
            assert algorithmMaybe.IsSome
            Some algorithmMaybe.Value
    printfn "verifyCmdHandler algF:%A" (algorithm)

    for item in opt.Items do
        let verifyResult = verifyHashFile algorithm item

        match verifyResult with
        | Error err -> printfn "Error: %s" err
        | Ok matches -> printf "Match: %A" matches


let verifyCmd =
    let verifyCmd = Command("check", "Verify that the specified hash is valid for the corresponding items.")

    // Items arg
    let itemArg =
        Argument<string []>("item", "Directory or file to hash.")
    itemArg.Arity <- ArgumentArity.OneOrMore
    verifyCmd.AddArgument itemArg

    // Algorithm option
    let hashAlgOption =
        Option<string>([| "-a"; "--algorithm" |], "The hash function to use. If unspecified, will try to use the appropriate function based on hash length.")
    hashAlgOption.FromAmong(allHashTypesStr) |> ignore
    verifyCmd.AddOption hashAlgOption

    verifyCmd.Handler <- CommandHandler.Create(verifyCmdHandler)
    verifyCmd


[<EntryPoint>]
let main args =
    let root =
        RootCommand("A command-line utility to checksum directories and files.")

    // Compute Command
    let computeCmd = Command("hash", "Compute the hash for selected files/directories.")
    computeCmd.AddOption(Option<bool>([| "-t"; "--tree" |], "Print directory tree."))
    root.AddCommand computeCmd

    // Verify Command
    root.AddCommand verifyCmd

    // ARGS
    let itemArg =
        Argument<string []>("item", "Directory or file to hash.")

    itemArg.Arity <- ArgumentArity.OneOrMore
    //root.AddArgument itemArg

    // OPTIONS
    root.AddOption(Option<bool>([| "-t"; "--tree" |], "Print directory tree."))
    root.AddOption(Option<bool>([| "-i"; "--include-hidden-files" |], "Include hidden files."))
    root.AddOption(Option<bool>([| "-e"; "--skip-empty-dir" |], "Skip empty directories."))
    // Hash Algorithm
    let hashAlgOption =
        Option<string>([| "-a"; "--algorithm" |], (fun () -> "sha1"), "The hash function to use.")

    hashAlgOption.FromAmong(allHashTypesStr) |> ignore
    //root.AddOption hashAlgOption

    root.Handler <- CommandHandler.Create(cmdHandler)
    root.Invoke args
