Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing.Verifiers.NUnit

Public Class NUnitVisualBasicCodefixVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New}, TCodefix As {CodeFixProvider, New})
    Inherits VisualBasicCodefixVerifier(Of TAnalyzer, TCodefix, NUnitVerifier)
End Class
