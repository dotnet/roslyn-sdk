// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing
{
    internal class AggregateOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptionsProvider _analyzerConfigOptionsProvider;

        public AggregateOptionsProvider(AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider, List<(string key, string value)> globalOptions)
        {
            _analyzerConfigOptionsProvider = analyzerConfigOptionsProvider;
            GlobalOptions = new AggregateConfigOptions(_analyzerConfigOptionsProvider.GlobalOptions, globalOptions);
        }

        public override AnalyzerConfigOptions GlobalOptions { get; }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
            => _analyzerConfigOptionsProvider.GetOptions(tree);

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
            => _analyzerConfigOptionsProvider.GetOptions(textFile);
    }
}
