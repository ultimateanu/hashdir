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
        member this.GetEnumerator() : IEnumerator<obj[]> =
            this.data.GetEnumerator()

        member this.GetEnumerator() : IEnumerator =
            this.data.GetEnumerator() :> IEnumerator

type HashingConfigs() =
    inherit BaseTestData()

    override _.data =
        Seq.ofList
            [ [| box [||] |]
              [| box [| "--algorithm"; "md5" |] |]
              [| box [| "--ignore"; "**/a1.txt" |] |]
              [| box [| "--include-hidden-files" |] |]
              [| box [| "--skip-empty-dir" |] |]
              [| box [| "--skip-empty-dir"; "--include-hidden-files" |] |] ]

type FsTempDirSetupFixture() =
    // Single temp dir which always gets cleaned up.
    let tempDir =
        Path.GetFullPath(
            Path.Combine(
                Path.GetTempPath(),
                "hashdir_test_" + Guid.NewGuid().ToString()
            )
        )

    let projectDir = Path.Combine(tempDir, "project")
    let topFileA = Path.Combine(tempDir, "topA.txt")

    // SETUP
    do
        Directory.CreateDirectory(tempDir) |> ignore

        // Create top level file.
        File.WriteAllText(topFileA, "topA")

        // Create project dir.
        Directory.CreateDirectory(projectDir) |> ignore
        File.WriteAllText(Path.Combine(projectDir, "project1.txt"), "project1")

        // Dir a has a couple of files.
        Directory.CreateDirectory(Path.Combine(projectDir, "a")) |> ignore
        File.WriteAllText(Path.Combine(projectDir, "a", "a1.txt"), "a1")
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
    member _.TopFileA = topFileA


