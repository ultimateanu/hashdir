open CommandLine
open HashUtil.FS
open System.IO


type Options =
    { [<Option('h', "include-hidden-files", Default = false, HelpText = "Include hidden files.")>]
      IncludeHiddenFiles: bool
      [<Option('e', "skip-empty-dir", Default = false, HelpText = "Skip empty directories.")>]
      SkipEmptyDir: bool
      [<Value(0, Required = true, MetaName = "input", HelpText = "Input directories or files.")>]
      Input: seq<string> }


let run (o: Options) =
    for item in o.Input do
        let optHashStructure =
            makeHashStructure o.IncludeHiddenFiles (not o.SkipEmptyDir) item
        let strWriter = new StringWriter()
        match optHashStructure with
        | Error e -> printfn "Error: %s" e
        | Ok hashStructure ->
            printHashStructure hashStructure strWriter
            printf "%s" (strWriter.ToString())


[<EntryPoint>]
let main argv =
    let parsedResult =
        CommandLine.Parser.Default.ParseArguments<Options>(argv)

    match parsedResult with
    | :? (Parsed<Options>) as parsed -> run parsed.Value
    | _ -> ()

    0
