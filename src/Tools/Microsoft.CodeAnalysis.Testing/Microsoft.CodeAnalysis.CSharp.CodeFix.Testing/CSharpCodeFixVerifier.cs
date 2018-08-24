using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing
{
    public class CSharpCodeFixVerifier<TAnalyzer, TCodeFix, TVerifier> : CodeFixVerifier<TAnalyzer, TCodeFix, CSharpCodeFixTest<TAnalyzer, TCodeFix, TVerifier>, TVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix  : CodeFixProvider, new()
        where TVerifier : IVerifier, new()
    {
    }
}
