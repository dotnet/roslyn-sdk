Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.VisualBasic.Testing.MSTest
    Module CodeFixVerifier
        Public Function Create(Of TAnalyzer As {DiagnosticAnalyzer, New}, TCodeFix As {CodeFixProvider, New})() As CodeFixVerifier(Of TAnalyzer, TCodeFix)
            Return New CodeFixVerifier(Of TAnalyzer, TCodeFix)
        End Function
    End Module
End Namespace
