Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing.Verifiers.xUnit

Public Class XUnitVisualBasicDiagnosticVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New})
    Inherits VisualBasicDiagnosticVerifier(Of TAnalyzer, XUnitVerifier)
End Class
