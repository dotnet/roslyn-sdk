// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.UnitTestFramework
{
    public static class DiagnosticVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        public static DiagnosticResult[] EmptyDiagnosticResults { get; } = { };

        public static DiagnosticResult Diagnostic(string diagnosticId = null)
        {
            TAnalyzer analyzer = new TAnalyzer();
            ImmutableArray<DiagnosticDescriptor> supportedDiagnostics = analyzer.SupportedDiagnostics;
            if (diagnosticId is null)
            {
                return Diagnostic(supportedDiagnostics.Single());
            }
            else
            {
                return Diagnostic(supportedDiagnostics.Single(i => i.Id == diagnosticId));
            }
        }

        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        {
            return new DiagnosticResult(descriptor);
        }

        public static DiagnosticResult CompilerError(string errorIdentifier)
        {
            return new DiagnosticResult(errorIdentifier, DiagnosticSeverity.Error);
        }

        public static Task VerifyCSharpDiagnosticAsync(string source, DiagnosticResult expected, CancellationToken cancellationToken)
            => VerifyCSharpDiagnosticAsync(source, new[] { expected }, cancellationToken);

        public static Task VerifyCSharpDiagnosticAsync(string source, DiagnosticResult[] expected, CancellationToken cancellationToken)
        {
            CSharpTest test = new CSharpTest
            {
                TestCode = source,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync(cancellationToken);
        }

        public class CSharpTest : GenericAnalyzerTest
        {
            public override string Language => LanguageNames.CSharp;

            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
                => new[] { new TAnalyzer() };

            protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
                => Enumerable.Empty<CodeFixProvider>();
        }

        public class VisualBasicTest : GenericAnalyzerTest
        {
            public override string Language => LanguageNames.VisualBasic;

            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
                => new[] { new TAnalyzer() };

            protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
                => Enumerable.Empty<CodeFixProvider>();
        }
    }
}
