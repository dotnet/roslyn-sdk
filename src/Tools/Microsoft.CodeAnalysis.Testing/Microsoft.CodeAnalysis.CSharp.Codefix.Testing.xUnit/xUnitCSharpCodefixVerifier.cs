using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers.xUnit;

namespace Microsoft.CodeAnalysis.CSharp.Codefix.Testing.xUnit
{
    public class XUnitCSharpCodefixVerifier<TAnalyzer, TCodefix> : CSharpCodeFixVerifier<TAnalyzer, TCodefix, XUnitVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodefix  : CodeFixProvider, new()
    {
    }
}
