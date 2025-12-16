# Project Overview

Hashdir is a dotnet (f# language) CLI tool to compute the hash or checksum of a directory. This is done recursively using the directories, files and their names within. This is useful for quickly comparing directories and also determining if anything in the filesystem has changed.

It is written in F# on .NET 8.

The tool supports many popular hashing algorithms such as `blake3`, `md5`, `ripemd160`, `sha1`, `sha256`, `sha384`, `sha512`, and `xxhash3`.

# How to Run

- **Dev Build:** `make build` can be used to quickly make sure the app builds.
- **Run Tests:** `make test` can be used to make sure the tests are passing.
- **Check changes/PR:** `git diff main` can be used to understand the feature branch.

# Project Structure

The solution is organized into several projects within the `src/` directory.

- **`src/App`**: The main CLI application project.
  - `Program.fs`: The entry point of the application, responsible for parsing command-line arguments and orchestrating the hashing process.

- **`src/HashUtil`**: A library project containing the core hashing logic.
  - `Checksum.fs`: This is a key file. It defines the supported hash algorithms (`HashType` discriminated union), a function to parse the algorithm from user input (`parseHashType`), and a factory function to create the appropriate `HashAlgorithm` instance (`getHashAlgorithm`). When adding a new algorithm, this file is the primary one to modify.
  - `NonCryptoWrapper.fs`: A wrapper class that adapts non-cryptographic hash algorithms (like `XxHash3`) to the standard `HashAlgorithm` interface, allowing them to be used seamlessly with the existing hashing infrastructure.
  - `Hashing.fs`: Contains the logic for hashing files and directories.
  - `Library.fs`: Provides a programmatic API for the hashing functionality.
  - `Util.fs`: Contains utility functions, including `computeHashOfString`.

- **`src/Checksums`**: A C# project that provides an implementation of the `RIPEMD160` algorithm.

- **`src/App.Tests`**: Contains tests for the `App` project.

- **`src/HashUtil.Tests`**: Contains tests for the `HashUtil` library.
  - `HashTests.fs`: Contains tests for the hashing functionality, including tests for different algorithms and input strings. When adding a new algorithm, this file should be updated with new test cases.
