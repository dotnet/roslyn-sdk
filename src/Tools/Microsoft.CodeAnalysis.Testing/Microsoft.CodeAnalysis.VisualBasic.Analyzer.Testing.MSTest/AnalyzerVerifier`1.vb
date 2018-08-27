Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing.Verifiers

Namespace Microsoft.CodeAnalysis.VisualBasic.Testing.MSTest
    Public Class AnalyzerVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New})
        Inherits VisualBasicAnalyzerVerifier(Of TAnalyzer, MSTestVerifier)
    End Class
End Namespace
