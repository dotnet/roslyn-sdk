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
        public static DiagnosticResult Diagnostic()
        {
            var analyzer = new TAnalyzer();
            return Diagnostic(analyzer.SupportedDiagnostics.Single());
        }

        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            var analyzer = new TAnalyzer();
            return Diagnostic(analyzer.SupportedDiagnostics.Single(i => i.Id == diagnosticId));
        }

        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor) => new DiagnosticResult(descriptor);

        public static Task VerifyAnalyzerAsync(string source, CancellationToken cancellationToken = default)
            => VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults, cancellationToken);

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
