// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Testing
{
    /// <summary>
    /// Allows adding additional global options
    /// </summary>
    internal class ConfigOptions : AnalyzerConfigOptions
    {
        private readonly AnalyzerConfigOptions _workspaceOptions;
        private readonly Dictionary<string, string> _globalOptions;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1414:Tuple types in signatures should have element names", Justification = "The tuple names in the global options are unknown.")]
        public ConfigOptions(AnalyzerConfigOptions workspaceOptions, List<(string, string)> globalOptions)
        {
            _workspaceOptions = workspaceOptions;
            _globalOptions = globalOptions.ToDictionary(t => t.Item1, t => t.Item2);
        }

        public override bool TryGetValue(string key, out string value)
            => _workspaceOptions.TryGetValue(key, out value) || _globalOptions.TryGetValue(key, out value);
    }
}
