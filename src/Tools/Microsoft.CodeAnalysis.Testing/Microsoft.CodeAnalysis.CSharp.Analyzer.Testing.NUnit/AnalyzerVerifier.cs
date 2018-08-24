using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Testing.NUnit
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
