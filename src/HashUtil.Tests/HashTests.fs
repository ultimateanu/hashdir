module HashTests

open HashUtil.Checksum
open Xunit
open Xunit.Abstractions

type ChecksumTests(output: ITestOutputHelper) =
    static member emptyHashes : obj[] seq =
        Seq.ofList [
            [|HashType.MD5; "d41d8cd98f00b204e9800998ecf8427e"|]
            [|HashType.SHA1; "da39a3ee5e6b4b0d3255bfef95601890afd80709"|]
            [|HashType.SHA256; "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"|]
            [|HashType.SHA384; "38b060a751ac96384cd9327eb1b1e36a21fdb71114be07434c0cc7bf63f6e1da274edebfe76f65fbd51ad2f14898b95b"|]
            [|HashType.SHA512; "cf83e1357eefb8bdf1542850d66d8007d620e4050b5715dc83f4a921d36ce9ce47d0d13c5d85f2b0ff8318d2877eec2f63b931bd47417a81a538327af927da3e"|]
        ]
    [<Theory; MemberData("emptyHashes")>]
    member _.``hash empty string`` (hashType : HashType,  expectedHash : string) : unit =
        let emptyStrHash = computeHashOfString hashType ""
        Assert.Equal(expectedHash, emptyStrHash)

    static member simpleStringHashes : obj[] seq =
        Seq.ofList [
            [|HashType.MD5; "hello"; "5d41402abc4b2a76b9719d911017c592"|]
            [|HashType.SHA1; "hello"; "aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d"|]
            [|HashType.SHA256; "hello"; "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824"|]
            [|HashType.SHA384; "hello"; "59e1748777448c69de6b800d7a33bbfb9ff1b463e44354c3553bcdb9c666fa90125a3c79f90397bdf5f6a13de828684f"|]
            [|HashType.SHA512; "hello"; "9b71d224bd62f3785d96d46ad3ea3d73319bfbc2890caadae2dff72519673ca72323c3d99ba5c11d7c7acc6e14b8c5da0c4663475c2e5c3adef46f73bcdec043"|]
        ]
    [<Theory; MemberData("simpleStringHashes")>]
    member _.``hash simple string`` (hashType : HashType, inputStr: string,  expectedHash : string) : unit =
        let strHash = computeHashOfString hashType inputStr
        Assert.Equal(expectedHash, strHash)
