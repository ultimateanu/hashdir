module E2ETests

open System
open System.Collections
open System.Collections.Generic
open System.IO
open System.Runtime.InteropServices
open Xunit
open Xunit.Abstractions


[<AbstractClass>]
type BaseTestData() =
    abstract member data: seq<obj[]>
    interface IEnumerable<obj[]> with
        member this.GetEnumerator() : IEnumerator<obj[]> = this.data.GetEnumerator()
        member this.GetEnumerator() : IEnumerator = this.data.GetEnumerator() :> IEnumerator


type HashingConfigs() =
    inherit BaseTestData()
    override _.data = Seq.ofList [
        [| box [||] |];
        [| box [|"--algorithm"; "md5"|] |];
        [| box [|"--include-hidden-files"|] |];
        [| box [|"--skip-empty-dir"|] |];
        [| box [|"--skip-empty-dir"; "--include-hidden-files"|] |];
    ]


type FsTempDirSetupFixture() =
    // Single temp dir which always gets cleaned up.
    let tempDir =
        Path.GetFullPath(Path.Combine(Path.GetTempPath(), "hashdir_test_" + Guid.NewGuid().ToString()))
    let projectDir = Path.Combine(tempDir, "project")

    // SETUP
    do
        Directory.CreateDirectory(tempDir) |> ignore
        Directory.CreateDirectory(projectDir) |> ignore
        File.WriteAllText(Path.Combine(projectDir, "project1.txt"), "project1")

        // Dir a has a couple of files.
        Directory.CreateDirectory(Path.Combine(projectDir, "a")) |> ignore
        File.WriteAllText(Path.Combine(projectDir, "a", "a1.txt"), "a1")
        File.WriteAllText(Path.Combine(projectDir, "a", "a2.txt"), "a2")
        File.WriteAllText(Path.Combine(projectDir, "a", "a2.txt"), "a2")

        // Dir b has a sub dir and a file within that.
        Directory.CreateDirectory(Path.Combine(projectDir, "b")) |> ignore
        Directory.CreateDirectory(Path.Combine(projectDir, "b", "bb")) |> ignore
        File.WriteAllText(Path.Combine(projectDir, "b", "bb", "b1.txt"), "b1")

        // Dir c has a hidden file.
        Directory.CreateDirectory(Path.Combine(projectDir, "c")) |> ignore
        let hiddenFilePath = Path.Combine(projectDir, "c", ".c1.txt")
        File.WriteAllText(hiddenFilePath, "c1")
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            File.SetAttributes(hiddenFilePath, FileAttributes.Hidden)

        // Dir d is empty.
        Directory.CreateDirectory(Path.Combine(projectDir, "d")) |> ignore


    // CLEANUP
    interface IDisposable with
        member _.Dispose() = Directory.Delete(tempDir, true)

    member _.TempDir = tempDir
    member _.ProjectDir = projectDir


