Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing

Namespace Microsoft.CodeAnalysis.VisualBasic.Testing.Default
    Public Class AnalyzerVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New})
        Inherits VisualBasicAnalyzerVerifier(Of TAnalyzer, DefaultVerifier)
    End Class
End Namespace
