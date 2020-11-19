open System
open System.IO;
open System.Security.Cryptography
open System.Text


let computeHashString (input : string) =
    input 
        |> Encoding.ASCII.GetBytes
        |> SHA256.Create().ComputeHash
        |> Seq.map (fun c -> c.ToString("x2"))
        |> Seq.reduce (+)


let computeHashStringFromFile filePath =
    if not (File.Exists filePath) then
        None
    else
        use file = File.Create filePath
        file
            |> SHA256.Create().ComputeHash
            |> Seq.map (fun c -> c.ToString("x2"))
            |> Seq.reduce (+)
            |> Some
    

[<EntryPoint>]
let main argv =
    for arg in argv do
        match (computeHashStringFromFile arg) with
            | Some(hash) -> printfn "%s %s" hash arg
            | None -> ()

    0