type CheckHashfile(fsTempDirSetupFixture: FsTempDirSetupFixture, debugOutput: ITestOutputHelper) =
    let hashFile = Path.Combine(fsTempDirSetupFixture.TempDir, "project_hash.txt")
    let oldStdOut = Console.Out
    let customStdOut = new IO.StringWriter()
    let getStdOut() =
        let buf = customStdOut.GetStringBuilder().ToString()
        customStdOut.GetStringBuilder().Clear() |> ignore
        buf

    // SETUP
    do
        Console.SetOut(customStdOut)
        File.WriteAllText(hashFile, "")


    // CLEANUP
    interface IDisposable with
        member _.Dispose() =
            Console.SetOut(oldStdOut)
            File.Delete(hashFile)

    interface IClassFixture<FsTempDirSetupFixture>


    [<Fact>]
    member _.``help flag``() =
        // Run program with help flag.
        let returnCode = Program.main [|"--help"|]
        Assert.Equal(0, returnCode)

        // Expect output to have help info.
        Assert.Contains(
            "A command-line utility to checksum directories and files.",
            getStdOut())


    [<Fact>]
    member _.``check hash file``() =
        // Simple hashfile with sha1 hash for project dir.
        File.WriteAllText(hashFile, "264aba9860d3dc213423759991dad98259bbf0c5  /project")

        // Run program and ask to check the hashfile.
        let returnCode = Program.main [|"check"; hashFile|]
        Assert.Equal(0, returnCode)

        // Expect output to say matches.
        let expectedOutput = sprintf "MATCHES    /project%s" Environment.NewLine
        Assert.Equal(expectedOutput, getStdOut())

    [<Fact>]
    member _.``check hash file (include hidden files)``() =
        // Simple hashfile with sha1 hash for project dir (including hidden files).
        File.WriteAllText(hashFile, "ea60ecdab5f999ef34fd19825ce63fac83a0c75b  /project")

        // Run program and ask to check the hashfile.
        let returnCode = Program.main [|"check"; hashFile; "--include-hidden-files"|]
        Assert.Equal(0, returnCode)

        // Expect output to say matches.
        let expectedOutput = sprintf "MATCHES    /project%s" Environment.NewLine
        Assert.Equal(expectedOutput, getStdOut())

    [<Fact>]
    member _.``check hash file (skip empty dir)``() =
        // Simple hashfile with sha1 hash for project dir (including empty dirs).
        File.WriteAllText(hashFile, "d4efa40abcb6ec73ee83df4c532aad568e7160a5  /project")

        // Run program and ask to check the hashfile.
        let returnCode = Program.main [|"check"; hashFile; "--skip-empty-dir"|]
        Assert.Equal(0, returnCode)

        // Expect output to say matches.
        let expectedOutput = sprintf "MATCHES    /project%s" Environment.NewLine
        Assert.Equal(expectedOutput, getStdOut())

    [<Fact>]
    member _.``check hash file (include hidden files, skip empty dir)``() =
        // Simple hashfile with sha1 hash for project dir (including empty dirs).
        File.WriteAllText(hashFile, "16e6570418dba2f4589c8972b9cfe4bb9e5c449c  /project")

        // Run program and ask to check the hashfile.
        let returnCode = Program.main [|"check"; hashFile; "--include-hidden-files"; "--skip-empty-dir"|]
        Assert.Equal(0, returnCode)

        // Expect output to say matches.
        let expectedOutput = sprintf "MATCHES    /project%s" Environment.NewLine
        Assert.Equal(expectedOutput, getStdOut())

    [<Theory>]
    [<ClassData(typeof<HashingConfigs>)>]
    member _.``check hash file matches hashing output``(inputFlags) =
        let hashingArgs = Array.append [|fsTempDirSetupFixture.ProjectDir|] inputFlags
        let checkArgs = Array.append [|"check"; hashFile|] inputFlags

        // Write output of hashing to hashFile.
        Assert.Equal(0, Program.main hashingArgs)
        File.WriteAllText(hashFile, getStdOut())

        // Run program and ask to check the hashfile.
        let returnCode = Program.main checkArgs
        Assert.Equal(0, returnCode)

        // Expect output to say matches.
        let expectedOutput = sprintf "MATCHES    /project%s" Environment.NewLine
        Assert.Equal(expectedOutput, getStdOut())

    [<Theory>]
    [<InlineData("normal",
        "MATCHES    match.txt",
        "DIFFERS    diff.txt",
        "ERROR")>]
    [<InlineData("detailed",
        "MATCHES    match.txt (ef5c844eab88bcaca779bd2f3ad67b573bbbbfca)",
        "DIFFERS    diff.txt (75a0ee1ba911f2f5199177dfd31808a12511bbdc, expected: 1111570418dba2f4589c8972b9cfe4bb9e5caaaa)",
        "missing.txt is not a valid path")>]
    [<InlineData("quiet", "", "", "")>]
    member _.``check hash file different verbosity levels``(verbosityLevel, matchOutput, diffOutput, errorOutput) =
        // Setup to ensure 1 match, diff and missing result.
        File.WriteAllText(Path.Combine(fsTempDirSetupFixture.TempDir, "match.txt"), "match")
        File.WriteAllText(Path.Combine(fsTempDirSetupFixture.TempDir, "diff.txt"), "diff")
        let hashFileContent = "\
            ef5c844eab88bcaca779bd2f3ad67b573bbbbfca  match.txt\n\
            1111570418dba2f4589c8972b9cfe4bb9e5caaaa  diff.txt\n\
            1111570418dba2f4589c8972b9cfe4bb9e5caaaa  missing.txt"
        File.WriteAllText(hashFile, hashFileContent)

        // Run program and ask to check the hashfile.
        let returnCode = Program.main [|"check"; hashFile; "--verbosity"; verbosityLevel|]

        // Expect return code error due to missing file.
        Assert.Equal(2, returnCode)
        // Expect specific output for each of the 3 cases.
        let checkOutput = getStdOut()
        Assert.Contains(matchOutput, checkOutput)
        Assert.Contains(diffOutput, checkOutput)
        Assert.Contains(errorOutput, checkOutput)
        if verbosityLevel = "quiet" then
            Assert.Equal("", checkOutput)