type FsTests
    (
        fsTempDirSetupFixture: FsTempDirSetupFixture,
        debugOutput: ITestOutputHelper
    ) =
    let hashFile =
        Path.Combine(fsTempDirSetupFixture.TempDir, "project_hash.sha1.txt")

    let oldStdOut = Console.Out
    let customStdOut = new IO.StringWriter()

    let getStdOut () =
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
        let returnCode = Program.main [| "--help" |]
        Assert.Equal(0, returnCode)

        // Expect output to have help info.
        Assert.Contains(
            "A command-line utility to hash directories and files.",
            getStdOut ()
        )


    [<Fact>]
    member _.``check hash file``() =
        // Simple hashfile with sha1 hash for project dir.
        File.WriteAllText(
            hashFile,
            "264aba9860d3dc213423759991dad98259bbf0c5  /project"
        )

        // Run program and ask to check the hashfile.
        let returnCode = Program.main [| "check"; hashFile; "-a"; "sha1" |]
        Assert.Equal(0, returnCode)

        // Expect output to say matches.
        let expectedOutput = sprintf "MATCHES    /project%s" Environment.NewLine
        Assert.Equal(expectedOutput, getStdOut ())

    [<Fact>]
    member _.``check hash file (include hidden files)``() =
        // Simple hashfile with sha1 hash for project dir (including hidden files).
        File.WriteAllText(
            hashFile,
            "ea60ecdab5f999ef34fd19825ce63fac83a0c75b  /project"
        )

        // Run program and ask to check the hashfile.
        let returnCode =
            Program.main
                [| "check"; hashFile; "-a"; "sha1"; "--include-hidden-files" |]

        Assert.Equal(0, returnCode)

        // Expect output to say matches.
        let expectedOutput = sprintf "MATCHES    /project%s" Environment.NewLine
        Assert.Equal(expectedOutput, getStdOut ())

    [<Fact>]
    member _.``check hash file (skip empty dir)``() =
        // Simple hashfile with sha1 hash for project dir (including empty dirs).
        File.WriteAllText(
            hashFile,
            "d4efa40abcb6ec73ee83df4c532aad568e7160a5  /project"
        )

        // Run program and ask to check the hashfile.
        let returnCode =
            Program.main
                [| "check"; hashFile; "-a"; "sha1"; "--skip-empty-dir" |]

        Assert.Equal(0, returnCode)

        // Expect output to say matches.
        let expectedOutput = sprintf "MATCHES    /project%s" Environment.NewLine
        Assert.Equal(expectedOutput, getStdOut ())

    [<Fact>]
    member _.``check hash file (include hidden files, skip empty dir)``() =
        // Simple hashfile with sha1 hash for project dir (including empty dirs).
        File.WriteAllText(
            hashFile,
            "16e6570418dba2f4589c8972b9cfe4bb9e5c449c  /project"
        )

        // Run program and ask to check the hashfile.
        let returnCode =
            Program.main
                [| "check"
                   hashFile
                   "-a"
                   "sha1"
                   "--include-hidden-files"
                   "--skip-empty-dir" |]

        Assert.Equal(0, returnCode)

        // Expect output to say matches.
        let expectedOutput = sprintf "MATCHES    /project%s" Environment.NewLine
        Assert.Equal(expectedOutput, getStdOut ())

    [<Theory>]
    [<ClassData(typeof<HashingConfigs>)>]
    member _.``check hash file matches hashing output``(inputFlags) =
        let hashingArgs =
            Array.append [| fsTempDirSetupFixture.ProjectDir |] inputFlags

        let checkArgs = Array.append [| "check"; hashFile |] inputFlags

        // Write output of hashing to hashFile.
        Assert.Equal(0, Program.main hashingArgs)
        File.WriteAllText(hashFile, getStdOut ())

        // Run program and ask to check the hashfile.
        let returnCode = Program.main checkArgs
        Assert.Equal(0, returnCode)

        // Expect output to say matches.
        let expectedOutput = sprintf "MATCHES    /project%s" Environment.NewLine
        Assert.Equal(expectedOutput, getStdOut ())

    [<Fact>]
    member _.``check auto detects md5``() =
        // Write output of hashing (md5) to hashFile.
        Assert.Equal(
            0,
            Program.main [| fsTempDirSetupFixture.ProjectDir; "-a"; "md5" |]
        )

        File.WriteAllText(hashFile, getStdOut ())

        // Run program and ask to check the hashfile without specifying algorithm.
        let returnCode = Program.main [| "check"; hashFile |]

        // Expect output to say matches.
        let expectedOutput = sprintf "MATCHES    /project%s" Environment.NewLine
        Assert.Equal(expectedOutput, getStdOut ())
        Assert.Equal(0, returnCode)

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
    member _.``check hash file different verbosity levels``
        (
            verbosityLevel,
            matchOutput,
            diffOutput,
            errorOutput
        ) =
        // Setup to ensure 1 match, diff and missing result.
        File.WriteAllText(
            Path.Combine(fsTempDirSetupFixture.TempDir, "match.txt"),
            "match"
        )

        File.WriteAllText(
            Path.Combine(fsTempDirSetupFixture.TempDir, "diff.txt"),
            "diff"
        )

        let hashFileContent =
            "\
            ef5c844eab88bcaca779bd2f3ad67b573bbbbfca  match.txt\n\
            1111570418dba2f4589c8972b9cfe4bb9e5caaaa  diff.txt\n\
            1111570418dba2f4589c8972b9cfe4bb9e5caaaa  missing.txt"

        File.WriteAllText(hashFile, hashFileContent)

        // Run program and ask to check the hashfile.
        let returnCode =
            Program.main
                [| "check"
                   hashFile
                   "-a"
                   "sha1"
                   "--verbosity"
                   verbosityLevel |]

        // Expect return code 1 (for missing hash item) and result messages.
        Assert.Equal(2, returnCode)
        let checkOutput = getStdOut ()
        Assert.Contains(matchOutput, checkOutput)
        Assert.Contains(diffOutput, checkOutput)
        Assert.Contains(errorOutput, checkOutput)

        if verbosityLevel = "quiet" then
            Assert.Equal("", checkOutput)

    [<Fact>]
    member _.``error when checking missing hash file``() =
        // Use hashfile which doesn't exist.
        let nonExistHashFile = hashFile + "z"

        // Run program and ask to check the hashfile.
        let returnCode = Program.main [| "check"; nonExistHashFile |]

        // Expect return code 1 (for missing input) and error message.
        Assert.Equal(1, returnCode)

        let expectedOutput =
            sprintf
                "Error: '%s' is not a valid hash file%s"
                nonExistHashFile
                Environment.NewLine

        Assert.Equal(expectedOutput, getStdOut ())

    [<Fact>]
    member _.``save hash files correctly``() =
        // Run hashdir and save hash file.
        let returnCode =
            Program.main
                [| fsTempDirSetupFixture.TopFileA
                   fsTempDirSetupFixture.ProjectDir
                   "--save" |]

        Assert.Equal(0, returnCode)

        // Expect saved hash files with correct hash.
        let projectHashFile =
            Path.Join(fsTempDirSetupFixture.TempDir, "project.1.sha1.txt")

        Assert.True(File.Exists(projectHashFile))

        Assert.Equal(
            "264aba9860d3dc213423759991dad98259bbf0c5  /project\n",
            File.ReadAllText(projectHashFile)
        )

        let topAHashFile =
            Path.Join(fsTempDirSetupFixture.TempDir, "topA.txt.1.sha1.txt")

        Assert.True(File.Exists(topAHashFile))

        Assert.Equal(
            "80c7fac7855e00074c94782d5d85076981be0115  topA.txt\n",
            File.ReadAllText(topAHashFile)
        )

        // Cleanup
        File.Delete(projectHashFile)
        File.Delete(topAHashFile)

    [<Fact>]
    member _.``save hash files with correct id``() =
        let getHashFilePath id =
            Path.Join(
                fsTempDirSetupFixture.TempDir,
                sprintf "topA.txt.%s.sha1.txt" (id.ToString())
            )

        // Run hashdir multiple times.
        Program.main [| fsTempDirSetupFixture.TopFileA; "--save" |] |> ignore
        Program.main [| fsTempDirSetupFixture.TopFileA; "--save" |] |> ignore
        File.WriteAllText(getHashFilePath 42, "something")
        File.WriteAllText(getHashFilePath "xyz", "something")
        Program.main [| fsTempDirSetupFixture.TopFileA; "--save" |] |> ignore

        // Expect multiple hash files with correct id.
        Assert.True(File.Exists(getHashFilePath 1))
        Assert.True(File.Exists(getHashFilePath 2))
        Assert.True(File.Exists(getHashFilePath 43))

        // Cleanup
        File.Delete(getHashFilePath 1)
        File.Delete(getHashFilePath 2)
        File.Delete(getHashFilePath 42)
        File.Delete(getHashFilePath "xyz")
        File.Delete(getHashFilePath 43)
