﻿module E2ETests

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
    let oldStdOut = Console.Out
    let customStdOut = new IO.StringWriter()
    let stdOutBuffer() = customStdOut.GetStringBuilder().ToString()

    // SETUP
    do
        Console.SetOut(customStdOut)

    // CLEANUP
    interface IDisposable with
        member _.Dispose() =
            Console.SetOut(oldStdOut)

    interface IClassFixture<FsTempDirSetupFixture>


    [<Fact>]
    member _.``help flag``() =
        // Run program with help flag.
        let returnCode = Program.main [|"--help"|]
        Assert.Equal(0, returnCode)

        // Expect output to say matches.
        Assert.Contains(
            "A command-line utility to checksum directories and files.",
            stdOutBuffer())


    [<Fact>]
    member _.``check hash file``() =
        // Simple hashfile with sha1 hash for project dir.
        let hashFilePath = Path.Combine(fsTempDirSetupFixture.TempDir, "project.sha1.txt")
        File.WriteAllText(hashFilePath, "264aba9860d3dc213423759991dad98259bbf0c5  /project")

        // Run program and ask to check the hashfile.
        let returnCode = Program.main [|"check"; hashFilePath|]
        Assert.Equal(0, returnCode)

        // Expect output to say matches.
        let expectedOutput = sprintf "MATCHES    /project%s" Environment.NewLine
        Assert.Equal(expectedOutput, stdOutBuffer())