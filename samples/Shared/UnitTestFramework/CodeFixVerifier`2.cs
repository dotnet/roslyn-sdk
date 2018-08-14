// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.UnitTestFramework
{
    public static class CodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public static DiagnosticResult[] EmptyDiagnosticResults
            => DiagnosticVerifier<TAnalyzer>.EmptyDiagnosticResults;

        public static DiagnosticResult Diagnostic(string diagnosticId = null)
            => DiagnosticVerifier<TAnalyzer>.Diagnostic(diagnosticId);

        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => DiagnosticVerifier<TAnalyzer>.Diagnostic(descriptor);

        public static DiagnosticResult CompilerError(string errorIdentifier)
            => DiagnosticVerifier<TAnalyzer>.CompilerError(errorIdentifier);

        public static Task VerifyCSharpDiagnosticAsync(string source, DiagnosticResult expected, CancellationToken cancellationToken = default)
            => DiagnosticVerifier<TAnalyzer>.VerifyCSharpDiagnosticAsync(source, expected, cancellationToken);

        public static Task VerifyCSharpDiagnosticAsync(string source, DiagnosticResult[] expected, CancellationToken cancellationToken = default)
            => DiagnosticVerifier<TAnalyzer>.VerifyCSharpDiagnosticAsync(source, expected, cancellationToken);

        public static Task VerifyCSharpFixAsync(string source, DiagnosticResult expected, string fixedSource, CancellationToken cancellationToken = default)
            => VerifyCSharpFixAsync(source, new[] { expected }, fixedSource, cancellationToken);

        public static Task VerifyCSharpFixAsync(string source, DiagnosticResult[] expected, string fixedSource, CancellationToken cancellationToken = default)
        {
            CSharpTest test = new CSharpTest
            {
                TestCode = source,
                FixedCode = fixedSource,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync(cancellationToken);
        }

        public class CSharpTest : DiagnosticVerifier<TAnalyzer>.CSharpTest
        {
            protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
                => new[] { new TCodeFix() };
        }

        public class VisualBasicTest : DiagnosticVerifier<TAnalyzer>.VisualBasicTest
        {
            protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
                => new[] { new TCodeFix() };
        }
    }
}
