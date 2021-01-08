namespace HashUtil

open System.IO

module Util =
    let getDirName path = DirectoryInfo(path).Name

    let makeOption x =
        match x with
            | Error _ -> None
            | Ok v -> Some(v)
