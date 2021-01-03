open System
open Xunit
open Xunit.Abstractions
open System.IO
open HashUtil.FS

type FileSystem(output:ITestOutputHelper) =
    let tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "hashdir_test_" + Guid.NewGuid().ToString()))
    let rootDir = Path.Combine(tempDir, "root")
    do
        Directory.CreateDirectory(tempDir) |> ignore;
        output.WriteLine("Created " + tempDir)
        Directory.CreateDirectory(rootDir) |> ignore;
        File.WriteAllText(Path.Combine(rootDir, "shakespeare.txt"), "To be or not to be...");
        // Dir with one file
        let dirOne = Path.Combine(rootDir, "dir_one")
        Directory.CreateDirectory(dirOne) |> ignore;
        File.WriteAllText(Path.Combine(dirOne, "file1.txt"), "1");
        // Dir with two file
        let dirTwo = Path.Combine(rootDir, "dir_two")
        Directory.CreateDirectory(dirTwo) |> ignore;
        File.WriteAllText(Path.Combine(dirTwo, "file1.txt"), "1");
        File.WriteAllText(Path.Combine(dirTwo, "file2.txt"), "2");


    [<Fact>]
    let ``Check root dir hash`` () =
        // One file dir
        let oneFileDirHash = makeHashStructure false (Path.Combine(rootDir, "dir_one"))
        Assert.True(oneFileDirHash.IsSome)
        Assert.Equal(
            "e0bc614e4fd035a488619799853b075143deea596c477b8dc077e309c0fe42e9",
            getHash oneFileDirHash.Value)

        // Two file dir
        let twoFilesDirHash = makeHashStructure false (Path.Combine(rootDir, "dir_two"))
        Assert.True(twoFilesDirHash.IsSome)
        Assert.Equal(
            "33b675636da5dcc86ec847b38c08fa49ff1cace9749931e0a5d4dfdbdedd808a",
            getHash twoFilesDirHash.Value)

        // Root hashdir
        let rootHash = makeHashStructure false rootDir
        Assert.True(rootHash.IsSome)
        Assert.Equal(
            "f5b7237efb5ad6d72149bf6b10e6d035cf012d9c37700905991549d6d32d81c4",
            getHash rootHash.Value)


    interface IDisposable with
        member x.Dispose() =
            Directory.Delete(tempDir, true)
            output.WriteLine("Deleted " + tempDir)
