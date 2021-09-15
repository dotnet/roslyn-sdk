// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Shell;

namespace Roslyn.SDK
{
    /// <summary>
    /// Ensures that Visual Studio probes the package directory for assemblies at load time.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.providebindingpathattribute"/>
    [ProvideBindingPath]
    internal class BindingPathProvider
    {
    }
}
