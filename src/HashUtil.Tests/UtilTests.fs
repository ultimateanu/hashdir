module UtilTests

open HashUtil
open HashUtil.Checksum
open HashUtil.Common
open HashUtil.Util
open Xunit
open Xunit.Abstractions
open System

type UtilTests(output: ITestOutputHelper) =
    [<Fact>]
    member _.``check spacing literals``() =
        Assert.Equal("    ", bSpacer)
        Assert.Equal("│   ", iSpacer)
        Assert.Equal("├── ", tSpacer)
        Assert.Equal("└── ", lSpacer)

    [<Theory>]
    [<InlineData("The ", "the ")>]
    [<InlineData("Bruce Wayne", "bruce wayne")>]
    member _.``toStrLower simple strings``(input, expectedOutput) =
        Assert.Equal(expectedOutput, toStrLower input)

    [<Fact>]
    member _.``toStrLower discriminated union types``() =
        Assert.Equal("md5", toStrLower MD5)
        Assert.Equal("sha1", toStrLower SHA1)
        Assert.Equal("sha384", toStrLower SHA384)

    [<Fact>]
    member _.``makeOption from error``() =
        Assert.Equal(None, Util.makeOption (Error "something went wrong"))
        Assert.Equal(None, Util.makeOption (Error 1))

    [<Fact>]
    member _.``makeOption from success result``() =
        Assert.Equal(Some "data", Util.makeOption (Ok "data"))
        Assert.Equal(Some 1, Util.makeOption (Ok 1))

    [<Fact>]
    member _.``printColor``() =
        let oldOut = Console.Out
        use out = new IO.StringWriter()
        Console.SetOut(out)
        Util.printColor ConsoleColor.Green "MATCHES"
        Console.SetOut(oldOut)
        let printOutput = out.GetStringBuilder().ToString()

        Assert.Equal("MATCHES", printOutput)
