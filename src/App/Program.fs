open CommandLine
open HashUtil.Checksum
open HashUtil.FS
open System.IO

type Options =
    { [<Option('t', "tree", Default = false, HelpText = "Print directory tree.")>]
      Tree: bool
      [<Option('h', "include-hidden-files", Default = false, HelpText = "Include hidden files.")>]
      IncludeHiddenFiles: bool
      [<Option('e', "skip-empty-dir", Default = false, HelpText = "Skip empty directories.")>]
      SkipEmptyDir: bool
      [<Value(0, Required = true, MetaName = "input", HelpText = "Input directories or files.")>]
      Input: seq<string> }


let run (o: Options) =
    for item in o.Input do
        let optHashStructure =
            makeHashStructure SHA256 o.IncludeHiddenFiles (not o.SkipEmptyDir) item

        let strWriter = new StringWriter()

        match optHashStructure with
        | Error e -> printfn "Error: %s" e
        | Ok hashStructure ->
            printHashStructure hashStructure o.Tree strWriter
            printf "%s" (strWriter.ToString())


[<EntryPoint>]
let main argv =
    let parsedResult =
        CommandLine.Parser.Default.ParseArguments<Options>(argv)

    match parsedResult with
    | :? (Parsed<Options>) as parsed -> run parsed.Value
    | _ -> ()

    0
