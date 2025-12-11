namespace HashUtil

open Microsoft.FSharp.Reflection
open System.IO
open System.IO.Hashing
open System.Security.Cryptography
open System.Text
open Blake3

module Checksum =
    type HashType =
        | MD5
        | RIPEMD160
        | SHA1
        | SHA256
        | SHA384
        | SHA512
        | BLAKE3
        | XXHASH3
        | CRC32

    let allHashTypes: HashType[] =
        typeof<HashType>
        |> FSharpType.GetUnionCases
        |> Array.map (fun info ->
            FSharpValue.MakeUnion(info, [||]) :?> HashType)

    let parseHashType (input: string) =
        let hashTypeStr = input.ToUpper().Trim()

        match hashTypeStr with
        | "MD5" -> Some MD5
        | "RIPEMD160" -> Some RIPEMD160
        | "SHA1" -> Some SHA1
        | "SHA256" -> Some SHA256
        | "SHA384" -> Some SHA384
        | "SHA512" -> Some SHA512
        | "BLAKE3" -> Some BLAKE3
        | "XXHASH3" -> Some XXHASH3
        | "CRC32" -> Some CRC32
        | _ -> None

    let getHashAlgorithm hashType : HashAlgorithm =
        match hashType with
        | MD5 -> MD5.Create()
        | RIPEMD160 -> Checksums.RIPEMD160.Create()
        | SHA1 -> SHA1.Create()
        | SHA256 -> SHA256.Create()
        | SHA384 -> SHA384.Create()
        | SHA512 -> SHA512.Create()
        | BLAKE3 -> new Blake3.Blake3HashAlgorithm()
        | XXHASH3 ->
            new NonCryptoWrapper(new System.IO.Hashing.XxHash3() )
        | CRC32 ->
            new NonCryptoWrapper(new System.IO.Hashing.Crc32() )
