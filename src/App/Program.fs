open HashUtil.Checksum
open HashUtil.FS
open HashUtil.Hashing
open HashUtil.Util
open HashUtil.Verification
open System
open System.CommandLine
open System.CommandLine.Invocation
open System.IO
open System.Threading


let defaultHashAlg = HashType.SHA1
let slashes = [|'/';'-'; '\\'; '|'|]

type HashingObserver() =
    let mutable filesHashed = 0
    let mutable hashingFile : string option = None
    let mutable hashingDir : string option = None
    member this.FilesHashed = filesHashed
    member this.HashingFile = hashingFile
    member this.HashingDir = hashingDir

    interface IObserver<HashingUpdate> with
        member this.OnCompleted(): unit =
            ()
        member this.OnError(error: exn): unit =
            raise (System.NotImplementedException())
        member this.OnNext(hashingUpdate: HashingUpdate): unit =
            match hashingUpdate with
                | FileHashStarted path ->
                    hashingFile <- Some path
                | FileHashCompleted _ ->
                    filesHashed <- this.FilesHashed + 1
                    hashingFile <- None
                | DirHashStarted dirPath ->
                    hashingDir <- Some dirPath
                    ()
                | DirHashCompleted _ ->
                    hashingDir <- None
                    ()
            ()


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
    member val Verbosity: PrintVerbosity =
        let verbosityMaybe = parsePrintVerbosity verbosity
        assert verbosityMaybe.IsSome
        verbosityMaybe.Value

    override x.ToString() =
        sprintf
            "CheckOpt[Items:%A IncludeHiddenFiles:%A SkipEmptyDir:%A Algorithm:%A]"
            x.Items
            x.IncludeHiddenFiles
            x.SkipEmptyDir
            x.Algorithm


let rootHandler (opt: RootOpt) =
    let hashingProgressObserver = HashingObserver()

    for pathRaw in opt.Items do
        let path = cleanPath pathRaw
        let hashingTask =
            Async.StartAsTask <|
                makeHashStructureObservable
                    hashingProgressObserver
                    opt.Algorithm
                    opt.IncludeHiddenFiles
                    (not opt.SkipEmptyDir)
                    path

        // Print current progress while hashing.
        let makeProgressStr slash (hashingObserver:HashingObserver)  =
            let numFiles = hashingObserver.FilesHashed
            let curFile = hashingObserver.HashingFile
            let curDir = hashingObserver.HashingDir
            let maxWidth = Console.WindowWidth
            let fileStr = if numFiles = 1 then "file" else "files"

            let makeLine (item:string) =
                let oldLen = (sprintf "\r%c %d %s [  ]" slash numFiles fileStr).Length
                let remainingSpace = max 0 (maxWidth - oldLen)
                let truncatedName =
                    if item.Length > remainingSpace then
                        item.Substring(0,remainingSpace)
                    else
                        item
                sprintf "\r%c %d %s [ %s ]" slash numFiles fileStr truncatedName

            let str =
                match curFile with
                    | None ->
                        // No file currently, report directory.
                        match curDir with
                            | None -> sprintf "\r%c %d %s" slash numFiles fileStr
                            | Some dirPath -> makeLine ("/" + (getChildName dirPath))
                    | Some fullPath -> fullPath |> getChildName |> makeLine

            let fullStr = str.PadRight maxWidth
            assert (fullStr.Length = maxWidth)
            fullStr

        let mutable slashIndex = 0
        while not hashingTask.IsCompleted do
            let slash = Array.get slashes slashIndex
            eprintf "%s" (makeProgressStr slash hashingProgressObserver)
            Thread.Sleep(150)
            slashIndex <- (slashIndex + 1) % slashes.Length
        eprintf "\r"

        let optHashStructure = hashingTask.Result
        use strWriter = new StringWriter()

        match optHashStructure with
        | Error err -> printfn "Error: %s" err
        | Ok hashStructure ->
            if opt.Save then
                saveHashStructure hashStructure opt.PrintTree opt.Algorithm

            printHashStructure hashStructure opt.PrintTree strWriter
            printf "%s" (strWriter.ToString())


let checkHandler (opt: CheckOpt) =
    let hashingProgressObserver = HashingObserver()

    let processHashFile hashFile =
        let verifyResult =
            verifyHashFile
                hashingProgressObserver
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
            printVerificationResults opt.Verbosity itemResults

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
