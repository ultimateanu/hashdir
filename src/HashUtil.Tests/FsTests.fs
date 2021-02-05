module FsTests

open HashUtil.Checksum
open HashUtil.FS
open HashUtil.Util
open System
open System.IO
open System.Runtime.InteropServices
open Xunit
open Xunit.Abstractions

type FsTempDirSetupFixture() =
    // Single temp dir which always gets cleaned up.
    let tempDir =
        Path.GetFullPath(Path.Combine(Path.GetTempPath(), "hashdir_test_" + Guid.NewGuid().ToString()))

    // SETUP
    do Directory.CreateDirectory(tempDir) |> ignore

    // CLEANUP
    interface IDisposable with
        member this.Dispose() =
            Directory.Delete(tempDir, true)

    member _.TempDir = tempDir


type FilenameInHash(fsTempDirSetupFixture: FsTempDirSetupFixture, output: ITestOutputHelper) =
    // Create root dir for each test.
    let rootDir =
        Path.Combine(fsTempDirSetupFixture.TempDir, "filename_in_hash_root_dir")

    // SETUP
    do Directory.CreateDirectory(rootDir) |> ignore

    // CLEANUP
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
            |> makeHashStructure HashType.SHA256 includeHiddenFiles includeEmptyDir
            |> makeOption

        let dirBHash =
            dirB
            |> makeHashStructure HashType.SHA256 includeHiddenFiles includeEmptyDir
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
            |> makeHashStructure HashType.SHA256 includeHiddenFiles includeEmptyDir
            |> makeOption

        let dirBHash =
            dirB
            |> makeHashStructure HashType.SHA256 includeHiddenFiles includeEmptyDir
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
            |> makeHashStructure HashType.SHA256 includeHiddenFiles includeEmptyDir
            |> makeOption

        let dirBHash =
            dirB
            |> makeHashStructure HashType.SHA256 includeHiddenFiles includeEmptyDir
            |> makeOption

        // Expect their hashes to be different.
        Assert.NotEqual<string>(getHash dirAHash.Value, getHash dirBHash.Value)


type HashProperties(fsTempDirSetupFixture: FsTempDirSetupFixture, output: ITestOutputHelper) =
    // Create root dir for each test.
    let rootDir =
        Path.Combine(fsTempDirSetupFixture.TempDir, "hash_properties")

    // SETUP
    do Directory.CreateDirectory(rootDir) |> ignore

    // CLEANUP
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
            |> makeHashStructure HashType.SHA256 includeHiddenFiles includeEmptyDir
            |> makeOption

        let fileHash =
            filePath
            |> makeHashStructure HashType.SHA256 includeHiddenFiles includeEmptyDir
            |> makeOption

        // Expect their hashes to be equal.
        Assert.True(dirHash.IsSome)
        Assert.True(fileHash.IsSome)
        Assert.Equal(getHash dirHash.Value, getHash fileHash.Value)


type FileHashes(fsTempDirSetupFixture: FsTempDirSetupFixture, output: ITestOutputHelper) =
    let hiddenFilePath = Path.Combine(fsTempDirSetupFixture.TempDir, ".fakerc")

    // SETUP
    do
        File.WriteAllText(hiddenFilePath, "config");
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            File.SetAttributes(hiddenFilePath, FileAttributes.Hidden);

    // CLEANUP
    interface IDisposable with
        member this.Dispose() =
            File.Delete(hiddenFilePath)

    interface IClassFixture<FsTempDirSetupFixture>

    [<Fact>]
    member _.``Hidden file (include)`` () =
        // Compute hash (including this hidden file)
        let includeHiddenFiles = true
        let fileHash =
            hiddenFilePath
                |> makeHashStructure SHA256 includeHiddenFiles true
                |> makeOption

        // Hash should exist and match
        Assert.True(fileHash.IsSome)
        Assert.Equal(
            "b79606fb3afea5bd1609ed40b622142f1c98125abcfe89a76a661b0e8e343910",
            getHash fileHash.Value)

    [<Fact>]
    member _.``Hidden file (exclude)`` () =
        // Compute hash (excluding this hidden file)
        let includeHiddenFiles = false
        let fileHash =
            hiddenFilePath
                |> makeHashStructure SHA256 includeHiddenFiles true
                |> makeOption

        // Hash should exist and match
        Assert.True(fileHash.IsNone)


type DirHashes(fsTempDirSetupFixture: FsTempDirSetupFixture, output: ITestOutputHelper) =
    interface IClassFixture<FsTempDirSetupFixture>

    [<Fact>]
    member _.``Dir with 0 files (include empty dir)`` () =
        // Setup dir with 0 files
        let dirZero = Path.Combine(fsTempDirSetupFixture.TempDir, "dir_zero")
        Directory.CreateDirectory(dirZero) |> ignore;

        // Compute hash (including this empty dir)
        let includeEmptyDir = true
        let zeroFileDirHash =
            dirZero
                |> makeHashStructure SHA256 false includeEmptyDir
                |> makeOption

        // Hash should exist and match
        Assert.True(zeroFileDirHash.IsSome)
        Assert.Equal(
            "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            getHash zeroFileDirHash.Value)

    [<Fact>]
    member _.``Dir with 0 files (exclude empty dir)`` () =
        // Setup dir with 0 files
        let dirZero = Path.Combine(fsTempDirSetupFixture.TempDir, "dir_zero")
        Directory.CreateDirectory(dirZero) |> ignore;

        // Compute hash (excluding this empty dir)
        let includeEmptyDir = false
        let zeroFileDirHash =
            dirZero
                |> makeHashStructure SHA256 false includeEmptyDir
                |> makeOption

        // Hash should exist and match
        Assert.True(zeroFileDirHash.IsNone)

    [<Fact>]
    member _.``Dir with 1 file``() =
        // Setup dir with 1 file
        let dirOne = Path.Combine(fsTempDirSetupFixture.TempDir, "dir_one")
        Directory.CreateDirectory(dirOne) |> ignore;
        File.WriteAllText(Path.Combine(dirOne, "file1.txt"), "1");

        // Compute hash
        let oneFileDirHash =
            dirOne
                |> makeHashStructure SHA256 false false
                |> makeOption

        // Hash should exist and match
        Assert.True(oneFileDirHash.IsSome)
        Assert.Equal(
            "c0b9c17c8ac302513644256d06d1518a50c0c349e28023c2795a17dfa5479e1f",
            getHash oneFileDirHash.Value)

    [<Fact>]
    member _.``Dir with 2 files`` () =
        // Setup dir with 2 files
        let dirTwo = Path.Combine(fsTempDirSetupFixture.TempDir, "dir_two")
        Directory.CreateDirectory(dirTwo) |> ignore;
        File.WriteAllText(Path.Combine(dirTwo, "file1.txt"), "1");
        File.WriteAllText(Path.Combine(dirTwo, "file2.txt"), "2");

        // Compute hash
        let twoFileDirHash =
            dirTwo
                |> makeHashStructure SHA256 false false
                |> makeOption

        // Hash should exist and match
        Assert.True(twoFileDirHash.IsSome)
        Assert.Equal(
            "072d85c3b6926317ee8c340d4e989c9588c75408e63b5674571624a096faf9b5",
            getHash twoFileDirHash.Value)
