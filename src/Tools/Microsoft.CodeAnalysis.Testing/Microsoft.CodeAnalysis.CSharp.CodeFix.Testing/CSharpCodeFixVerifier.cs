using Microsoft.CodeAnalysis.Analyzer.Testing;
using Microsoft.CodeAnalysis.Codefix.Testing;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Codefix.Testing
{
    public class CSharpCodeFixVerifier<TAnalyzer, TCodeFix, TVerifier> : CodeFixVerifier<TAnalyzer, TCodeFix, CSharpCodeFixTest<TAnalyzer, TCodeFix, TVerifier>, TVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix  : CodeFixProvider, new()
        where TVerifier : IVerifier, new()
    {
    }

    public class DefaultCSharpCodeFixVerifier<TAnalyzer, TCodeFix> : CodeFixVerifier<TAnalyzer, TCodeFix, CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>, DefaultVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
    }
}
