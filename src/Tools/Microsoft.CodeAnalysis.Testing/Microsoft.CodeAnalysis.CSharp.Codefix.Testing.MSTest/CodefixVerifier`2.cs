using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Microsoft.CodeAnalysis.CSharp.Testing.MSTest
{
    public class CodefixVerifier<TAnalyzer, TCodefix> : CSharpCodeFixVerifier<TAnalyzer, TCodefix, MSTestVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodefix : CodeFixProvider, new()
    {
    }
}
