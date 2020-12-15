namespace HashUtil

open System.Security.Cryptography
open System.Text

module Say =
    let hello name =
        printfn "Hello %s" name
    let bigger x = 2*x

module Checksum =
    let computeHashString (input : string) =
        input
            |> Encoding.ASCII.GetBytes
            |> SHA256.Create().ComputeHash
            |> Seq.map (fun c -> c.ToString("x2"))
            |> Seq.reduce (+)