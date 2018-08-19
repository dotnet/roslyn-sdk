using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers.MSTest;

namespace Microsoft.CodeAnalysis.CSharp.Codefix.Testing.MSTest
{
    public class MSTestCSharpCodefixVerifier<TAnalyzer, TCodefix> : CSharpCodeFixVerifier<TAnalyzer, TCodefix, MSTestVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodefix  : CodeFixProvider, new()
    {
    }
}
