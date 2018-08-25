Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing.Verifiers

Namespace Microsoft.CodeAnalysis.VisualBasic.Testing.NUnit
    Public Class AnalyzerVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New})
        Inherits VisualBasicAnalyzerVerifier(Of TAnalyzer, NUnitVerifier)
    End Class
End Namespace
