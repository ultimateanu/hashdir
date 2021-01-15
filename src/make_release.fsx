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

type RuntimeIdentifier =
    // MacOS
    | Mac64
    // Windows
    | Win86
    | Win64
    | WinArm
    | WinArm64
    // Linux
    | Linux64
    | LinuxArm
    | LinuxArm64

type PublishSpec =
    { Name: string
      Rid: RuntimeIdentifier
      Os: OperatingSystem
      Architecture: Architecture
      Compression: Compression }

let macProfiles =
    [ { Name = "mac64"
        Rid = Mac64
        Os = MacOS
        Architecture = X64
        Compression = TarGz } ]

let windowsProfiles =
    [ { Name = "win32"
        Rid = Win86
        Os = Windows
        Architecture = X86
        Compression = Zip }
      { Name = "win64"
        Rid = Win64
        Os = Windows
        Architecture = X64
        Compression = Zip }
      { Name = "win32arm"
        Rid = WinArm
        Os = Windows
        Architecture = Arm
        Compression = Zip }
      { Name = "win64arm"
        Rid = WinArm64
        Os = Windows
        Architecture = Arm64
        Compression = Zip } ]

let linuxProfiles =
    [ { Name = "Linux64"
        Rid = Linux64
        Os = Linux
        Architecture = X64
        Compression = TarGz }
      { Name = "LinuxArm"
        Rid = LinuxArm
        Os = Linux
        Architecture = Arm
        Compression = TarGz }
      { Name = "LinuxArm64"
        Rid = LinuxArm64
        Os = Linux
        Architecture = Arm64
        Compression = TarGz } ]

// TODO: add zip also?
let dotNetProfiles =
    [ { Name = "dotnet"
        Rid = Win86
        Os = OperatingSystem.Any
        Architecture = Any
        Compression = TarGz } ]

let RuntimeIdentifierString id =
    match id with
    // MacOS
    | Mac64 -> "osx-x64"
    // Windows
    | Win86 -> "win-x86"
    | Win64 -> "win-x64"
    | WinArm -> "win-arm"
    | WinArm64 -> "win-arm64"
    // Linux
    | Linux64 -> "linux-x64"
    | LinuxArm -> "linux-arm"
    | LinuxArm64 -> "linux-arm64"

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

let printColor color str =
    Console.ForegroundColor <- color
    printfn "\n\n%s" str
    Console.ResetColor()

let ensure exp msg =
    if not exp then
        printColor ConsoleColor.Red (sprintf "ERROR: %s" msg)
        Environment.Exit(1)

let runProcess cmd (args: string) =
    printColor ConsoleColor.Yellow (sprintf "RUNNING: %s %s" cmd args)
    let cleanPs = Process.Start(cmd, args)
    cleanPs.WaitForExit()
    assert (cleanPs.ExitCode = 0)

let dotnet args = runProcess "dotnet" args

let buildBinary (profile: PublishSpec) =
    // Build binary
    dotnet (sprintf "publish -c Release -p:PublishProfile=binary -p:RuntimeIdentifier=%s src/App/App.fsproj"
        (RuntimeIdentifierString profile.Rid))

    // Create published dir
    let releaseName =
        sprintf "hashdir_%s_%s_%s" versionStr (displayOsString profile.Os) (displayArchString profile.Architecture)

    let oldProfileDir = "src/App/bin/Release/net5.0/publish/binary"

    let newProfileDir = Path.Combine(outputDir, releaseName)

    Directory.CreateDirectory(newProfileDir) |> ignore
    File.Copy("README.md", Path.Combine(newProfileDir, "README.md"))
    File.Copy("LICENSE", Path.Combine(newProfileDir, "LICENSE"))

    // TODO: copy recursively not just the files
    let releaseFiles = Directory.GetFiles oldProfileDir

    // Expect only a single binary
    ensure (1 = Array.length releaseFiles) "Expected a single binary file."

    releaseFiles
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



let makeNuGetRelease () =
    dotnet "pack -c Release ./src/App/App.fsproj"
    let nugetOutputFiles = Directory.GetFiles "src/App/nupkg"
    ensure (1 = Array.length nugetOutputFiles) "Expected a single file for NuGet output."
    let nugetPackagePath = Array.head nugetOutputFiles
    File.Copy(nugetPackagePath, Path.Combine(outputDir, Path.GetFileName(nugetPackagePath)))


let main =
    // Create fresh output dir
    if Directory.Exists(outputDir) then
        Directory.Delete(outputDir, true)

    Directory.CreateDirectory(outputDir) |> ignore

    // Clean then build each release target
    dotnet "clean"

    let outputProfiles =
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            printColor ConsoleColor.Yellow (sprintf "\n\nBuilding Windows binaries")
            windowsProfiles
        elif RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
            printColor ConsoleColor.Yellow (sprintf "\n\nBuilding macOS binaries")
            macProfiles
        elif RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
            printColor ConsoleColor.Yellow (sprintf "\n\nBuilding Linux binaries")
            linuxProfiles
        else
            printColor ConsoleColor.Red (sprintf "Error: Unknown platform")
            assert (false)
            []

    for profile in outputProfiles do
        buildBinary profile |> ignore

    // Make NuGet package
    makeNuGetRelease ()


main
