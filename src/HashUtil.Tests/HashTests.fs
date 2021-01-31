module HashTests

open HashUtil.Checksum
open Xunit
open Xunit.Abstractions

type ChecksumTests(output: ITestOutputHelper) =
    static member emptyHashes : obj[] seq =
        Seq.ofList [[| HashType.SHA256; "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855" |] ;
        [|HashType.MD5; "d41d8cd98f00b204e9800998ecf8427e"|]
        ]
    [<Theory; MemberData("emptyHashes")>]
    member _.``hash empty string`` (hashType : HashType,  expectedHash : string) : unit =
        let emptyStrHash = computeHashOfString hashType ""
        Assert.Equal(expectedHash, emptyStrHash)

    static member simpleStringHashes : obj[] seq =
        Seq.ofList [
        [|HashType.SHA256; "hello"; "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824" |] ;
        [|HashType.SHA256; "some string"; "61d034473102d7dac305902770471fd50f4c5b26f6831a56dd90b5184b3c30fc"|]
        [|HashType.MD5; "hello"; "5d41402abc4b2a76b9719d911017c592"|]
        [|HashType.MD5; "some string"; "5ac749fbeec93607fc28d666be85e73a"|]
        ]
    [<Theory; MemberData("simpleStringHashes")>]
    member _.``hash simple string`` (hashType : HashType, inputStr: string,  expectedHash : string) : unit =
        let strHash = computeHashOfString hashType inputStr
        Assert.Equal(expectedHash, strHash)
