#!/usr/bin/env dotnet fsi

open System
open System.Diagnostics
open System.IO
open System.IO.Compression
open System.Runtime.InteropServices
#r "System.IO.Compression.FileSystem.dll"


// Configuration ----------------------------------------------------
let versionStr = "0.1.0"
// ------------------------------------------------------------------

type Compression = Zip | TarGz
type OperatingSystem = MacOS | Windows | Linux | Any
type Architecture = X86 | X64 | Arm | Arm64 | Any
type PublishSpec = { name: string;
                     os: OperatingSystem;
                     architecture: Architecture;
                     compression: Compression }
let macProfiles = [
    {name="mac64"; os=MacOS; architecture=X64; compression=TarGz}]
let windowsProfiles = [
    {name="win32"; os=Windows; architecture=X86; compression=Zip};
    {name="win64"; os=Windows; architecture=X64; compression=Zip};
    {name="win32arm"; os=Windows; architecture=Arm; compression=Zip};
    {name="win64arm"; os=Windows; architecture=Arm64; compression=Zip}]
let linuxProfiles = [
    {name="linux32"; os=Linux; architecture=X86; compression=TarGz};
    {name="linux64"; os=Linux; architecture=X64; compression=TarGz}]
// TODO: add zip also?
let dotNetProfiles =[
    {name="dotnet"; os=OperatingSystem.Any; architecture=Any; compression=TarGz}]
let outputDir = "release"

let displayOsString os =
    match os with
        | MacOS -> "macOS"
        | Windows -> "Windows"
        | Linux -> "Linux"
        | OperatingSystem.Any -> "Any"

let displayArchString os =
    match os with
        | X86 -> "32bit"
        | X64 -> "64bit"
        | Arm -> "ARM"
        | Arm64 -> "ARM64"
        | Architecture.Any -> "Any"

let runProcess cmd (args: string) =
    Console.ForegroundColor <- ConsoleColor.Yellow
    printfn "\n\nRUNNING: %s %s" cmd args
    Console.ResetColor()

    let cleanPs = Process.Start(cmd, args)
    cleanPs.WaitForExit()
    assert (cleanPs.ExitCode = 0)

let dotnet args = runProcess "dotnet" args

let buildBinary (profile:PublishSpec) =
    // Build binary
    dotnet (sprintf "publish -c Release -p:PublishProfile=%s src/App/App.fsproj" profile.name)

    // Create published dir
    let oldProfileDir =
        Path.Combine("src/App/bin/Release/net5.0/publish", profile.name)
    let newProfileDirName = profile.name
    let newProfileDir = Path.Combine(outputDir, newProfileDirName)

    Directory.CreateDirectory(newProfileDir) |> ignore
    File.Copy("README.md", Path.Combine(newProfileDir, "README.md"))
    File.Copy("LICENSE", Path.Combine(newProfileDir, "LICENSE"))

    // TODO: copy recursively not just the files
    oldProfileDir
    |> Directory.GetFiles
    |> Array.map (fun f -> File.Copy(f, Path.Combine(newProfileDir, Path.GetFileName(f))))
    |> ignore

    // Compress folder
    // TODO: to tar.gz etc.
    // TODO: name dotnet correctly
    let zipFilename =
        sprintf "hashdir_%s_%s_%s.zip"
            versionStr
            (displayOsString profile.os)
            (displayArchString profile.architecture)
    ZipFile.CreateFromDirectory(newProfileDir, Path.Combine(outputDir, zipFilename))


let main =
    // Create fresh output dir
    if Directory.Exists(outputDir) then
        Directory.Delete(outputDir, true)
    Directory.CreateDirectory(outputDir) |> ignore

    // Clean then build each release target
    dotnet "clean"

    let outputProfilesz =
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            printf "Building Windows binaries"
            windowsProfiles
        elif RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
            printf "Building macOS binaries"
            macProfiles
        elif RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
            printf "Building Linux binaries"
            macProfiles
        else
            printf "Error: Unknown platform"
            assert(false)
            []


    for profile in outputProfilesz do
        buildBinary profile |> ignore

main
