namespace HashUtil

open Microsoft.FSharp.Reflection
open System.IO
open System.Security.Cryptography
open System.Text

module Checksum =
    type HashType =
        | MD5
        | RIPEMD160
        | SHA1
        | SHA256
        | SHA384
        | SHA512

    let allHashTypes : HashType[] =
        typeof<HashType>
        |> FSharpType.GetUnionCases
        |> Array.map(fun info -> FSharpValue.MakeUnion(info,[||]) :?> HashType)

    let parseHashType (input: string) =
        let hashTypeStr = input.ToUpper().Trim()

        match hashTypeStr with
        | "MD5" -> Some MD5
        | "RIPEMD160" -> Some RIPEMD160
        | "SHA1" -> Some SHA1
        | "SHA256" -> Some SHA256
        | "SHA384" -> Some SHA384
        | "SHA512" -> Some SHA512
        | _ -> None

    let getHashAlgorithm hashType: HashAlgorithm =
        match hashType with
        | MD5 -> upcast MD5.Create()
        | RIPEMD160 -> upcast Checksums.RIPEMD160.Create()
        | SHA1 -> upcast SHA1.Create()
        | SHA256 -> upcast SHA256.Create()
        | SHA384 -> upcast SHA384.Create()
        | SHA512 -> upcast SHA512.Create()
