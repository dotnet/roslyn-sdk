Imports Microsoft.CodeAnalysis.Analyzer.Testing
Imports Microsoft.CodeAnalysis.Codefix.Testing
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics

Public Class VisualBasicCodefixVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New}, TCodeFix As {CodeFixProvider, New}, TVerifier As {IVerifier, New})
    Inherits CodeFixVerifier(Of TAnalyzer, TCodeFix, VisualBasicCodeFixTest(Of TAnalyzer, TCodeFix, TVerifier), TVerifier)
End Class

Public Class DefaultVisualBasicCodefixVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New}, TCodeFix As {CodeFixProvider, New})
    Inherits CodeFixVerifier(Of TAnalyzer, TCodeFix, VisualBasicCodeFixTest(Of TAnalyzer, TCodeFix, DefaultVerifier), DefaultVerifier)
End Class
