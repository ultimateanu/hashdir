#!/usr/bin/env dotnet fsi

#r "System.IO.Compression.FileSystem.dll"
#r "System.Security.Cryptography.dll"
#load "HashUtil/Util.fs"

open System
open System.Diagnostics
open System.IO
open System.IO.Compression
open System.Runtime.InteropServices
open System.Security.Cryptography
open HashUtil.Util

// Configuration ----------------------------------------------------
let versionStr = "1.3.1"
// ------------------------------------------------------------------

let releaseDir = "release"
let binDir = "src/App/bin"
let nameAndVersion = sprintf "hashdir_%s" versionStr

type Compression =
    | Zip
    | TarGz

type RuntimeIdentifier =
    // MacOS
    | Mac64
    | MacArm
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
      Compression: Compression }

let macProfiles =
    [ { Name = "macOS_64bit"
        Rid = Mac64
        Compression = TarGz }
      { Name = "macOS_ARM"
        Rid = MacArm
        Compression = TarGz } ]

let windowsProfiles =
    [ { Name = "Windows_32bit"
        Rid = Win86
        Compression = Zip }
      { Name = "Windows_64bit"
        Rid = Win64
        Compression = Zip }
      { Name = "Windows_ARM"
        Rid = WinArm
        Compression = Zip }
      { Name = "Windows_ARM64"
        Rid = WinArm64
        Compression = Zip } ]

let linuxProfiles =
    [ { Name = "Linux_64bit"
        Rid = Linux64
        Compression = TarGz }
      { Name = "Linux_ARM"
        Rid = LinuxArm
        Compression = TarGz }
      { Name = "Linux_ARM64"
        Rid = LinuxArm64
        Compression = TarGz } ]

let RuntimeIdentifierString id =
    match id with
    // MacOS
    | Mac64 -> "osx-x64"
    | MacArm -> "osx-arm64"
    // Windows
    | Win86 -> "win-x86"
    | Win64 -> "win-x64"
    | WinArm -> "win-arm"
    | WinArm64 -> "win-arm64"
    // Linux
    | Linux64 -> "linux-x64"
    | LinuxArm -> "linux-arm"
    | LinuxArm64 -> "linux-arm64"

let printColor color str =
    Console.ForegroundColor <- color
    printfn "\n\n%s" str
    Console.ResetColor()

let ensure exp msg =
    if not exp then
        printColor ConsoleColor.Red (sprintf "ERROR: %s" msg)
        exit 1

let runProcess cmd (args: string) =
    printColor ConsoleColor.Yellow (sprintf "RUNNING: %s %s" cmd args)
    let cleanPs = Process.Start(cmd, args)
    cleanPs.WaitForExit()
    ensure (cleanPs.ExitCode = 0) "Process not successful"

let dotnet args = runProcess "dotnet" args

let compressDir compression releaseName =
    match compression with
    | Zip ->
        let zipFilename =
            Path.Combine(releaseDir, sprintf "%s.zip" releaseName)

        ZipFile.CreateFromDirectory(Path.Combine(releaseDir, releaseName), zipFilename)
    | TarGz ->
        let tarGzFilename =
            Path.Combine(releaseDir, sprintf "%s.tar.gz" releaseName)

        let tarArgs =
            sprintf "-czv -C %s -f %s %s" releaseDir tarGzFilename releaseName

        runProcess "tar" tarArgs

let buildSingleBinary (profile: PublishSpec) =
    // Build binary
    dotnet (
        sprintf
            "publish -c Release --framework net8.0 -p:PublishProfile=binary -p:RuntimeIdentifier=%s src/App/App.fsproj"
            (RuntimeIdentifierString profile.Rid)
    )

    // Create published dir
    let releaseName = sprintf "%s_%s" nameAndVersion profile.Name
    let binaryPath = sprintf "src/App/bin/Release/net8.0/%s/hashdir" (RuntimeIdentifierString profile.Rid)
    let newProfileDir = Path.Combine(releaseDir, releaseName)

    Directory.CreateDirectory(newProfileDir) |> ignore
    File.Copy("README.md", Path.Combine(newProfileDir, "README.md"))
    File.Copy("LICENSE", Path.Combine(newProfileDir, "LICENSE"))
    File.Copy(binaryPath, Path.Combine(newProfileDir, "hashdir"))

    // Compress release into a single file.
    compressDir profile.Compression releaseName

let makeNuGetRelease () =
    dotnet "pack -c Release ./src/App/App.fsproj"
    let nugetOutputFiles = Directory.GetFiles "src/App/nupkg"
    ensure (1 = Array.length nugetOutputFiles) "Expected a single file for NuGet output."
    let nugetPackagePath = Array.head nugetOutputFiles
    File.Copy(nugetPackagePath, Path.Combine(releaseDir, Path.GetFileName(nugetPackagePath)))

let makeDotnetRelease () =
    dotnet "publish -c Release --framework net8.0 -p:PublishProfile=dotnet src/App/App.fsproj"
    let releaseName = sprintf "%s_dotnet" nameAndVersion
    Directory.Move("src/App/bin/Release/net7.0/publish/dotnet", Path.Combine(releaseDir, releaseName))
    compressDir Zip releaseName
    compressDir TarGz releaseName

let buildRelease () =
    // Create fresh output dir
    if Directory.Exists(releaseDir) then
        Directory.Delete(releaseDir, true)

    Directory.CreateDirectory(releaseDir) |> ignore

    dotnet "clean"

    if Directory.Exists(binDir) then
        Directory.Delete(binDir, true)

    // Build single binaries for current platform.
    let binaryProfiles =
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

    for profile in binaryProfiles do
        buildSingleBinary profile |> ignore

    // Make cross platform outputs on macOS.
    if RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
        // makeNuGetRelease ()
        makeDotnetRelease ()

let makeChecksumFile () =
    let hashAlg = SHA256.Create()

    let checksumFilename =
        Path.Combine(releaseDir, sprintf "%s_checksums_sha256.txt" nameAndVersion)

    // Delete old checksum file.
    if File.Exists checksumFilename then
        File.Delete checksumFilename

    let hashLines =
        Directory.GetFiles(releaseDir)
        |> Array.filter (fun f -> f.StartsWith(Path.Combine(releaseDir, "hashdir_")))
        |> Array.sort
        |> Array.map (fun f -> sprintf "%s  %s" (computeHashOfFile hashAlg f) (Path.GetFileName f))

    File.WriteAllLines(checksumFilename, hashLines)

let main () =
    let argErrorMsg =
        "Expected a single argument: <build | hash>"

    ensure (fsi.CommandLineArgs.Length = 2) argErrorMsg

    match fsi.CommandLineArgs.[1] with
    | "build" -> buildRelease ()
    | "hash" -> makeChecksumFile ()
    | _ -> ensure false argErrorMsg


main ()
