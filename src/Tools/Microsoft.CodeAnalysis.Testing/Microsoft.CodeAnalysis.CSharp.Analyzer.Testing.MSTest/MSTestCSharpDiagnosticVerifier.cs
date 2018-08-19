using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers.MSTest;

namespace Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.MSTest
{
    public class MSTestCSharpDiagnosticVerifier<TAnalyzer> : CSharpDiagnosticVerifier<TAnalyzer, MSTestVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
    }
}
