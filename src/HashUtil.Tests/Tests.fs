open HashUtil.Checksum
open HashUtil.FS
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
    let makeOption x =
        match x with
            | Error _ -> None
            | Ok v -> Some(v)

    interface IClassFixture<FsSetupFixture>

    [<Fact>]
    member _.``Dir with 0 files (include empty dir)`` () =
        let includeEmptyDir = true
        let oneFileDirHash =
            Path.Combine(fsSetupFixture.RootDir, "dir_zero")
                |> makeHashStructure SHA256 false includeEmptyDir
                |> makeOption
        Assert.True(oneFileDirHash.IsSome)
        Assert.Equal(
            "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            getHash oneFileDirHash.Value)
        output.WriteLine("a root:" + fsSetupFixture.RootDir)

    [<Fact>]
    member _.``Dir with 0 files (exclude empty dir)`` () =
        let includeEmptyDir = false
        let oneFileDirHash =
            Path.Combine(fsSetupFixture.RootDir, "dir_zero")
                |> makeHashStructure SHA256 false includeEmptyDir
                |> makeOption
        Assert.True(oneFileDirHash.IsNone)

    [<Fact>]
    member _.``Dir with 1 file`` () =
        let oneFileDirHash =
            Path.Combine(fsSetupFixture.RootDir, "dir_one")
                |> makeHashStructure SHA256 false false
                |> makeOption
        Assert.True(oneFileDirHash.IsSome)
        Assert.Equal(
            "c0b9c17c8ac302513644256d06d1518a50c0c349e28023c2795a17dfa5479e1f",
            getHash oneFileDirHash.Value)

    [<Fact>]
    member _.``Dir with 2 files`` () =
        let twoFileDirHash =
            Path.Combine(fsSetupFixture.RootDir, "dir_two")
                |> makeHashStructure SHA256 false false
                |> makeOption
        Assert.True(twoFileDirHash.IsSome)
        Assert.Equal(
            "072d85c3b6926317ee8c340d4e989c9588c75408e63b5674571624a096faf9b5",
            getHash twoFileDirHash.Value)

    [<Fact>]
    member _.``Empty file (include)`` () =
        let includeHiddenFiles = true
        let includeEmptyDir = true
        let rootHash =
            Path.Combine(fsSetupFixture.RootDir, ".fakerc")
                |> makeHashStructure SHA256 includeHiddenFiles includeEmptyDir
                |> makeOption
        Assert.True(rootHash.IsSome)
        Assert.Equal(
            "b79606fb3afea5bd1609ed40b622142f1c98125abcfe89a76a661b0e8e343910",
            getHash rootHash.Value)

    [<Fact>]
    member _.``Empty file (exclude)`` () =
        let includeHiddenFiles = false
        let includeEmptyDir = true
        let rootHash =
            Path.Combine(fsSetupFixture.RootDir, ".fakerc")
                |> makeHashStructure SHA256 includeHiddenFiles includeEmptyDir
                |> makeOption
        Assert.True(rootHash.IsNone)

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
