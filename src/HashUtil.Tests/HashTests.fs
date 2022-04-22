module HashTests

open HashUtil
open HashUtil.Checksum
open Microsoft.FSharp.Reflection
open Xunit
open Xunit.Abstractions

type ChecksumTests(output: ITestOutputHelper) =
    static member emptyHashes: obj [] seq =
        Seq.ofList [ [| MD5
                        "d41d8cd98f00b204e9800998ecf8427e" |]
                     [| RIPEMD160
                        "9c1185a5c5e9fc54612808977ee8f548b2258d31" |]
                     [| SHA1
                        "da39a3ee5e6b4b0d3255bfef95601890afd80709" |]
                     [| SHA256
                        "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855" |]
                     [| SHA384
                        "38b060a751ac96384cd9327eb1b1e36a21fdb71114be07434c0cc7bf63f6e1da274edebfe76f65fbd51ad2f14898b95b" |]
                     [| SHA512
                        "cf83e1357eefb8bdf1542850d66d8007d620e4050b5715dc83f4a921d36ce9ce47d0d13c5d85f2b0ff8318d2877eec2f63b931bd47417a81a538327af927da3e" |] ]

    [<Theory; MemberData("emptyHashes")>]
    member _.``hash empty string``(hashType, expectedHash) =
        let hashAlg = Checksum.getHashAlgorithm hashType
        let emptyStrHash = computeHashOfString hashAlg ""
        Assert.Equal(expectedHash, emptyStrHash)

    static member simpleStringHashes: obj [] seq =
        Seq.ofList [ [| MD5
                        "hello"
                        "5d41402abc4b2a76b9719d911017c592" |]
                     [| RIPEMD160
                        "hello"
                        "108f07b8382412612c048d07d13f814118445acd" |]
                     [| SHA1
                        "hello"
                        "aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d" |]
                     [| SHA256
                        "hello"
                        "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824" |]
                     [| SHA384
                        "hello"
                        "59e1748777448c69de6b800d7a33bbfb9ff1b463e44354c3553bcdb9c666fa90125a3c79f90397bdf5f6a13de828684f" |]
                     [| SHA512
                        "hello"
                        "9b71d224bd62f3785d96d46ad3ea3d73319bfbc2890caadae2dff72519673ca72323c3d99ba5c11d7c7acc6e14b8c5da0c4663475c2e5c3adef46f73bcdec043" |] ]

    [<Theory; MemberData("simpleStringHashes")>]
    member _.``hash simple string``(hashType, inputStr, expectedHash) =
        let hashAlg = Checksum.getHashAlgorithm hashType
        let strHash = computeHashOfString hashAlg inputStr
        Assert.Equal(expectedHash, strHash)

    static member hashTypeStrings: obj [] seq =
        Seq.ofList [ [| [ "md5"; " md5   "; "MD5 " ]
                        Some MD5 |]
                     [| [ "ripemd160"; " ripemd160   "; " RIPEMD160 " ]
                        Some RIPEMD160 |]
                     [| [ "sha1"; " Sha1   "; "SHA1 " ]
                        Some SHA1 |]
                     [| [ "sha256"; " Sha256   "; "SHA256 " ]
                        Some SHA256 |]
                     [| [ "sha384"; " Sha384   "; "SHA384 " ]
                        Some SHA384 |]
                     [| [ "sha512"; " Sha512   "; "SHA512 " ]
                        Some SHA512 |]
                     [| [ "asha1"; " md6   "; "SHA513 " ]
                        None |] ]

    [<Theory; MemberData("hashTypeStrings")>]
    member _.``parse HashType``(inputStrs, expectedType) =
        for inputStr in inputStrs do
            let hashTypeMaybe = parseHashType inputStr
            Assert.Equal(expectedType, hashTypeMaybe)

    [<Fact>]
    member _.``parse all possible types``() =
        let parsedHashTypes =
            typeof<HashType>
            |> FSharpType.GetUnionCases
            |> Array.map (fun info -> info.Name)
            |> Array.map parseHashType

        Assert.All(parsedHashTypes, (fun parsedHashType -> Assert.True(parsedHashType.IsSome)))
