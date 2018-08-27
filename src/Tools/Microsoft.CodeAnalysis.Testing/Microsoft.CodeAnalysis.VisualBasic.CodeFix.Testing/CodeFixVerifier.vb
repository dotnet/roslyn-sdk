Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing

Namespace Microsoft.CodeAnalysis.VisualBasic.Testing.Default
    Public Class CodeFixVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New}, TCodeFix As {CodeFixProvider, New})
        Inherits VisualBasicCodeFixVerifier(Of TAnalyzer, TCodeFix, DefaultVerifier)
    End Class
End Namespace
