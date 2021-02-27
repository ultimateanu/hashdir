namespace HashUtil

open System
open System.IO

module Util =
    let toStrLower x =
        x.ToString().ToLower()

    let makeOption x =
        match x with
        | Error _ -> None
        | Ok v -> Some(v)

    let getDirName path =
        DirectoryInfo(path).Name

    let printColor color str =
        Console.ForegroundColor <- color
        printf "%s" str
        Console.ResetColor()
