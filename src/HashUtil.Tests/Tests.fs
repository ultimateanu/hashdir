module Tests

open System
open Xunit
open HashUtil

[<Fact>]
let ``My test`` () =
    Assert.True(true)

[<Fact>]
let ``Bigger`` () =
    let ans = Say.bigger 3
    Assert.Equal(6, ans)
