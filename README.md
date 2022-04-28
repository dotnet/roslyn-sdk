# Roslyn SDK

| Branch | Status |
|:------:|:------:|
|dev16.0.x|[![Build Status](https://dnceng.visualstudio.com/public/_apis/build/status/dotnet/roslyn-sdk/public-CI?branchName=dev16.0.x&label=build)](https://dnceng.visualstudio.com/public/_build/latest?definitionId=137&branchName=dev16.0.x)|
|main|[![Build Status](https://dnceng.visualstudio.com/public/_apis/build/status/dotnet/roslyn-sdk/public-CI?branchName=main&label=build)](https://dnceng.visualstudio.com/public/_build/latest?definitionId=137&branchName=main)|

# What is the Roslyn-SDK?

Roslyn is the compiler platform for .NET. It consists of the compiler itself and a powerful set of APIs to interact with the compiler. The Roslyn platform is hosted at [github.com/dotnet/roslyn](https://github.com/dotnet/roslyn). The compiler is part of every .NET installation. The APIs to interact with the compiler are available via NuGet (see the [Roslyn repository](https://github.com/dotnet/roslyn) for details). The Roslyn SDK includes additional components to get you started with advanced topics such as distributing a Roslyn analyzer as Visual Studio extension or to inspect code with the Syntax Visualizer. The documentation for the Roslyn platform can be found at [docs.microsoft.com/dotnet/csharp/roslyn-sdk](https://docs.microsoft.com/dotnet/csharp/roslyn-sdk). This repository contains code for both the Roslyn-SDK templates and Syntax Visualizer.

# Installation instructions

## Visual Studio 2017 (Version 15.5 and above)

1. Run **Visual Studio Installer**
2. Hit **Modify**
3. Select the **Individual components** tab
4. Check the box for **.NET Compiler Platform SDK**

## Visual Studio 2015

For older versions of Visual Studio the [.NET Compiler Platform SDK](https://visualstudiogallery.msdn.microsoft.com/2ddb7240-5249-4c8c-969e-5d05823bcb89) is available as an extension in the Visual Studio gallery.
