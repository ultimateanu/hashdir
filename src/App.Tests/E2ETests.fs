module E2ETests

open System
open System.IO
open System.Runtime.InteropServices
open Xunit
open Xunit.Abstractions

type FsTempDirSetupFixture() =
    // Single temp dir which always gets cleaned up.
    let tempDir =
        Path.GetFullPath(Path.Combine(Path.GetTempPath(), "hashdir_test_" + Guid.NewGuid().ToString()))

    // SETUP
    do
        Directory.CreateDirectory(tempDir) |> ignore
        let dirProject = Path.Combine(tempDir, "project")
        Directory.CreateDirectory(dirProject) |> ignore
        File.WriteAllText(Path.Combine(dirProject, "project1.txt"), "project1")

        // Dir a has a couple of files.
        Directory.CreateDirectory(Path.Combine(dirProject, "a")) |> ignore
        File.WriteAllText(Path.Combine(dirProject, "a", "a1.txt"), "a1")
        File.WriteAllText(Path.Combine(dirProject, "a", "a2.txt"), "a2")
        File.WriteAllText(Path.Combine(dirProject, "a", "a2.txt"), "a2")

        // Dir b has a sub dir and a file within that.
        Directory.CreateDirectory(Path.Combine(dirProject, "b")) |> ignore
        Directory.CreateDirectory(Path.Combine(dirProject, "b", "bb")) |> ignore
        File.WriteAllText(Path.Combine(dirProject, "b", "bb", "b1.txt"), "b1")

        // Dir c has a hidden file.
        Directory.CreateDirectory(Path.Combine(dirProject, "c")) |> ignore
        let hiddenFilePath = Path.Combine(dirProject, "c", ".c1.txt")
        File.WriteAllText(hiddenFilePath, "c1")
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            File.SetAttributes(hiddenFilePath, FileAttributes.Hidden)

        // Dir d is empty.
        Directory.CreateDirectory(Path.Combine(dirProject, "d")) |> ignore


    // CLEANUP
    interface IDisposable with
        member _.Dispose() = Directory.Delete(tempDir, true)

    member _.TempDir = tempDir


type CheckHashfile(fsTempDirSetupFixture: FsTempDirSetupFixture, debugOutput: ITestOutputHelper) =
    let hashFile = Path.Combine(fsTempDirSetupFixture.TempDir, "project_hash.txt")
    let oldStdOut = Console.Out
    let customStdOut = new IO.StringWriter()
    let getStdOut() = customStdOut.GetStringBuilder().ToString()
    let clearStdOut() = customStdOut.GetStringBuilder().Clear()

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

    [<Fact>]
    member _.``check hash file matches hashing output``() =
        // Write output of hashing to hashFile.
        let projectDirPath = Path.Combine(fsTempDirSetupFixture.TempDir, "project")
        Assert.Equal(0, Program.main [|projectDirPath|])
        File.WriteAllText(hashFile, getStdOut())
        clearStdOut() |> ignore

        // Run program and ask to check the hashfile.
        let returnCode = Program.main [|"check"; hashFile|]
        Assert.Equal(0, returnCode)

        // Expect output to say matches.
        let expectedOutput = sprintf "MATCHES    /project%s" Environment.NewLine
        Assert.Equal(expectedOutput, getStdOut())
