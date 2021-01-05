open System
open Xunit
open Xunit.Abstractions
open System.IO
open HashUtil.FS

type FsSetupFixture() =
    let tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "hashdir_test_" + Guid.NewGuid().ToString()))
    let rootDir = Path.Combine(tempDir, "root")
    do
        Directory.CreateDirectory(tempDir) |> ignore;
        Directory.CreateDirectory(rootDir) |> ignore;
        File.WriteAllText(Path.Combine(rootDir, "shakespeare.txt"), "To be or not to be...");
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
                |> makeHashStructure false includeEmptyDir
                |> makeOption
        Assert.True(oneFileDirHash.IsSome)
        Assert.Equal(
            "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            getHash oneFileDirHash.Value)

    [<Fact>]
    member _.``Dir with 0 files (exclude empty dir)`` () =
        let includeEmptyDir = false
        let oneFileDirHash =
            Path.Combine(fsSetupFixture.RootDir, "dir_zero")
                |> makeHashStructure false includeEmptyDir
                |> makeOption
        Assert.True(oneFileDirHash.IsNone)

    [<Fact>]
    member _.``Dir with 1 file`` () =
        let oneFileDirHash =
            Path.Combine(fsSetupFixture.RootDir, "dir_one")
                |> makeHashStructure false false
                |> makeOption
        Assert.True(oneFileDirHash.IsSome)
        Assert.Equal(
            "e0bc614e4fd035a488619799853b075143deea596c477b8dc077e309c0fe42e9",
            getHash oneFileDirHash.Value)

    [<Fact>]
    member _.``Dir with 2 files`` () =
        let twoFileDirHash =
            Path.Combine(fsSetupFixture.RootDir, "dir_two")
                |> makeHashStructure false false
                |> makeOption
        Assert.True(twoFileDirHash.IsSome)
        Assert.Equal(
            "33b675636da5dcc86ec847b38c08fa49ff1cace9749931e0a5d4dfdbdedd808a",
            getHash twoFileDirHash.Value)

    [<Fact>]
    member _.``Root hash (include empty dir)`` () =
        let includeEmptyDir = true
        let rootHash =
            fsSetupFixture.RootDir
                |> makeHashStructure false includeEmptyDir
                |> makeOption
        Assert.True(rootHash.IsSome)
        Assert.Equal(
            "070019945bc35bce97b3ca01630efed4f8d191b1336b78c085aa944d8a375f27",
            getHash rootHash.Value)

    [<Fact>]
    member _.``Root hash (exclude empty dir)`` () =
        let includeEmptyDir = false
        let rootHash =
            fsSetupFixture.RootDir
                |> makeHashStructure false includeEmptyDir
                |> makeOption
        Assert.True(rootHash.IsSome)
        Assert.Equal(
            "f5b7237efb5ad6d72149bf6b10e6d035cf012d9c37700905991549d6d32d81c4",
            getHash rootHash.Value)
