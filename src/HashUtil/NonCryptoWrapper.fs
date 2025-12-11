namespace HashUtil

open System
open System.IO.Hashing
open System.Security.Cryptography

type NonCryptoWrapper(hashAlgo: NonCryptographicHashAlgorithm) =
    inherit HashAlgorithm()

    override this.HashCore(array: byte[], ibStart: int, cbSize: int) =
        hashAlgo.Append(System.ArraySegment<byte>(array, ibStart, cbSize))

    override this.HashFinal() =
        hashAlgo.GetCurrentHash()

    override this.Initialize() =
        hashAlgo.Reset()