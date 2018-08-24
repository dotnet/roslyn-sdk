using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Microsoft.CodeAnalysis.CSharp.Testing.NUnit
{
    public class CodefixVerifier<TAnalyzer, TCodefix> : CSharpCodeFixVerifier<TAnalyzer, TCodefix, NUnitVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodefix : CodeFixProvider, new()
    {
    }
}
