open HashUtil.Checksum
open HashUtil.FS
open HashUtil.Util
open System
open System.IO
open System.Runtime.InteropServices
open Xunit
open Xunit.Abstractions

type FsSetupFixture() =
    let tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "hashdir_test_" + Guid.NewGuid().ToString()))
    let rootDir = Path.Combine(tempDir, "root")
    do
        Directory.CreateDirectory(tempDir) |> ignore;
        Directory.CreateDirectory(rootDir) |> ignore;
        File.WriteAllText(Path.Combine(rootDir, "shakespeare.txt"), "To be or not to be...");
        // Hidden file
        let hiddenFilePath = Path.Combine(rootDir, ".fakerc")
        File.WriteAllText(Path.Combine(rootDir, ".fakerc"), "config");
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            File.SetAttributes(hiddenFilePath, FileAttributes.Hidden);
        // Dir with 0 files
        Directory.CreateDirectory(Path.Combine(rootDir, "dir_zero")) |> ignore;
        // Dir with 1 file
        let dirOne = Path.Combine(rootDir, "dir_one")
        Directory.CreateDirectory(dirOne) |> ignore;
        File.WriteAllText(Path.Combine(dirOne, "file1.txt"), "1");
        // Dir with 2 files
        let dirTwo = Path.Combine(rootDir, "dir_two")
        Directory.CreateDirectory(dirTwo) |> ignore;
        File.WriteAllText(Path.Combine(dirTwo, "file1.txt"), "1");
        File.WriteAllText(Path.Combine(dirTwo, "file2.txt"), "2");

    member _.RootDir = rootDir

    // Clean up temp dir when finished.
    interface IDisposable with
        member this.Dispose() =
            Directory.Delete(tempDir, true)
            ()


type HashStructureTests(fsSetupFixture: FsSetupFixture, output:ITestOutputHelper) =
    interface IClassFixture<FsSetupFixture>

    [<Fact>]
    member _.``Root hash (include empty dir)`` () =
        let includeEmptyDir = true
        let rootHash =
            fsSetupFixture.RootDir
                |> makeHashStructure SHA256 false includeEmptyDir
                |> makeOption
        Assert.True(rootHash.IsSome)
        Assert.Equal(
            "e8bc90f811e969cc4f4f119057b5bda6b4269ede9b54c6358ff49d0b32e3b55f",
            getHash rootHash.Value)

    [<Fact>]
    member _.``Root hash (exclude empty dir)`` () =
        let includeEmptyDir = false
        let rootHash =
            fsSetupFixture.RootDir
                |> makeHashStructure SHA256 false includeEmptyDir
                |> makeOption
        Assert.True(rootHash.IsSome)
        Assert.Equal(
            "115c16b7fc0e1af49b0c646d982afdcf2bdb32ad28e1dac335e64a3d0e023aed",
            getHash rootHash.Value)

    [<Fact>]
    member _.``Root hash (include hidden file)`` () =
        let includeHiddenFiles = true
        let includeEmptyDir = true
        let rootHash =
            fsSetupFixture.RootDir
                |> makeHashStructure SHA256 includeHiddenFiles includeEmptyDir
                |> makeOption
        Assert.True(rootHash.IsSome)
        Assert.Equal(
            "f878c04d1538053df32fe42f626211e6d02515ef453e3286ed4c8ed2405efe2e",
            getHash rootHash.Value)

    [<Fact>]
    member _.``Root hash (exclude hidden file)`` () =
        let includeHiddenFiles = false
        let includeEmptyDir = true
        let rootHash =
            fsSetupFixture.RootDir
                |> makeHashStructure SHA256 includeHiddenFiles includeEmptyDir
                |> makeOption
        Assert.True(rootHash.IsSome)
        Assert.Equal(
            "e8bc90f811e969cc4f4f119057b5bda6b4269ede9b54c6358ff49d0b32e3b55f",
            getHash rootHash.Value)
