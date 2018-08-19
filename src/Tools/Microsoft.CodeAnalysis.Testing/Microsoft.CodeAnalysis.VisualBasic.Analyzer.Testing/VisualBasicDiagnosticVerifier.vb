Imports Microsoft.CodeAnalysis.Analyzer.Testing
Imports Microsoft.CodeAnalysis.Diagnostics

Public Class VisualBasicDiagnosticVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New}, TVerifier As {IVerifier, New})
    Inherits DiagnosticVerifier(Of TAnalyzer, VisualBasicAnalyzerTest(Of TAnalyzer, TVerifier), TVerifier)
End Class

Public Class DefaultVisualBasicDiagnosticVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New})
    Inherits DiagnosticVerifier(Of TAnalyzer, VisualBasicAnalyzerTest(Of TAnalyzer, DefaultVerifier), DefaultVerifier)
End Class

Public Class VisualBasicDiagnosticVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New})
    Public Shared Function WithDefaultVerifier() As DefaultVisualBasicDiagnosticVerifier(Of TAnalyzer)
        Return New DefaultVisualBasicDiagnosticVerifier(Of TAnalyzer)
    End Function

    Private Shared Function WithVerifier(Of TVerifier As {IVerifier, New})() As VisualBasicDiagnosticVerifier(Of TAnalyzer, TVerifier)
        Return New VisualBasicDiagnosticVerifier(Of TAnalyzer, TVerifier)
    End Function
End Class
