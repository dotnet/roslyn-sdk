# Roslyn SDK

This branch contains a version of the syntax visualizer that can be used to generate unit tests for IOperation and Dataflow analysis. Steps to build and use:

1. Build Roslyn with `build.cmd -restore -buildAll -pack`
    a. At some point @jaredpar might remove `-buildAll`. At that point, just `-build` should work.
2. Publish updated Roslyn to the dev hive. Can be done by opening Roslyn.sln and rebuilding the entire solution.
3. Update [build/Versions.props](build/Versions.props) line 67 with the location of the published NuGet packages (should be roslyn-root/Binaries/Debug/NuGet/PerBuildPreRelease).
4. Run Restore.cmd. If you are redoing this, make sure to clear the developer versions from the nuget cache, or your changes won't be picked up.
5. F5 Roslyn.SDK.VS2017
6. Open a solution. Make sure that `<Features>flow-analysis</Features>` is in the project file.
7. Open the syntax visualizer and right-click on a block, and choose `Generate IOperation Test (If Possible)`. Test will appear in the bottom pane.

For updates that do not change the shape of the IOperation types or the test generator code, simply redeploying Roslyn will be fine. If changes are made that update the actual types or flow graph string builder code, then you'll need to update the dlls in the extension. You can either follow all instructions above again, or just replace the following files:

* Microsoft.VisualStudio.IntegrationTest.Utilities.dll
* Roslyn.Services.Test.Utilities.dll
* Roslyn.Test.Utilities.dll 

Replace them in `%localappdata%/Microsoft/VisualStudio/15.0_<hive here>/Extensions/Microsoft/.NET Compiler Platform SDK For Visual Studio 2017/42.42.42.42/`
