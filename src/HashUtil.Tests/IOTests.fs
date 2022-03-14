module IOTests

open HashUtil.FS
open System.IO
open Xunit
open Xunit.Abstractions

type DisplayTests(output: ITestOutputHelper) =
    [<Fact>]
    member _.``Print file hash``() =
        let hashStructure =
            ItemHash.File(path = "/path/to/file.txt", hash = "a1b2c3")

        let strWriter = new StringWriter()

        // Output without tree should be a single line.
        printHashStructure hashStructure false strWriter true
        let expectedStr = "a1b2c3  file.txt\n"
        Assert.Equal(expectedStr, strWriter.ToString())

        // With tree should be same result.
        strWriter.GetStringBuilder().Clear() |> ignore
        printHashStructure hashStructure true strWriter true
        Assert.Equal(expectedStr, strWriter.ToString())

    [<Fact>]
    member _.``Print dir tree hash (0 files)``() =
        let hashStructure =
            ItemHash.Dir(path = "/path/to/dir", hash = "d1", children = [])

        let strWriter = new StringWriter()
        printHashStructure hashStructure true strWriter true

        let expectedStr = "d1  /dir\n"
        Assert.Equal(expectedStr, strWriter.ToString())

    [<Fact>]
    member _.``Print dir tree hash (1 file)``() =
        let hashStructure =
            ItemHash.Dir(
                path = "/path/to/dir",
                hash = "d1",
                children = [ ItemHash.File(path = "/path/to/dir/file1.txt", hash = "f1") ]
            )

        let strWriter = new StringWriter()
        printHashStructure hashStructure true strWriter true

        let expectedStr = "d1  /dir\n└── f1  file1.txt\n"
        Assert.Equal(expectedStr, strWriter.ToString())

    [<Fact>]
    member _.``Print dir tree hash (2 files)``() =
        let hashStructure =
            ItemHash.Dir(
                path = "/path/to/dir",
                hash = "d1",
                children =
                    [ ItemHash.File(path = "/path/to/dir/file1.txt", hash = "f1")
                      ItemHash.File(path = "/path/to/dir/file2.txt", hash = "f2") ]
            )

        let strWriter = new StringWriter()
        printHashStructure hashStructure true strWriter true

        let expectedStr =
            "d1  /dir\n├── f1  file1.txt\n└── f2  file2.txt\n"

        Assert.Equal(expectedStr, strWriter.ToString())

    [<Fact>]
    member _.``Print dir multiple files no tree``() =
        let hashStructure =
            ItemHash.Dir(
                path = "/path/to/dir",
                hash = "d1",
                children =
                    [ ItemHash.File(path = "/path/to/dir/file1.txt", hash = "f1")
                      ItemHash.File(path = "/path/to/dir/file2.txt", hash = "f2")
                      ItemHash.File(path = "/path/to/dir/file3.txt", hash = "f3") ]
            )

        let strWriter = new StringWriter()
        printHashStructure hashStructure false strWriter true

        let expectedStr = "d1  /dir\n"
        Assert.Equal(expectedStr, strWriter.ToString())
