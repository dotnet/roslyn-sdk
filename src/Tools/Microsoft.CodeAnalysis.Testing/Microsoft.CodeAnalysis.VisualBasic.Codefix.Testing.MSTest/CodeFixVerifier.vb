Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics

Module CodeFixVerifier
    Public Function Create(Of TAnalyzer As {DiagnosticAnalyzer, New}, TCodefix As {CodeFixProvider, New})() As CodeFixVerifier(Of TAnalyzer, TCodefix)
        Return New CodeFixVerifier(Of TAnalyzer, TCodefix)
    End Function
End Module
