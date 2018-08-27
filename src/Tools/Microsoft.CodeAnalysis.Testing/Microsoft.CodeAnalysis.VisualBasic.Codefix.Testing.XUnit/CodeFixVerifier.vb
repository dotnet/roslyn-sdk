Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.VisualBasic.Testing.XUnit
    Module CodeFixVerifier
        Public Function Create(Of TAnalyzer As {DiagnosticAnalyzer, New}, TCodefix As {CodeFixProvider, New})() As CodefixVerifier(Of TAnalyzer, TCodefix)
            Return New CodefixVerifier(Of TAnalyzer, TCodefix)
        End Function
    End Module
End Namespace
