using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers.NUnit;

namespace Microsoft.CodeAnalysis.CSharp.Codefix.Testing.NUnit
{
    public class NUnitCSharpCodefixVerifier<TAnalyzer, TCodefix> : CSharpCodeFixVerifier<TAnalyzer, TCodefix, NUnitVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodefix  : CodeFixProvider, new()
    {
    }
}
