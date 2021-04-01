module AppTests

open HashUtil.Checksum
open HashUtil.Hashing
open HashUtil.Verification
open System
open Xunit
open Xunit.Abstractions

type AppTests(output: ITestOutputHelper) =
    [<Fact>]
    member _.``Console width is not too small``() =
        Assert.True(Progress.consoleMaxWidth() > 10)

    [<Fact>]
    member _.``RootOpt parses md5 algorithm correctly``() =
        let rootOpt =
            Program.RootOpt([|"report.pdf"; "sources.txt"|],
                true, true, true, false, "md5")

        Assert.Equal(HashType.MD5, rootOpt.Algorithm)
        let expectedStr = "RootOpt[Items:[|\"report.pdf\"; \"sources.txt\"|] PrintTree:true Save:true IncludeHiddenFiles:true SkipEmptyDir:false Algorithm:MD5]"
        Assert.Equal(expectedStr, rootOpt.ToString())

    [<Fact>]
    member _.``RootOpt uses sha1 if algorithm not specified``() =
        let rootOpt =
            Program.RootOpt([|"report.pdf"; "sources.txt"|],
                true, true, true, false, null)

        Assert.Equal(HashType.SHA1, rootOpt.Algorithm)

    [<Fact>]
    member _.``CheckOpt parses algorithm and verbosity correctly``() =
        let checkOpt =
            Program.CheckOpt([|"report.pdf"|], false, true, "sha256", "detailed")

        Assert.True(checkOpt.Algorithm.IsSome)
        Assert.Equal(HashType.SHA256, checkOpt.Algorithm.Value)
        Assert.Equal(PrintVerbosity.Detailed, checkOpt.Verbosity)
        let expectedStr = "CheckOpt[Items:[|\"report.pdf\"|] IncludeHiddenFiles:false SkipEmptyDir:true Algorithm:Some SHA256]"
        Assert.Equal(expectedStr, checkOpt.ToString())

    [<Fact>]
    member _.``CheckOpt sets algorithm None when missing``() =
        let checkOpt =
            Program.CheckOpt([|"report.pdf"|], false, true, null, "detailed")

        Assert.True(checkOpt.Algorithm.IsNone)

    [<Fact>]
    member _.``Progress string for short file name``() =
        // Setup observer which is on second file.
        let observer = Progress.HashingObserver()
        let iObserver = observer :> IObserver<HashingUpdate>
        iObserver.OnNext (HashingUpdate.FileHashStarted "/path/to/first.txt")
        iObserver.OnNext (HashingUpdate.FileHashCompleted "/path/to/first.txt")
        iObserver.OnNext (HashingUpdate.FileHashStarted "/path/to/second.txt")

        // Create progress string.
        let progressStr, nextIndex = Progress.makeProgressStr 0 observer

        // Expect final string to start this way.
        Assert.Equal(1, nextIndex)
        Assert.Equal(Progress.consoleMaxWidth(), progressStr.Length)
        Assert.Equal("\r⣷ 1 file [ second.txt ]", progressStr.TrimEnd())

    [<Fact>]
    member _.``Progress string for dir name``() =
        // Setup observer which is on first dir.
        let observer = Progress.HashingObserver()
        let iObserver = observer :> IObserver<HashingUpdate>
        iObserver.OnNext (HashingUpdate.DirHashStarted "/path/to/dir")

        // Create progress string.
        let progressStr, nextIndex = Progress.makeProgressStr 15 observer

        // Expect final string to start this way.
        Assert.Equal(0, nextIndex)
        Assert.Equal(Progress.consoleMaxWidth(), progressStr.Length)
        Assert.Equal("\r⣾ 0 files [ /dir ]", progressStr.TrimEnd())
