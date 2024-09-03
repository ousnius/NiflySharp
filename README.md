## NiflySharp by [ousnius](https://github.com/ousnius)
C# / .NET NIF library for the Gamebryo/NetImmerse File Format (NetImmerse, Gamebryo, Creation Engine).  
Created with a clean-room design.

[![.NET](https://github.com/ousnius/NiflySharp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/ousnius/NiflySharp/actions/workflows/dotnet.yml) [![NuGet](https://img.shields.io/nuget/v/Nifly)](https://www.nuget.org/packages/Nifly)

This is a rewrite of the C++ version of [nifly](https://github.com/ousnius/nifly) for C# using source generation based on [nifxml](https://github.com/niftools/nifxml).

### Features
- Reading and writing NIF files (NetImmerse, Gamebryo, Creation Engine)
- Cross platform (Windows, Linux, macOS) running on .NET 8
- NIF blocks unknown to the library are kept untouched
- Lots of other helper functions
- Current file support:
  - Any previous or older games using known NIF formats (untested)
  - Fallout 3
  - Fallout: New Vegas
  - Fallout 4
  - Fallout 4 VR
  - Fallout 76 (mostly untested)
  - The Elder Scrolls IV: Oblivion (works, but mostly untested)
  - The Elder Scrolls V: Skyrim
  - The Elder Scrolls V: Skyrim Special Edition
  - The Elder Scrolls V: Skyrim VR
  - Starfield (initial support, mostly untested)

#### Libraries used
- [nifxml](https://github.com/niftools/nifxml) - Used by source generator for NIF specification
- [Miniball](https://github.com/SearchAThing-forks/miniball) - C# port for generating bounding spheres

#### Credits
- [Contributors to nifly](https://github.com/ousnius/nifly/graphs/contributors)
- [Contributors to NiflySharp](https://github.com/ousnius/NiflySharp/graphs/contributors)
- [ousnius](https://github.com/ousnius)
- [jonwd7](https://github.com/jonwd7)
- [Candoran2](https://github.com/Candoran2)
- [NifTools team and contributors](https://www.niftools.org/)
