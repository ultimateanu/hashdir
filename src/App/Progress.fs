module Progress

open HashUtil.Hashing
open HashUtil.Util
open System

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

let progressSymbols = [|'⣷';  '⣯'; '⣟'; '⡿'; '⢿'; '⣻'; '⣽'; '⣾'|]

let private incProgressIndex slashIndex =
    (slashIndex + 1) % progressSymbols.Length

let getConsoleMaxWidth() =
    let defaultWidth = 60
    try
        if Console.BufferWidth > 10 then Console.BufferWidth else defaultWidth
    with
        // Use a default backup width value if needed (e.g. xUnit tests)
        _ -> defaultWidth

// Internal version which takes in console width for testing.
let makeProgressStrInternal slashIndex (hashingObserver:HashingObserver) consoleMaxWidth =
    let slash = Array.get progressSymbols (slashIndex % progressSymbols.Length)
    let numFiles = hashingObserver.FilesHashed
    let curFile = hashingObserver.HashingFile
    let curDir = hashingObserver.HashingDir
    let fileStr = if numFiles = 1 then "file" else "files"

    let makeLine (item:string) =
        let oldLen = (sprintf "\r%c %d %s [  ]" slash numFiles fileStr).Length
        let remainingSpace = max 0 (consoleMaxWidth - oldLen)
        let truncatedName =
            if item.Length > remainingSpace then
                let middlePart = "..."
                let halfSpace = (remainingSpace - middlePart.Length) / 2
                let leftHalf = item.Substring(0, halfSpace)
                let rightHalf = item.Substring(item.Length - halfSpace)

                assert(leftHalf.Length = rightHalf.Length)
                leftHalf + middlePart + rightHalf
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

    let fullStr = str.PadRight consoleMaxWidth
    assert (fullStr.Length = consoleMaxWidth)
    (fullStr, incProgressIndex slashIndex)

// Print current progress while hashing.
let makeProgressStr slashIndex (hashingObserver:HashingObserver)  =
    makeProgressStrInternal slashIndex hashingObserver (getConsoleMaxWidth())
