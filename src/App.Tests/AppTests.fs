﻿module AppTests

open HashUtil.Checksum
open HashUtil.Hashing
open HashUtil.Verification
open System
open System.IO
open Xunit
open Xunit.Abstractions

type AppTests(output: ITestOutputHelper) =
    [<Fact>]
    member _.``Console width is not too small``() =
        Assert.True(Progress.getConsoleMaxWidth () > 10)

    [<Fact>]
    member _.``RootOpt parses md5 algorithm correctly``() =
        let rootOpt =
            Program.RootOpt(
                [| "report.pdf"; "sources.txt" |],
                true,
                true,
                true,
                false,
                [| "**/node"; "cache.txt" |], (* ignore *)
                false,
                "md5",
                false
            )

        Assert.Equal(HashType.MD5, rootOpt.Algorithm)

        let expectedStr =
            "RootOpt[Items:[|\"report.pdf\"; \"sources.txt\"|] PrintTree:true Save:true IncludeHiddenFiles:true SkipEmptyDir:false IgnorePatterns:[|\"**/node\"; \"cache.txt\"|] HashOnly:false Algorithm:MD5 Color:false]"

        Assert.Equal(expectedStr, rootOpt.ToString())

    [<Fact>]
    member _.``CheckOpt parses algorithm and verbosity correctly``() =
        let checkOpt =
            Program.CheckOpt(
                [| "report.pdf" |],
                false,
                true,
                [| "**/node"; "cache.txt" |], (* ignore *)
                "sha256",
                "detailed",
                true
            )

        Assert.True(checkOpt.Algorithm.IsSome)
        Assert.Equal(HashType.SHA256, checkOpt.Algorithm.Value)
        Assert.Equal(PrintVerbosity.Detailed, checkOpt.Verbosity)

        let expectedStr =
            "CheckOpt[Items:[|\"report.pdf\"|] IncludeHiddenFiles:false SkipEmptyDir:true IgnorePatterns:[|\"**/node\"; \"cache.txt\"|] Algorithm:Some SHA256 Color:true]"

        Assert.Equal(expectedStr, checkOpt.ToString())

    [<Fact>]
    member _.``CheckOpt sets algorithm None when missing``() =
        let checkOpt =
            Program.CheckOpt(
                [| "report.pdf" |],
                false,
                true,
                [||], (* ignore *)
                null,
                "detailed",
                true
            )

        Assert.True(checkOpt.Algorithm.IsNone)

    [<Fact>]
    member _.``Progress string for short file name``() =
        // Setup observer which is on second file.
        let observer = Progress.HashingObserver()
        let iObserver = observer :> IObserver<HashingUpdate>
        iObserver.OnNext(HashingUpdate.FileHashStarted "/path/to/first.txt")
        iObserver.OnNext(HashingUpdate.FileHashCompleted "/path/to/first.txt")
        iObserver.OnNext(HashingUpdate.FileHashStarted "/path/to/second.txt")

        // Create progress string.
        use strWriter = new StringWriter()
        let nextIndex = Progress.makeProgressStr 0 observer strWriter true
        let progressStr = strWriter.ToString()

        // Expect final string to start this way.
        Assert.Equal(1, nextIndex)
        Assert.Equal(Progress.getConsoleMaxWidth (), progressStr.Length)
        Assert.Equal("\r⣷ 1 file [ second.txt ]", progressStr.TrimEnd())

    [<Fact>]
    member _.``Progress string for long file name``() =
        // Setup observer which has long filename.
        let observer = Progress.HashingObserver()
        let iObserver = observer :> IObserver<HashingUpdate>

        let filePath =
            sprintf "/path/to/%s.txt" (String.replicate 10 "longname")

        iObserver.OnNext(HashingUpdate.FileHashStarted filePath)

        // Create progress string.
        use strWriter = new StringWriter()

        let nextIndex =
            Progress.makeProgressStrInternal 0 observer 50 strWriter true

        let progressStr = strWriter.ToString()

        // Expect final string to start this way.
        Assert.Equal(1, nextIndex)
        Assert.Equal(50, progressStr.Length)

        Assert.Equal(
            "\r⣷ 0 files [ longnamelongname...namelongname.txt ]",
            progressStr.TrimEnd()
        )

    [<Fact>]
    member _.``Progress string for dir name``() =
        // Setup observer which is on first dir.
        let observer = Progress.HashingObserver()
        let iObserver = observer :> IObserver<HashingUpdate>
        iObserver.OnNext(HashingUpdate.DirHashStarted "/path/to/dir")

        // Create progress string.
        use strWriter = new StringWriter()
        let nextIndex = Progress.makeProgressStr 15 observer strWriter true
        let progressStr = strWriter.ToString()

        // Expect final string to start this way.
        Assert.Equal(0, nextIndex)
        Assert.Equal(Progress.getConsoleMaxWidth (), progressStr.Length)
        Assert.Equal("\r⣾ 0 files [ /dir ]", progressStr.TrimEnd())
