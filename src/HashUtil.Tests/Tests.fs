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


    [<Fact>]
    let ``Check root dir hash`` () =
        let optHashStructure = makeHashStructure false rootDir
        Assert.True(optHashStructure.IsSome)
        let dirHash = getHash optHashStructure.Value
        Assert.Equal("9da9449853c7cfafb7b428097cbb3aaf45587b66146bf01a1515c667f1e24707", dirHash)


    interface IDisposable with
        member x.Dispose() =
            Directory.Delete(tempDir, true)
            output.WriteLine("Deleted " + tempDir)
