Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing.Verifiers.NUnit

Public Class NUnitVisualBasicDiagnosticVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New})
    Inherits VisualBasicDiagnosticVerifier(Of TAnalyzer, NUnitVerifier)
End Class
