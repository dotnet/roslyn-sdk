﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing
{
    public class CodeFixVerifier<TAnalyzer, TCodeFix, TTest, TVerifier>
           where TAnalyzer : DiagnosticAnalyzer, new()
           where TCodeFix : CodeFixProvider, new()
           where TTest : CodeFixTest<TVerifier>, new()
           where TVerifier : IVerifier, new()
    {
        public static DiagnosticResult[] EmptyDiagnosticResults
            => AnalyzerVerifier<TAnalyzer, TTest, TVerifier>.EmptyDiagnosticResults;

        public static DiagnosticResult Diagnostic(string diagnosticId = null)
            => AnalyzerVerifier<TAnalyzer, TTest, TVerifier>.Diagnostic(diagnosticId);

        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => AnalyzerVerifier<TAnalyzer, TTest, TVerifier>.Diagnostic(descriptor);

        public static DiagnosticResult CompilerError(string errorIdentifier)
            => AnalyzerVerifier<TAnalyzer, TTest, TVerifier>.CompilerError(errorIdentifier);

        public static Task VerifyAnalyzerAsync(string source, DiagnosticResult expected, CancellationToken cancellationToken = default)
            => AnalyzerVerifier<TAnalyzer, TTest, TVerifier>.VerifyAnalyzerAsync(source, expected, cancellationToken);

        public static Task VerifyAnalyzerAsync(string source, DiagnosticResult[] expected, CancellationToken cancellationToken = default)
            => AnalyzerVerifier<TAnalyzer, TTest, TVerifier>.VerifyAnalyzerAsync(source, expected, cancellationToken);

        public static Task VerifyFCodeFixAsync(string source, DiagnosticResult expected, string fixedSource, CancellationToken cancellationToken = default)
            => VerifyFCodeFixAsync(source, new[] { expected }, fixedSource, cancellationToken);

        public static Task VerifyFCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource, CancellationToken cancellationToken = default)
        {
            var test = new TTest
            {
                TestCode = source,
                FixedCode = fixedSource,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync(cancellationToken);
        }
    }
}
