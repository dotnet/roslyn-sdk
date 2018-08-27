Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing.Verifiers

Namespace Microsoft.CodeAnalysis.VisualBasic.Testing.MSTest
    Public Class CodeFixVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New}, TCodeFix As {CodeFixProvider, New})
        Inherits VisualBasicCodeFixVerifier(Of TAnalyzer, TCodeFix, MSTestVerifier)
    End Class
End Namespace
