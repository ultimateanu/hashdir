namespace HashUtil

open System
open System.IO
open System.Text.RegularExpressions
open System.Security.Cryptography
open System.Text


module Common =
    let bSpacer = "    "
    let iSpacer = "│   "
    let tSpacer = "├── "
    let lSpacer = "└── "


module Util =
    let toStrLower x = x.ToString().ToLower()

    let makeOption x =
        match x with
        | Error _ -> None
        | Ok v -> Some(v)

    let getChildName path =
        if Directory.Exists path then
            DirectoryInfo(path).Name
        else
            Path.GetFileName path

    let printColor colorize color str =
        if colorize then
            Console.ForegroundColor <- color
            printf "%s" str
            Console.ResetColor()
        else
            printf "%s" str

    let printColorToWriter
        (colorize: bool)
        (color: ConsoleColor option)
        (str: string)
        (outputWriter: TextWriter)
        =
        let colorToPrint: ConsoleColor option =
            match colorize with
            | true -> color
            | false -> None

        match colorToPrint with
        | Some c ->
            Console.ForegroundColor <- c
            outputWriter.Write(str)
            Console.ResetColor()
        | None -> outputWriter.Write(str)

    // Removes trailing characters if path is directory.
    let cleanPath path =
        if Directory.Exists(path) then
            let curDir = DirectoryInfo(path)
            let parentDir = curDir.Parent
            Path.Join(parentDir.FullName, curDir.Name)
        else
            path

    let (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)

        if m.Success then
            Some(List.tail [ for g in m.Groups -> g.Value ])
        else
            None

    let computeHashOfString (hashAlg: HashAlgorithm) (str: string) =
        str
        |> Encoding.ASCII.GetBytes
        |> hashAlg.ComputeHash
        |> Seq.map (fun c -> c.ToString("x2"))
        |> Seq.reduce (+)

    let computeHashOfFile (hashAlg: HashAlgorithm) filePath =
        assert File.Exists filePath
        use file = File.OpenRead filePath

        file
        |> hashAlg.ComputeHash
        |> Seq.map (fun c -> c.ToString("x2"))
        |> Seq.reduce (+)
