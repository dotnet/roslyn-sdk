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

        public Dictionary<string, string> Preamble { get; } = new Dictionary<string, string>();

        public ConfigFile(string fileName)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        }

        public new string this[string globalKey]
        {
            get
            {
                return Preamble[globalKey];
            }

            set
            {
                Preamble[globalKey] = value;
            }
        }

        public string this[string sectionKey, string key]
        {
            get
            {
                if (!TryGetValue(sectionKey, out var values))
                {
                    values = new Dictionary<string, string>();
                    base[sectionKey] = values;
                }

                return values[key];
            }

            set
            {
                if (!TryGetValue(sectionKey, out var values))
                {
                    values = new Dictionary<string, string>();
                    base[sectionKey] = values;
                }

                values[key] = value;
            }
        }

        public void Add(string globalKey, string value)
        {
            Preamble.Add(globalKey, value);
        }

        public void Add(string sectionKey, string key, string value)
        {
            if (!TryGetValue(sectionKey, out var values))
            {
                values = new Dictionary<string, string>();
                base[sectionKey] = values;
            }

            values.Add(key, value);
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
