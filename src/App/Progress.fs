module Progress

open HashUtil.Hashing
open HashUtil.Util
open System
open System.IO

type HashingObserver() =
    let mutable filesHashed = 0
    let mutable hashingFile : string option = None
    let mutable hashingDir : string option = None
    member this.FilesHashed = filesHashed
    member this.HashingFile = hashingFile
    member this.HashingDir = hashingDir

    interface IObserver<HashingUpdate> with
        member this.OnCompleted() : unit = ()

        member this.OnError(error: exn) : unit =
            raise (System.NotImplementedException())

        member this.OnNext(hashingUpdate: HashingUpdate) : unit =
            match hashingUpdate with
            | FileHashStarted path -> hashingFile <- Some path
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

let progressSymbols =
    [| '⣷'
       '⣯'
       '⣟'
       '⡿'
       '⢿'
       '⣻'
       '⣽'
       '⣾' |]

let private incProgressIndex slashIndex =
    (slashIndex + 1) % progressSymbols.Length

let getConsoleMaxWidth () =
    let defaultWidth = 60

    try
        if Console.BufferWidth > 10 then
            Console.BufferWidth
        else
            defaultWidth
    with
    // Use a default backup width value if needed (e.g. xUnit tests)
    | _ -> defaultWidth

// Internal version which takes in console width for testing.
let makeProgressStrInternal
    slashIndex
    (hashingObserver: HashingObserver)
    consoleMaxWidth
    (outputWriter: TextWriter)
    (colorize: bool)
    =
    let slash =
        Array.get progressSymbols (slashIndex % progressSymbols.Length)

    let numFiles = hashingObserver.FilesHashed
    let curFile = hashingObserver.HashingFile
    let curDir = hashingObserver.HashingDir
    let fileStr = if numFiles = 1 then "file" else "files"

    printColorToWriter colorize None "\r" outputWriter
    printColorToWriter colorize (Some ConsoleColor.Green) (string slash) outputWriter
    printColorToWriter colorize None (sprintf " %d %s " numFiles fileStr) outputWriter

    let mutable outputCopy =
        sprintf "\r%c %d %s " slash numFiles fileStr

    let makeTruncatedName (item: string) =
        let oldLen = outputCopy.Length + "[  ]".Length
        let remainingSpace = max 0 (consoleMaxWidth - oldLen)

        let truncatedName =
            if item.Length > remainingSpace then
                let middlePart = "..."
                let halfSpace = (remainingSpace - middlePart.Length) / 2
                let leftHalf = item.Substring(0, halfSpace)
                let rightHalf = item.Substring(item.Length - halfSpace)

                assert (leftHalf.Length = rightHalf.Length)
                leftHalf + middlePart + rightHalf
            else
                item

        truncatedName

    match curFile with
    | None ->
        // No file currently, report directory.
        match curDir with
        | None -> ()
        | Some dirPath ->
            let dirStr =
                makeTruncatedName ("/" + (getChildName dirPath))

            printColorToWriter colorize None "[ " outputWriter
            printColorToWriter colorize (Some ConsoleColor.Cyan) dirStr outputWriter
            printColorToWriter colorize None " ]" outputWriter
            outputCopy <- outputCopy + "[ " + dirStr + " ]"
    | Some fullPath ->
        let fileStr =
            fullPath |> getChildName |> makeTruncatedName

        printColorToWriter colorize None "[ " outputWriter
        printColorToWriter colorize None fileStr outputWriter
        printColorToWriter colorize None " ]" outputWriter
        outputCopy <- outputCopy + "[ " + fileStr + " ]"

    // Print spaces to reach end of line (to clear old output).
    let remainingWidth =
        max 0 (consoleMaxWidth - outputCopy.Length)

    printColorToWriter colorize None (String.replicate remainingWidth " ") outputWriter
    outputCopy <- outputCopy + (String.replicate remainingWidth " ")
    assert (outputCopy.Length >= consoleMaxWidth)

    incProgressIndex slashIndex

// Print current progress while hashing.
let makeProgressStr
    slashIndex
    (hashingObserver: HashingObserver)
    (outputWriter: TextWriter)
    (colorize: bool)
    =
    makeProgressStrInternal
        slashIndex
        hashingObserver
        (getConsoleMaxWidth ())
        outputWriter
        colorize
