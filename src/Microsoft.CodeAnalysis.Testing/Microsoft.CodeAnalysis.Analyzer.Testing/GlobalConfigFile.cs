// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Testing
{
    public class GlobalConfigFile : ConfigFile
    {
        public GlobalConfigFile(
            Dictionary<string, string>? preamble = null,
            Dictionary<string, Dictionary<string, string>>? sections = null)
            : base("/.globalconfig", preamble, sections)
        {
        }
    }
}
