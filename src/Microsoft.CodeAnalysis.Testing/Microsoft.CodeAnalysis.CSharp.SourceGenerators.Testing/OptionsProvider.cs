// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Testing
{
    /// <summary>
    /// This class just passes argument through to the projects options provider and it used to provider custom global options
    /// </summary>
    internal class OptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptionsProvider _analyzerConfigOptionsProvider;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1414:Tuple types in signatures should have element names", Justification = "The tuple names in the global options are unknown.")]
        public OptionsProvider(AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider, List<(string, string)> globalOptions)
        {
            _analyzerConfigOptionsProvider = analyzerConfigOptionsProvider;
            GlobalOptions = new ConfigOptions(_analyzerConfigOptionsProvider.GlobalOptions, globalOptions);
        }

        public override AnalyzerConfigOptions GlobalOptions { get; }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
            => _analyzerConfigOptionsProvider.GetOptions(tree);

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
            => _analyzerConfigOptionsProvider.GetOptions(textFile);
    }
}
