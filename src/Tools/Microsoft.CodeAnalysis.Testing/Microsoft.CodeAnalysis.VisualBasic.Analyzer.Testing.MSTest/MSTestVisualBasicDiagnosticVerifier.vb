Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing.Verifiers.MSTest

Public Class MSTestVisualBasicDiagnosticVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New})
    Inherits VisualBasicDiagnosticVerifier(Of TAnalyzer, MSTestVerifier)
End Class
