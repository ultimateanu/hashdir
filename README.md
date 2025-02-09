# hashdir

[![build](https://github.com/ultimateanu/hashdir/actions/workflows/build.yml/badge.svg)](https://github.com/ultimateanu/hashdir/actions/workflows/build.yml)
[![test](https://github.com/ultimateanu/hashdir/actions/workflows/test.yml/badge.svg)](https://github.com/ultimateanu/hashdir/actions/workflows/test.yml)
[![codecov](https://codecov.io/gh/ultimateanu/hashdir/branch/main/graph/badge.svg?token=5RR570QEIX)](https://codecov.io/gh/ultimateanu/hashdir)

![sample terminal usage](https://ultimateanu.github.io/hashdir/assets/img/check_demo.svg)

_A command-line utility to hash directories and files._

**hashdir** aims to be the easiest way to hash a file/directory. This is useful in many situations such as transferring files, archiving data, or detecting duplicates. It is a single binary, works on all major OS's, and has a simple command-line interface. It is developed with F# on .NET 8.

Links: [Github](https://github.com/ultimateanu/hashdir), [NuGet](https://www.nuget.org/packages/hashdir), [Project Site](https://ultimateanu.github.io/hashdir)

## Installation

There are several ways to get hashdir. Full details can be found [here](https://ultimateanu.github.io/hashdir/#installation).

- **Homebrew\***: `brew install ultimateanu/software/hashdir`
- **dotnet**: `dotnet tool install --global hashdir`
- **Scoop**: `scoop bucket add ultimateanu https://github.com/ultimateanu/homebrew-software; scoop install hashdir`
- **Stand-alone binary**: latest version for macOS, Windows, and Linux can be found at [releases](https://github.com/ultimateanu/hashdir/releases)
- **AUR (Arch User Repository)**: If you are using an Arch-based distribution, you can build and install the [hashdir](https://aur.archlinux.org/packages/hashdir) package from the AUR

\*_Homebrew currently requires a project to have 50 stars to be included in core. So I’ve set up a custom tap for now that still allows easy installation. If you like this project, please consider starring on Github and adding a formula to Homebrew core eventually._

## Usage

```
Description:
  A command-line utility to hash directories and files.

Usage:
  hashdir [<item>...] [command] [options]

Arguments:
  <item>  Directory or file to hash/check

Options:
  -t, --tree                                                        Print directory tree
  -s, --save                                                        Save the checksum to a file
  -a, --algorithm <blake3|md5|ripemd160|sha1|sha256|sha384|sha512>  The hash function to use [default: sha1]
  -i, --include-hidden-files                                        Include hidden files
  -e, --skip-empty-dir                                              Skip empty directories
  -n, --ignore <pattern>                                            Directories/files to not include
  -h, --hash-only                                                   Print only the hash
  -c, --color                                                       Colorize the output [default: True]
  --version                                                         Show version information
  -?, -h, --help                                                    Show help and usage information


Commands:
  check <item>  Verify that the specified hash file is valid.
```

### Examples

1. Hash a file/directory: `hashdir ~/Desktop/project/`
2. Hash a directory with hidden files and print tree: `hashdir --include-hidden-files --tree ~/Desktop/project`
3. Hash multiple items using MD5: `hashdir -a md5 song.mp3 info.txt report.pdf`
4. Hash a directory, but ignore certain directories/files: `hashdir --ignore "node_modules" --ignore "**/*.xml" ~/Desktop/project`

## License

[MIT License](https://github.com/ultimateanu/hashdir/blob/main/LICENSE)

**hashdir** is an open-source project with a permissive license. If you find a bug or have suggestions feel free to create an issue on Github. Any contributions to the code, tests, or documentation are also welcome via a pull request.
