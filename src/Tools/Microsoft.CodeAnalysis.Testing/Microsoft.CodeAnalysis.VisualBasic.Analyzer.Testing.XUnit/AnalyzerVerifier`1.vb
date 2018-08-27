Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing.Verifiers

Namespace Microsoft.CodeAnalysis.VisualBasic.Testing.XUnit
    Public Class AnalyzerVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New})
        Inherits VisualBasicAnalyzerVerifier(Of TAnalyzer, XUnitVerifier)
    End Class
End Namespace
