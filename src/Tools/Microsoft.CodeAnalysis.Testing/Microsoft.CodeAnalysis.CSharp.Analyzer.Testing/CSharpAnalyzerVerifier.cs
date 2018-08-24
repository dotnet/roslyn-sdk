using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing
{
    public class CSharpAnalyzerVerifier<TAnalyzer, TVerifier> : AnalyzerVerifier<TAnalyzer, CSharpAnalyzerTest<TAnalyzer, TVerifier>, TVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TVerifier : IVerifier, new()
    {
    }
}
