// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing
{
    internal class AggregateConfigOptions : AnalyzerConfigOptions
    {
        private readonly AnalyzerConfigOptions _analyzerConfigOptions;
        private readonly Dictionary<string, string> _globalOptions;

        public AggregateConfigOptions(AnalyzerConfigOptions analyzerConfigOptions, List<(string key, string value)> globalOptions)
        {
            _analyzerConfigOptions = analyzerConfigOptions;
            _globalOptions = globalOptions.ToDictionary(t => t.key, t => t.value);
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            => _analyzerConfigOptions.TryGetValue(key, out value) || _globalOptions.TryGetValue(key, out value);
    }
}
