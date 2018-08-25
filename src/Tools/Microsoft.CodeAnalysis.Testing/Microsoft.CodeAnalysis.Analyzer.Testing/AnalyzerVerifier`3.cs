// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing
{
    public class AnalyzerVerifier<TAnalyzer, TTest, TVerifier>
           where TAnalyzer : DiagnosticAnalyzer, new()
           where TTest : AnalyzerTest<TVerifier>, new()
           where TVerifier : IVerifier, new()
    {
        public static Task VerifyAnalyzerAsync(string source, DiagnosticResult expected, CancellationToken cancellationToken = default)
            => VerifyAnalyzerAsync(source, new[] { expected }, cancellationToken);

        public static Task VerifyAnalyzerAsync(string source, DiagnosticResult[] expected, CancellationToken cancellationToken = default)
        {
            var test = new TTest
            {
                TestCode = source,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync(cancellationToken);
        }
    }
}
