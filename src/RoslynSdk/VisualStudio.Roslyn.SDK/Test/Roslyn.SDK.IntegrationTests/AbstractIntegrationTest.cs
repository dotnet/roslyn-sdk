// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Extensibility.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    // Preserve DOTNET_ROOT so Visual Studio can resolve the repo-local SDK selected by the build.
    [IdeSettings(MinVersion = VisualStudioVersion.VS18, MaxAttempts = 2, EnvironmentVariables = new[] { "DOTNET_MULTILEVEL_LOOKUP=", "DOTNET_INSTALL_DIR=", "DotNetRoot=", "DotNetTool=" })]
    public abstract class AbstractIntegrationTest : AbstractIdeIntegrationTest
    {
    }
}
