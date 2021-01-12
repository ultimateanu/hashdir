#!/usr/bin/env dotnet fsi

open System
open System.Diagnostics
open System.IO

let macProfiles = [ "mac64" ]
let windowsProfiles = [ "win32"; "win64" ]
let linuxProfiles = [ "linux32"; "linux64" ]
let dotNetProfiles = [ "dotnet" ]

// Configuration
let outputProfiles = windowsProfiles @ dotNetProfiles
let versionStr = "0.1.0"

let outputDir = "release"

let runProcess cmd (args: string) =
    Console.ForegroundColor <- ConsoleColor.Yellow
    printfn "\n\nRUNNING: %s %s" cmd args
    Console.ResetColor()

    let cleanPs = Process.Start(cmd, args)
    cleanPs.WaitForExit()
    assert (cleanPs.ExitCode = 0)

let dotnet args = runProcess "dotnet" args

let buildBinary profile =
    // Build binary
    dotnet (sprintf "publish -c Release -p:PublishProfile=%s src/App/App.fsproj" profile)

    // Create published dir
    let oldProfileDir =
        Path.Join("src/App/bin/Release/net5.0/publish", profile)

    let newProfileDir =
        Path.Join(outputDir, sprintf "hashdir_%s_%s" versionStr profile)

    Directory.CreateDirectory(newProfileDir) |> ignore
    File.Copy("README.md", Path.Join(newProfileDir, "README.md"))
    File.Copy("LICENSE", Path.Join(newProfileDir, "LICENSE"))

    // TODO: copy recursively not just the files
    oldProfileDir
    |> Directory.GetFiles
    |> Array.map (fun f -> File.Copy(f, Path.Join(newProfileDir, Path.GetFileName(f))))


// Main Program
if Directory.Exists(outputDir) then
    Directory.Delete(outputDir, true)

Directory.CreateDirectory(outputDir)

dotnet "clean"

for profile in outputProfiles do
    buildBinary profile |> ignore
