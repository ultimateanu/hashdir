open CommandLine
open HashUtil.FS


type Options = {
  [<Option('i', "include-hidden-files", Default = false, HelpText = "Include hidden files.")>] IncludeHiddenFiles : bool;
  [<Option('e', "include-empty-dir", Default = true, HelpText = "Include empty directories.")>] IncludeEmptyDir : bool;
  [<Value(0, Required = true, MetaName="input", HelpText = "Input directories or files.")>] Input : seq<string>;
}


let run (o : Options)  =
    for item in o.Input do
        // TODO: add arg for empty dir
        let optHashStructure = makeHashStructure o.IncludeHiddenFiles true item
        match optHashStructure with
            // TODO: return more informative type in case dir exists but is empty
            | None -> printfn "%s is not a valid path" item
            | Some(hashStructure) -> printHashStructure hashStructure


[<EntryPoint>]
let main argv =
    let parsedResult = CommandLine.Parser.Default.ParseArguments<Options>(argv)
    match parsedResult with
        | :? Parsed<Options> as parsed -> run parsed.Value
        | _ -> ()

    0
