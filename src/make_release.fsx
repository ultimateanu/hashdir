#!/usr/bin/env dotnet fsi

open System
open System.Diagnostics
open System.IO
open System.IO.Compression
open System.Runtime.InteropServices

#r "System.IO.Compression.FileSystem.dll"


// Configuration ----------------------------------------------------
let versionStr = "0.1.0"
let outputDir = "release"
// ------------------------------------------------------------------

type Compression =
    | Zip
    | TarGz

type OperatingSystem =
    | MacOS
    | Windows
    | Linux
    | Any

type Architecture =
    | X86
    | X64
    | Arm
    | Arm64
    | Any

type PublishSpec =
    { Name: string
      Os: OperatingSystem
      Architecture: Architecture
      Compression: Compression }

let macProfiles =
    [ { Name = "mac64"
        Os = MacOS
        Architecture = X64
        Compression = TarGz } ]

let windowsProfiles =
    [ { Name = "win32"
        Os = Windows
        Architecture = X86
        Compression = Zip }
      { Name = "win64"
        Os = Windows
        Architecture = X64
        Compression = Zip }
      { Name = "win32arm"
        Os = Windows
        Architecture = Arm
        Compression = Zip }
      { Name = "win64arm"
        Os = Windows
        Architecture = Arm64
        Compression = Zip } ]

let linuxProfiles =
    [ { Name = "linux32"
        Os = Linux
        Architecture = X86
        Compression = TarGz }
      { Name = "linux64"
        Os = Linux
        Architecture = X64
        Compression = TarGz } ]

// TODO: add zip also?
let dotNetProfiles =
    [ { Name = "dotnet"
        Os = OperatingSystem.Any
        Architecture = Any
        Compression = TarGz } ]


let displayOsString os =
    match os with
    | MacOS -> "macOS"
    | Windows -> "Windows"
    | Linux -> "Linux"
    | OperatingSystem.Any -> "Any"

let displayArchString arch =
    match arch with
    | X86 -> "32bit"
    | X64 -> "64bit"
    | Arm -> "ARM"
    | Arm64 -> "ARM64"
    | Architecture.Any -> "Any"

let printColor str =
    Console.ForegroundColor <- ConsoleColor.Yellow
    printfn "\n\n%s" str
    Console.ResetColor()

let runProcess cmd (args: string) =
    printColor (sprintf "RUNNING: %s %s" cmd args)
    let cleanPs = Process.Start(cmd, args)
    cleanPs.WaitForExit()
    assert (cleanPs.ExitCode = 0)

let dotnet args = runProcess "dotnet" args

let buildBinary (profile: PublishSpec) =
    // Build binary
    dotnet (sprintf "publish -c Release -p:PublishProfile=%s src/App/App.fsproj" profile.Name)

    // Create published dir
    let releaseName =
        sprintf "hashdir_%s_%s_%s" versionStr (displayOsString profile.Os) (displayArchString profile.Architecture)

    let oldProfileDir =
        Path.Combine("src/App/bin/Release/net5.0/publish", profile.Name)

    let newProfileDir = Path.Combine(outputDir, releaseName)

    Directory.CreateDirectory(newProfileDir) |> ignore
    File.Copy("README.md", Path.Combine(newProfileDir, "README.md"))
    File.Copy("LICENSE", Path.Combine(newProfileDir, "LICENSE"))

    // TODO: copy recursively not just the files
    oldProfileDir
    |> Directory.GetFiles
    |> Array.map (fun f -> File.Copy(f, Path.Combine(newProfileDir, Path.GetFileName(f))))
    |> ignore

    // TODO: name dotnet correctly

    // Compress release into a single file.
    match profile.Compression with
    | Zip ->
        let zipFilename =
            Path.Combine(outputDir, sprintf "%s.zip" releaseName)

        ZipFile.CreateFromDirectory(newProfileDir, zipFilename)
    | TarGz ->
        let tarGzFilename =
            Path.Combine(outputDir, sprintf "%s.tar.gz" releaseName)

        let tarArgs =
            sprintf "-czv -C %s -f %s %s" outputDir tarGzFilename releaseName

        runProcess "tar" tarArgs


let main =
    // Create fresh output dir
    if Directory.Exists(outputDir) then
        Directory.Delete(outputDir, true)

    Directory.CreateDirectory(outputDir) |> ignore

    // Clean then build each release target
    dotnet "clean"

    let outputProfiles =
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            printColor (sprintf "\n\nBuilding Windows binaries")
            windowsProfiles
        elif RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
            printColor (sprintf "\n\nBuilding macOS binaries")
            macProfiles
        elif RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
            printColor (sprintf "\n\nBuilding Linux binaries")
            linuxProfiles
        else
            printColor (sprintf "Error: Unknown platform")
            assert (false)
            []

    for profile in outputProfiles do
        buildBinary profile |> ignore


main
