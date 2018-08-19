using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Analyzer.Testing;

namespace Microsoft.CodeAnalysis.Codefix.Testing
{
    public class CodeFixVerifier<TAnalyzer, TCodeFix, TTest, TVerifier>
           where TAnalyzer : DiagnosticAnalyzer, new()
           where TCodeFix : CodeFixProvider, new()
           where TTest : BaseCodeFixTest<TVerifier>, new()
           where TVerifier : IVerifier, new()
    {
        public static DiagnosticResult[] EmptyDiagnosticResults
            => DiagnosticVerifier<TAnalyzer, TTest, TVerifier>.EmptyDiagnosticResults;

        public static DiagnosticResult Diagnostic(string diagnosticId = null)
            => DiagnosticVerifier<TAnalyzer, TTest, TVerifier>.Diagnostic(diagnosticId);

        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => DiagnosticVerifier<TAnalyzer, TTest, TVerifier>.Diagnostic(descriptor);

        public static DiagnosticResult CompilerError(string errorIdentifier)
            => DiagnosticVerifier<TAnalyzer, TTest, TVerifier>.CompilerError(errorIdentifier);

        public static Task VerifyDiagnosticAsync(string source, DiagnosticResult expected, CancellationToken cancellationToken = default)
            => DiagnosticVerifier<TAnalyzer, TTest, TVerifier>.VerifyDiagnosticAsync(source, expected, cancellationToken);

        public static Task VerifyFixAsync(string source, DiagnosticResult expected, string fixedSource, CancellationToken cancellationToken = default)
            => VerifyixAsync(source, new[] { expected }, fixedSource, cancellationToken);

        public static Task VerifyixAsync(string source, DiagnosticResult[] expected, string fixedSource, CancellationToken cancellationToken = default)
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
