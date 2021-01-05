open CommandLine
open HashUtil.FS


type Options = {
  [<Option('i', "include-hidden-files", Default = false, HelpText = "Include hidden files.")>] IncludeHiddenFiles : bool;
  [<Option('e', "include-empty-dir", Default = true, HelpText = "Include empty directories.")>] IncludeEmptyDir : bool;
  [<Value(0, Required = true, MetaName="input", HelpText = "Input directories or files.")>] Input : seq<string>;
}


let run (o : Options)  =
    for item in o.Input do
        let optHashStructure = makeHashStructure o.IncludeHiddenFiles o.IncludeEmptyDir item
        match optHashStructure with
            | Error e -> printfn "Error: %s" e
            | Ok hashStructure -> printHashStructure hashStructure


[<EntryPoint>]
let main argv =
    let parsedResult = CommandLine.Parser.Default.ParseArguments<Options>(argv)
    match parsedResult with
        | :? Parsed<Options> as parsed -> run parsed.Value
        | _ -> ()

    0
