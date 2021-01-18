- [Overview](#overview)
- [Install](#install)
    * [Stand-alone binary](#stand-alone-binary)
    * [Package manager](#package-manager)
    * [Cross platform dotnet app](#cross-platform-dotnet-app)
    * [Build from source](#build-from-source)
- [Usage](#usage)


# Overview
**hashdir** is a simple command line tool to checksum directories and files.

![Sample usage](hashdir/assets/img/hashdir_demo.svg)

A [checksum](https://en.wikipedia.org/wiki/Checksum) is a short sequence of letters and numbers derived from another (often larger) sequence of data. Checksums are created from input data using a hashing algorithm. For a good hashing algorithm, it is extremely difficult to come up with an input that results in a specific checksum. Therefore a checksum acts like a digital fingerprint - _if the checksums match we can be reasonably sure the input data matches_.

This is useful in many situations:
- **Transferring files** - compare checksums to ensure nothing was corrupted or tampered
- **Archiving data** - storing the checksum along with the data allows you to verify that your usb/harddrive/cloud provider didn’t modify your data
- **Duplicate detection** - check if you have duplicate files or directories and know what is safe to delete


# Install
There are several ways to install hashdir, and they are listed below roughly in order of convenience. You can choose the method that fits your needs. The latest release can always be found at <https://github.com/ultimateanu/hashdir/releases>.

### Stand-alone binary
This is a single executable file with no external dependencies. It is available for various operating systems (e.g. macOS, Windows, Linux). This method is very simple but the stand-alone binaries are quite large since they bundle in the necessary dotnet runtime.
1. Download the latest version for your OS from [releases](https://github.com/ultimateanu/hashdir/releases)
2. Extract the contents of the .zip or .tar.gz file
3. Run the binary
```
hashdir --help
```
4. _Optional_: copy the executable to a directory in your PATH (e.g. /usr/local/bin)

### Package manager
hashdir is available via package managers. If you already have a package manager, this allows for easy installation and upgrades.
##### dotnet (NuGet)
```
dotnet tool install --global hashdir
dotnet tool update --global hashdir
```

### Cross platform dotnet app
If you already have the dotnet runtime on your machine, you can use the dotnet application which is a cross-platform solution. Since this relies on the dotnet platform for your system, the resulting size is significantly smaller.
1. Download the hashdir_x.y.z_dotnet zip or tar.gz file from [releases](https://github.com/ultimateanu/hashdir/releases)
2. Extract the contents of the .zip or .tar.gz file
3. Run the app  
```
dotnet hashdir.dll --help
```

### Build from source
Since this is an open source project you can also build from source! This requires [dotnet 5](https://dotnet.microsoft.com).
1. Download the source code from the main branch on [GitHub](https://github.com/ultimateanu/hashdir/tree/main)
2. _Optional_: Build and run the app
```
dotnet run --project src/App/App.fsproj -- --help    
```
3. Publish a release version
```
dotnet publish -c Release src/App/App.fsproj 
```
4. Run the app
```
dotnet src/App/bin/Release/net5.0/publish/hashdir.dll --help
```


# Usage
