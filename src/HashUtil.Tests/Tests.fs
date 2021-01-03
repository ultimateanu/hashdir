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
        let outputHashStructure = makeHashStructure false rootDir
        Assert.True(outputHashStructure.IsSome)

        // Root hashdir
        let dirHash = getHash outputHashStructure.Value
        Assert.Equal("9da9449853c7cfafb7b428097cbb3aaf45587b66146bf01a1515c667f1e24707", dirHash)

        // Overall expected hash structure
        let expectedHashStructure =
            Dir(path = rootDir,
                hash = "9da9449853c7cfafb7b428097cbb3aaf45587b66146bf01a1515c667f1e24707",
                children = [
                    File(path = Path.Combine(rootDir, "shakespeare.txt"),
                         hash = "d66f4bbd3a6f998979be96cced35d44ed32226f58eb38d347ef09c5d205b6fc4")])
        Assert.Equal(expectedHashStructure, outputHashStructure.Value)


    interface IDisposable with
        member x.Dispose() =
            Directory.Delete(tempDir, true)
            output.WriteLine("Deleted " + tempDir)
