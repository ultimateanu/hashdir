module HashTests

open HashUtil.Checksum
open Xunit
open Xunit.Abstractions

type ChecksumTests(output: ITestOutputHelper) =

    [<Fact>]
    member _.``SHA256 empty string hash``() =
        let emptyStrHash = computeHashOfString HashType.SHA256 ""
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", emptyStrHash)

    [<Fact>]
    member _.``SHA256 string hash``() =
        let emptyStrHash =
            computeHashOfString HashType.SHA256 "input"

        Assert.Equal("c96c6d5be8d08a12e7b5cdc1b207fa6b2430974c86803d8891675e76fd992c20", emptyStrHash)
