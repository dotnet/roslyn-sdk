Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing.Verifiers.MSTest

Public Class MSTestVisualBasicCodefixVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New}, TCodefix As {CodeFixProvider, New})
    Inherits VisualBasicCodefixVerifier(Of TAnalyzer, TCodefix, MSTestVerifier)
End Class
