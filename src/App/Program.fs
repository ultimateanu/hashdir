open HashUtil.Checksum
open HashUtil.FS
open HashUtil.Hashing
open HashUtil.Util
open HashUtil.Verification
open System
open System.CommandLine
open System.CommandLine.Invocation
open System.Threading


let defaultHashAlg = HashType.SHA1

type RootOpt
    (
        item,
        tree,
        save,
        includeHiddenFiles,
        skipEmptyDir,
        hashOnly,
        algorithm,
        color: bool
    ) =
    // Arguments
    member val Items: string[] = item

    // Options
    member val PrintTree: bool = tree
    member val Save: bool = save
    member val IncludeHiddenFiles: bool = includeHiddenFiles
    member val SkipEmptyDir: bool = skipEmptyDir
    member val HashOnly: bool = hashOnly

    member val Algorithm: HashType =
        match algorithm with
        | null -> defaultHashAlg
        | str ->
            let alg = parseHashType str
            assert alg.IsSome
            alg.Value

    member val Color: bool = color

    override x.ToString() =
        sprintf
            "RootOpt[Items:%A PrintTree:%A Save:%A IncludeHiddenFiles:%A SkipEmptyDir:%A HashOnly:%A Algorithm:%A Color:%A]"
            x.Items
            x.PrintTree
            x.Save
            x.IncludeHiddenFiles
            x.SkipEmptyDir
            x.HashOnly
            x.Algorithm
            x.Color


type CheckOpt
    (
        item,
        includeHiddenFiles,
        skipEmptyDir,
        algorithm,
        verbosity,
        color
    ) =
    // Arguments
    member val Items: string[] = item

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

    member val Verbosity: PrintVerbosity =
        let verbosityMaybe = parsePrintVerbosity verbosity
        assert verbosityMaybe.IsSome
        verbosityMaybe.Value

    member val Color: bool = color

    override x.ToString() =
        sprintf
            "CheckOpt[Items:%A IncludeHiddenFiles:%A SkipEmptyDir:%A Algorithm:%A Color:%A]"
            x.Items
            x.IncludeHiddenFiles
            x.SkipEmptyDir
            x.Algorithm
            x.Color


let rootHandler (opt: RootOpt) =
    for pathRaw in opt.Items do
        let hashingProgressObserver = Progress.HashingObserver()

        let path = cleanPath pathRaw

        let hashingTask =
            Async.StartAsTask
            <| makeHashStructureObservable
                hashingProgressObserver
                opt.Algorithm
                opt.IncludeHiddenFiles
                (not opt.SkipEmptyDir)
                path

        // Show progress while hashing happens in background.
        let mutable slashIndex = 0

        while not hashingTask.IsCompleted do
            let nextIndex =
                Progress.makeProgressStr
                    slashIndex
                    hashingProgressObserver
                    Console.Error
                    opt.Color

            slashIndex <- nextIndex
            Thread.Sleep(200)

        Console.Error.Write("\r".PadRight(Progress.getConsoleMaxWidth ()))
        Console.Error.Write("\r")
        Console.Error.Flush()

        let optHashStructure = hashingTask.Result

        match optHashStructure with
        | Error err -> printfn "Error: %s" err
        | Ok hashStructure ->
            if opt.Save then
                saveHashStructure hashStructure opt.PrintTree opt.Algorithm

            printHashStructure hashStructure opt.PrintTree Console.Out opt.Color

let checkHandler (opt: CheckOpt) =
    let processHashFile hashFile =
        let hashingProgressObserver = Progress.HashingObserver()

        let verifyTask =
            Async.StartAsTask
            <| verifyHashFile
                hashingProgressObserver
                opt.Algorithm
                opt.IncludeHiddenFiles
                (not opt.SkipEmptyDir)
                hashFile

        // Show progress while verification happens in background.
        let mutable slashIndex = 0

        while not verifyTask.IsCompleted do
            let nextIndex =
                Progress.makeProgressStr
                    slashIndex
                    hashingProgressObserver
                    Console.Error
                    opt.Color

            slashIndex <- nextIndex
            Thread.Sleep(200)

        Console.Error.Write("\r".PadRight(Progress.getConsoleMaxWidth ()))
        Console.Error.Write("\r")
        Console.Error.Flush()

        let verifyResult = verifyTask.Result

        match verifyResult with
        | Error err ->
            printfn "Error: %s" err
            // return exit code 1 for missing hashFile
            1
        | Ok itemResults ->
            let printAndGetMatchResult result =
                printVerificationResults opt.Verbosity opt.Color result

                match result with
                | Ok r ->
                    match r with
                    | VerificationResult.Matches _ -> true
                    | _ -> false
                | Error _ -> false

            // Make list of matched before List.forall which might short circuit.
            let matched = itemResults |> List.map printAndGetMatchResult

            if List.forall id matched then 0 else 2

    let resultCodes = opt.Items |> Array.toList |> List.map processHashFile

    let hasError x =
        resultCodes |> List.tryFind (fun code -> code = x)

    match (hasError 1) with
    | Some code -> code
    | _ ->
        match (hasError 2) with
        | Some code -> code
        | _ -> 0


let itemArg =
    let arg = Argument<string[]>("item", "Directory or file to hash/check")

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

let hashOnlyOpt = Option<bool>([| "-h"; "--hash-only" |], "Print only the hash")

let verbosityOpt =
    let opt =
        Option<string>(
            [| "-v"; "--verbosity" |],
            (fun () -> toStrLower PrintVerbosity.Normal),
            "Sets the verbosity level for the output"
        )

    opt.FromAmong(allPrintVerbosity |> Array.map toStrLower) |> ignore
    opt

let colorOpt =
    Option<bool>([| "-c"; "--color" |], (fun () -> true), "Colorize the output")

let checkCmd =
    let checkCmd =
        Command("check", "Verify that the specified hash file is valid.")

    // ARGS
    checkCmd.AddArgument itemArg

    // OPTIONS
    checkCmd.AddOption hiddenFilesOpt
    checkCmd.AddOption skipEmptyOpt
    checkCmd.AddOption(algorithmOpt true)
    checkCmd.AddOption verbosityOpt
    checkCmd.Handler <- CommandHandler.Create(checkHandler)

    checkCmd

let rootCmd =
    let root =
        RootCommand("A command-line utility to checksum directories and files.")

    // Check (verb command)
    root.AddCommand checkCmd

    // ARGS
    root.AddArgument itemArg

    // OPTIONS
    root.AddOption(Option<bool>([| "-t"; "--tree" |], "Print directory tree"))

    root.AddOption(
        Option<bool>([| "-s"; "--save" |], "Save the checksum to a file")
    )

    root.AddOption hiddenFilesOpt
    root.AddOption skipEmptyOpt
    root.AddOption hashOnlyOpt
    root.AddOption(algorithmOpt false)
    root.AddOption colorOpt

    root.Handler <- CommandHandler.Create(rootHandler)
    root


[<EntryPoint>]
let main args =
    Console.OutputEncoding <- System.Text.Encoding.UTF8
    let returnCode = rootCmd.Invoke args
    returnCode
