using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers.xUnit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.xUnit
{
    public class XUnitCSharpDiagnosticVerifier<TAnalyzer> : CSharpDiagnosticVerifier<TAnalyzer, XUnitVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
    }
}
