Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing.Verifiers

Public Class AnalyzerVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New})
    Inherits VisualBasicAnalyzerVerifier(Of TAnalyzer, NUnitVerifier)
End Class
