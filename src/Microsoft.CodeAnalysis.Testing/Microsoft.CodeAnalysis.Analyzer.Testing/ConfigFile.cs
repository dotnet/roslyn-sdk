// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Testing
{
    public class ConfigFile : Dictionary<string, Dictionary<string, string>>
    {
        public string FileName { get; }

        public Dictionary<string, string> Preamble { get; }

        public ConfigFile(
            string fileName,
            Dictionary<string, string>? preamble = null)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Preamble = preamble ?? new Dictionary<string, string>();
        }

        private static string ToString(Dictionary<string, string> dictionary)
        {
            return string.Join(
                Environment.NewLine,
                dictionary.Select(pair => $@"{pair.Key} = {pair.Value}"));
        }

        private static string ToString(Dictionary<string, Dictionary<string, string>> dictionary)
        {
            return string.Join(
                Environment.NewLine,
                dictionary.Select(pair => $@"[{pair.Key}]
{ToString(pair.Value)}"));
        }

        public override string ToString()
        {
            return @$"
{ToString(Preamble)}

{ToString(this)}
";
        }
    }
}
