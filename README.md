# hashdir

![build](https://github.com/ultimateanu/hashdir/workflows/build/badge.svg)
![test](https://github.com/ultimateanu/hashdir/workflows/test/badge.svg)
[![codecov](https://codecov.io/gh/ultimateanu/hashdir/branch/main/graph/badge.svg?token=5RR570QEIX)](https://codecov.io/gh/ultimateanu/hashdir)

![sample terminal usage](https://ultimateanu.github.io/hashdir/assets/img/check_demo.svg)

_A command-line utility to checksum directories and files._

**hashdir** aims to be the easiest way to hash a file/directory. This is useful in many situations such as transferring files, archiving data, or detecting duplicates. It is a single binary, works on all major OS's, and has a simple command-line interface. It is developed with F# on .NET 5.

Links: [Github](https://github.com/ultimateanu/hashdir), [project site](https://ultimateanu.github.io/hashdir)

## Installation
There are several ways to get hashdir. Full details can be found [here](https://ultimateanu.github.io/hashdir/#installation).

- **Homebrew**: `brew install ultimateanu/software/hashdir`
- **dotnet**: `dotnet tool install --global hashdir`
- **Stand-alone binary**: latest version for macOS, Windows, and Linux can be found at [releases](https://github.com/ultimateanu/hashdir/releases)

## Usage
```
hashdir:
  A command-line utility to checksum directories and files.

Usage:
  hashdir [options] [<item>...] [command]

Arguments:
  <item>    Directory or file to hash/check

Options:
  -t, --tree                                         Print directory tree
  -s, --save                                         Save the checksum to a file
  -i, --include-hidden-files                         Include hidden files
  -e, --skip-empty-dir                               Skip empty directories
  -a, --algorithm <md5|sha1|sha256|sha384|sha512>    The hash function to use [default: sha1]
  --version                                          Show version information
  -?, -h, --help                                     Show help and usage information

Commands:
  check <item>    Verify that the specified hash file is valid.
```

### Examples
1. Hash a file/directory: `hashdir ~/Desktop/project/`
2. Hash a directory with hidden files and print tree: `hashdir --include-hidden-files --tree ~/Desktop/project`
3. Hash multiple items using MD5: `hashdir -a md5 song.mp3 info.txt report.pdf`

## License
[MIT License](https://github.com/ultimateanu/hashdir/blob/main/LICENSE)

**hashdir** is an open-source project with a permissive license. If you find a bug or have suggestions feel free to create an issue on Github. Any contributions to the code, tests, or documentation are also welcome via a pull request.
