using Microsoft.CodeAnalysis.Analyzer.Testing;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Analyzer.Testing
{
    public class CSharpDiagnosticVerifier<TAnalyzer, TVerifier> : DiagnosticVerifier<TAnalyzer, CSharpAnalyzerTest<TAnalyzer, TVerifier>, TVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TVerifier : IVerifier, new()
    {
    }

    public static class CSharpDiagnosticVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        public static CSharpDiagnosticVerifier<TAnalyzer, DefaultVerifier> WithDefaultVerifier()
            => WithVerifier<DefaultVerifier>();

        public static CSharpDiagnosticVerifier<TAnalyzer, TVerifier> WithVerifier<TVerifier>()
            where TVerifier : IVerifier, new()
            => new CSharpDiagnosticVerifier<TAnalyzer, TVerifier>();
    }
}
