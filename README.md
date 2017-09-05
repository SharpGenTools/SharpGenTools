# SharpGenTools [![Build status](https://ci.appveyor.com/api/projects/status/w6nasj26yiwxq2y0/branch/master?svg=true)](https://ci.appveyor.com/project/jkoritzinsky/sharptools/branch/master) [![MyGet Pre Release](https://img.shields.io/myget/sharpgentools/vpre/SharpGenTools.Sdk.svg)](https://www.myget.org/feed/Packages/sharpgentools) [![NuGet](https://img.shields.io/nuget/v/SharpGenTools.Sdk.svg)](https://www.nuget.org/packages/SharpGenTools.Sdk) 

Code-gen tools forked from SharpDX and independently mantained.

## Features
* Accurate, fast code-gen for C++ and COM interfaces from their C++ headers.
* No dependencies on .NET Runtime COM support
* Supports passing code-gen information through MSBuild project and package references
* Pluggable Runtime Library Name (default library is SharpGen.Runtime)
  - Projects such as SharpDX could change to "SharpDX" to not break compatibility

## Components
* SharpGen
   - The code-gen engine that runs CastXML to parse the C++ and then generates the C#.
* SharpPatch
   - Patches the `calli` instructions for efficient COM interop as well as other constructs not possible in C#
* SharpGenTools.Sdk
   - MSBuild tooling to integrate SharpGen and SharpPatch directly into projects
* SharpGen.Interactive
   - Legacy command line front-end for SharpGen with Windows Forms progress dialog
   - The original SharpGen application
* SharpPatch.Cli
   - Legacy command line front-end for SharpPatch
   - The original SharpCli application

## Requirements
### To Use
* Any projects using the SDK to generate code must use new SDK-style projects with MSBuild 15.3 or higher (.NET Core 2.0 SDK or VS 2017.3)
* Make any mapping files a `SharpGenMapping` item in your `.csproj`.

### To Build
* MSBuild 15.3
* Visual C++ Tools (Toolset v140 or newer)
* Windows 10.0.15063 SDK (Creators Update)
* .NET Core 2.0
* Desktop .NET Workload (for SharpGen.Interactive)