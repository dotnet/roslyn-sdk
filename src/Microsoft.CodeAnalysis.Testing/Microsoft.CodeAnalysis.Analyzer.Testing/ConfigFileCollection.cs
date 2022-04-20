// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    public class ConfigFileCollection : SourceFileCollection
    {
        public void Add((string filename, ConfigFile configFile) file)
        {
            Add((file.filename, SourceText.From(file.configFile.ToString(), Encoding.UTF8)));
        }
    }
}
