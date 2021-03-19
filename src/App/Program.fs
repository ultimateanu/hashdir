open HashUtil.Checksum
open HashUtil.FS
open HashUtil.Util
open HashUtil.Verification
open System.CommandLine
open System.CommandLine.Invocation
open System.Diagnostics
open System.IO


let defaultHashAlg = HashType.SHA1

type RootOpt(item, tree, save, includeHiddenFiles, skipEmptyDir, algorithm) =
    // Arguments
    member val Items: string [] = item

    // Options
    member val PrintTree: bool = tree
    member val Save: bool = save
    member val IncludeHiddenFiles: bool = includeHiddenFiles
    member val SkipEmptyDir: bool = skipEmptyDir
    member val Algorithm: HashType =
        match algorithm with
        | null -> defaultHashAlg
        | str ->
            let alg = parseHashType str
            assert alg.IsSome
            alg.Value

    override x.ToString() =
        sprintf
            "RootOpt[Items:%A PrintTree:%A Save:%A IncludeHiddenFiles:%A SkipEmptyDir:%A Algorithm:%A]"
            x.Items
            x.PrintTree
            x.Save
            x.IncludeHiddenFiles
            x.SkipEmptyDir
            x.Algorithm


type CheckOpt(item, includeHiddenFiles, skipEmptyDir, algorithm, verbosity) =
    // Arguments
    member val Items: string [] = item

    // Options
    member val IncludeHiddenFiles: bool = includeHiddenFiles
    member val SkipEmptyDir: bool = skipEmptyDir
    member val Algorithm: HashType option =
        match algorithm with
        | null -> None
        | str ->
            let alg = parseHashType str
            assert alg.IsSome
            Some alg.Value
    member val Verbosity: string = verbosity


    override x.ToString() =
        sprintf
            "VerifyOpt[Items:%A IncludeHiddenFiles:%A SkipEmptyDir:%A Algorithm:%A]"
            x.Items
            x.IncludeHiddenFiles
            x.SkipEmptyDir
            x.Algorithm


let rootHandler (opt: RootOpt) =
    for pathRaw in opt.Items do
        let path = cleanPath pathRaw
        let optHashStructure =
            makeHashStructure
                opt.Algorithm
                opt.IncludeHiddenFiles
                (not opt.SkipEmptyDir)
                path

        use strWriter = new StringWriter()

        match optHashStructure with
        | Error err -> printfn "Error: %s" err
        | Ok hashStructure ->
            if opt.Save then
                saveHashStructure hashStructure opt.PrintTree opt.Algorithm

            printHashStructure hashStructure opt.PrintTree strWriter
            printf "%s" (strWriter.ToString())


let checkHandler (opt: CheckOpt) =
    // Parse verbosity. System.CommandLine should have already verified.
    let verbosityMaybe = parsePrintVerbosity opt.Verbosity
    assert verbosityMaybe.IsSome
    let verbosity = verbosityMaybe.Value

    let processHashFile hashFile =
        let verifyResult =
            verifyHashFile
                opt.Algorithm
                opt.IncludeHiddenFiles
                (not opt.SkipEmptyDir)
                hashFile

        match verifyResult with
        | Error err ->
            printfn "Error: %s" err
            // return exit code 1 for missing hashFile
            1
        | Ok itemResults ->
            printVerificationResults verbosity itemResults

            if (allItemsMatch itemResults) then
                0
            else
                2

    let resultCodes =
        opt.Items
        |> Array.toList
        |> List.map processHashFile

    let hasError x =
        resultCodes |> List.tryFind (fun code -> code = x)

    match (hasError 1) with
    | Some code -> code
    | _ ->
        match (hasError 2) with
        | Some code -> code
        | _ -> 0


let itemArg =
    let arg =
        Argument<string []>("item", "Directory or file to hash/check")

    arg.Arity <- ArgumentArity.OneOrMore
    arg


let algorithmOpt forCheck =
    let hashAlgOption =
        match forCheck with
        | true ->
            Option<string>(
                [| "-a"; "--algorithm" |],
                "The hash function to use. If unspecified, will try to "
                + "use the appropriate function based on hash length"
            )
        | false ->
            Option<string>(
                [| "-a"; "--algorithm" |],
                sprintf "The hash function to use [default: %s]"
                    <| defaultHashAlg.ToString().ToLower()
            )

    let allHashTypesStr = allHashTypes |> Array.map toStrLower
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
            "Sets the verbosity level for the output"
        )

    opt.FromAmong(allPrintVerbosity |> Array.map toStrLower)
    |> ignore

    opt

let verifyCmd =
    let verifyCmd =
        Command("check", "Verify that the specified hash file is valid.")

    // ARGS
    verifyCmd.AddArgument itemArg

    // OPTIONS
    verifyCmd.AddOption hiddenFilesOpt
    verifyCmd.AddOption skipEmptyOpt
    verifyCmd.AddOption(algorithmOpt true)
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

    root.AddOption(
        Option<bool>([| "-s"; "--save" |], "Save the checksum to a file")
    )

    root.AddOption hiddenFilesOpt
    root.AddOption skipEmptyOpt
    root.AddOption(algorithmOpt false)

    root.Handler <- CommandHandler.Create(rootHandler)
    root


[<EntryPoint>]
let main args =
    let returnCode = rootCmd.Invoke args
    returnCode
