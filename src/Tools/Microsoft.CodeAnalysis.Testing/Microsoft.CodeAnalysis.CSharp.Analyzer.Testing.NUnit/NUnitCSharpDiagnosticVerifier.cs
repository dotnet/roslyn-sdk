using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers.NUnit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.NUnit
{
    public class NUnitCSharpDiagnosticVerifier<TAnalyzer> : CSharpDiagnosticVerifier<TAnalyzer, NUnitVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
    }
}
