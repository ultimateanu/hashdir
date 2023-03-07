namespace HashUtil

open FS
open GlobExpressions
open System.IO
open System.Security.Cryptography
open System

module Hashing =
    type HashingUpdate =
        | FileHashStarted of path: string
        | FileHashCompleted of path: string
        | DirHashStarted of path: string
        | DirHashCompleted of path: string

    let rec private makeDirHashStructure
        (progressObserver: IObserver<HashingUpdate>)
        (hashAlg: HashAlgorithm)
        (ignoreGlobs: Glob list)
        includeHiddenFiles
        includeEmptyDir
        (rootDir: string)
        dirPath
        =
        assert Directory.Exists(dirPath)

        let children =
            dirPath
            |> Directory.EnumerateFileSystemEntries
            |> Seq.toList
            |> List.sort
            |> List.map (
                makeHashStructureHelper
                    progressObserver
                    hashAlg
                    ignoreGlobs
                    includeHiddenFiles
                    includeEmptyDir
                    rootDir
            )
            |> List.choose Util.makeOption

        if children.IsEmpty && not includeEmptyDir then
            Error("Excluding dir because it is empty")
        else
            let getNameAndHashString (x: FS.ItemHash) : string =
                match x with
                | File(path, hash) -> hash + (Path.GetFileName path)
                | Dir(path, hash, _) -> hash + (Util.getChildName path)

            let childrenHash =
                children
                |> List.map getNameAndHashString
                |> fun x -> "" :: x // Add empty string as a child to compute hash of empty dir.
                |> List.reduce (+)
                |> Util.computeHashOfString hashAlg

            Ok(Dir(path = dirPath, hash = childrenHash, children = children))

    and private makeHashStructureHelper
        (progressObserver: IObserver<HashingUpdate>)
        (hashAlg: HashAlgorithm)
        (ignoreGlobs: Glob list)
        (includeHiddenFiles: bool)
        (includeEmptyDir: bool)
        (rootDir: string)
        path
        =

        let relativePath = Path.GetRelativePath(rootDir, path)

        let shouldIgnore =
            ignoreGlobs
            |> List.map (fun glob -> glob.IsMatch relativePath)
            |> List.contains true

        if shouldIgnore then
            Error("Not including ignored pattern")
        else if File.Exists(path) then
            if
                ((not includeHiddenFiles)
                 && (File.GetAttributes(path) &&& FileAttributes.Hidden)
                     .Equals(FileAttributes.Hidden))
            then
                Error("Not including hidden file")
            else
                progressObserver.OnNext(HashingUpdate.FileHashStarted path)
                let hash = Util.computeHashOfFile hashAlg path
                progressObserver.OnNext(HashingUpdate.FileHashCompleted path)
                Ok(ItemHash.File(path = path, hash = hash))
        else if Directory.Exists(path) then
            progressObserver.OnNext(HashingUpdate.DirHashStarted path)

            let result =
                makeDirHashStructure
                    progressObserver
                    hashAlg
                    ignoreGlobs
                    includeHiddenFiles
                    includeEmptyDir
                    rootDir
                    path

            progressObserver.OnNext(HashingUpdate.DirHashCompleted path)
            result
        else
            Error(sprintf "%s is not a valid path" path)

    let makeHashStructureObservable
        (progressObserver: IObserver<HashingUpdate>)
        (hashType: Checksum.HashType)
        (ignorePatterns: string[])
        includeHiddenFiles
        includeEmptyDir
        (rootDir: string)
        path
        =
        async {
            let hashAlg = Checksum.getHashAlgorithm hashType

            let ignoreGlobMatchers =
                ignorePatterns |> Array.toList |> List.map Glob

            let result =
                makeHashStructureHelper
                    progressObserver
                    hashAlg
                    ignoreGlobMatchers
                    includeHiddenFiles
                    includeEmptyDir
                    rootDir
                    path

            progressObserver.OnCompleted()
            return result
        }

    type EmptyHashingObserver() =
        interface IObserver<HashingUpdate> with
            member this.OnCompleted() : unit = ()

            member this.OnError(error: exn) : unit =
                raise (System.NotImplementedException())

            member this.OnNext(hashingUpdate: HashingUpdate) : unit = ()


    let makeHashStructure
        (hashType: Checksum.HashType)
        includeHiddenFiles
        includeEmptyDir
        path
        =
        let emptyHashingObserver = EmptyHashingObserver()

        Async.RunSynchronously
        <| makeHashStructureObservable
            emptyHashingObserver
            hashType
            [||] // ignorePatterns
            includeHiddenFiles
            includeEmptyDir
            path // rootDir
            path
