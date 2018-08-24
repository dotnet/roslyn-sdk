using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Testing.MSTest
{
    public static class AnalyzerVerifier
    {
        public static AnalyzerVerifier<TAnalyzer> Create<TAnalyzer>()
            where TAnalyzer : DiagnosticAnalyzer, new()
        {
            return new AnalyzerVerifier<TAnalyzer>();
        }
    }
}
