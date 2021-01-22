module FsTests

open HashUtil.FS
open HashUtil.Util
open System
open System.IO
open Xunit
open Xunit.Abstractions

type FsTempDirSetupFixture() =
    // Single temp dir which always gets cleaned up.
    let tempDir =
        Path.GetFullPath(Path.Combine(Path.GetTempPath(), "hashdir_test_" + Guid.NewGuid().ToString()))

    do Directory.CreateDirectory(tempDir) |> ignore

    member _.TempDir = tempDir

    // Clean up temp dir when finished.
    interface IDisposable with
        member this.Dispose() =
            Directory.Delete(tempDir, true)
            ()


type FilenameInHash(fsTempDirSetupFixture: FsTempDirSetupFixture, output: ITestOutputHelper) =
    // Create root dir for each test.
    let rootDir =
        Path.Combine(fsTempDirSetupFixture.TempDir, "filename_in_hash_root_dir")

    do Directory.CreateDirectory(rootDir) |> ignore

    // Clean up root dir after each test.
    interface IDisposable with
        member this.Dispose() = Directory.Delete(rootDir, true)

    interface IClassFixture<FsTempDirSetupFixture>

    [<Fact>]
    member _.``Dir hashs same when child files/dirs have same names & content``() =
        // Setup two directories with files/dirs of same name and content.
        let dirA = Path.Combine(rootDir, "dir_a")
        let dirAInternal = Path.Combine(dirA, "internal")
        let dirB = Path.Combine(rootDir, "dir_b")
        let dirBInternal = Path.Combine(dirB, "internal")
        Directory.CreateDirectory(dirA) |> ignore
        Directory.CreateDirectory(dirAInternal) |> ignore
        Directory.CreateDirectory(dirB) |> ignore
        Directory.CreateDirectory(dirBInternal) |> ignore
        File.WriteAllText(Path.Combine(dirA, "file_a.txt"), "content_top")
        File.WriteAllText(Path.Combine(dirB, "file_a.txt"), "content_top")
        File.WriteAllText(Path.Combine(dirAInternal, "file.txt"), "content_internal")
        File.WriteAllText(Path.Combine(dirBInternal, "file.txt"), "content_internal")

        // Compute the hash of the two directories.
        let includeHiddenFiles = true
        let includeEmptyDir = true

        let dirAHash =
            dirA
                |> makeHashStructure includeHiddenFiles includeEmptyDir
                |> makeOption

        let dirBHash =
            dirB
                |> makeHashStructure includeHiddenFiles includeEmptyDir
                |> makeOption

        // Expect their hashes to be equal.
        Assert.Equal(getHash dirAHash.Value, getHash dirBHash.Value)

    [<Fact>]
    member _.``Dir hashs differ when child files have different names``() =
        // Setup two directories with files of same content but different names.
        let dirA = Path.Combine(rootDir, "dir_a")
        let dirAInternal = Path.Combine(dirA, "internal")
        let dirB = Path.Combine(rootDir, "dir_b")
        let dirBInternal = Path.Combine(dirB, "internal")
        Directory.CreateDirectory(dirA) |> ignore
        Directory.CreateDirectory(dirAInternal) |> ignore
        Directory.CreateDirectory(dirB) |> ignore
        Directory.CreateDirectory(dirBInternal) |> ignore
        File.WriteAllText(Path.Combine(dirA, "file_a.txt"), "content_top")
        File.WriteAllText(Path.Combine(dirB, "file_b.txt"), "content_top")
        File.WriteAllText(Path.Combine(dirAInternal, "file.txt"), "content_internal")
        File.WriteAllText(Path.Combine(dirBInternal, "file.txt"), "content_internal")

        // Compute the hash of the two directories.
        let includeHiddenFiles = true
        let includeEmptyDir = true

        let dirAHash =
            dirA
                |> makeHashStructure includeHiddenFiles includeEmptyDir
                |> makeOption

        let dirBHash =
            dirB
                |> makeHashStructure includeHiddenFiles includeEmptyDir
                |> makeOption

        // Expect their hashes to be different.
        Assert.NotEqual<string>(getHash dirAHash.Value, getHash dirBHash.Value)

    [<Fact>]
    member _.``Dir hashs differ when child dirs have different names``() =
        // Setup two directories with child dirs of same content but different names.
        let dirA = Path.Combine(rootDir, "dir_a")
        let dirAInternal = Path.Combine(dirA, "internal_a")
        let dirB = Path.Combine(rootDir, "dir_b")
        let dirBInternal = Path.Combine(dirB, "internal_b")
        Directory.CreateDirectory(dirA) |> ignore
        Directory.CreateDirectory(dirAInternal) |> ignore
        Directory.CreateDirectory(dirB) |> ignore
        Directory.CreateDirectory(dirBInternal) |> ignore
        File.WriteAllText(Path.Combine(dirA, "file_root.txt"), "content_top")
        File.WriteAllText(Path.Combine(dirB, "file_root.txt"), "content_top")
        File.WriteAllText(Path.Combine(dirAInternal, "file.txt"), "content_internal")
        File.WriteAllText(Path.Combine(dirBInternal, "file.txt"), "content_internal")

        // Compute the hash of the two directories.
        let includeHiddenFiles = true
        let includeEmptyDir = true

        let dirAHash =
            dirA
                |> makeHashStructure includeHiddenFiles includeEmptyDir
                |> makeOption

        let dirBHash =
            dirB
                |> makeHashStructure includeHiddenFiles includeEmptyDir
                |> makeOption

        // Expect their hashes to be different.
        Assert.NotEqual<string>(getHash dirAHash.Value, getHash dirBHash.Value)


type HashProperties(fsTempDirSetupFixture: FsTempDirSetupFixture, output: ITestOutputHelper) =
    // Create root dir for each test.
    let rootDir =
        Path.Combine(fsTempDirSetupFixture.TempDir, "filename_in_hash_root_dir")

    do Directory.CreateDirectory(rootDir) |> ignore

    // Clean up root dir after each test.
    interface IDisposable with
        member this.Dispose() = Directory.Delete(rootDir, true)

    interface IClassFixture<FsTempDirSetupFixture>

    [<Fact>]
    member _.``Empty dir and empty file should have same hash``() =
        // Setup two directories with files/dirs of same name and content.
        let dirPath = Path.Combine(rootDir, "empty_dir")
        let filePath = Path.Combine(rootDir, "empty_file.txt")
        Directory.CreateDirectory(dirPath) |> ignore
        File.WriteAllText(filePath, "")

        // Compute the hash of empty dir and empty file.
        let includeHiddenFiles = true
        let includeEmptyDir = true

        let dirHash =
            dirPath
                |> makeHashStructure includeHiddenFiles includeEmptyDir
                |> makeOption

        let fileHash =
            filePath
                |> makeHashStructure includeHiddenFiles includeEmptyDir
                |> makeOption

        // Expect their hashes to be equal.
        Assert.True(dirHash.IsSome)
        Assert.True(fileHash.IsSome)
        Assert.Equal(getHash dirHash.Value, getHash fileHash.Value)
