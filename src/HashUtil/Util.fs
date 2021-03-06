﻿namespace HashUtil

open System
open System.IO
open System.Text.RegularExpressions


module Common =
    let bSpacer = "    "
    let iSpacer = "│   "
    let tSpacer = "├── "
    let lSpacer = "└── "


module Util =
    let toStrLower x =
        x.ToString().ToLower()

    let makeOption x =
        match x with
        | Error _ -> None
        | Ok v -> Some(v)

    let getChildName path =
        if Directory.Exists path then
            DirectoryInfo(path).Name
        else
            Path.GetFileName path

    let printColor color str =
        Console.ForegroundColor <- color
        printf "%s" str
        Console.ResetColor()

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
