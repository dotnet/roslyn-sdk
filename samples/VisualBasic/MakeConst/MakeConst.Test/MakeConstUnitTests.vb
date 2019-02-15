Imports MakeConst
Imports MakeConst.VisualBasic.Tests.TestHelper
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Xunit

Namespace MakeConst.Test
    Public Class UnitTest
        Inherits CodeFixVerifier

        'No diagnostics expected to show up
        <Fact>
        Public Sub TestMethod1()
            Dim test = ""
            VerifyBasicDiagnostic(test)
        End Sub

        'Diagnostic And CodeFix both triggered And checked for
        <Fact>
        Public Sub TestMethod2()


        End Sub

        Protected Overrides Function GetBasicCodeFixProvider() As CodeFixProvider
            Return New MakeConstCodeFixProvider()
        End Function

        Protected Overrides Function GetBasicDiagnosticAnalyzer() As DiagnosticAnalyzer
            Return New MakeConstAnalyzer()
        End Function

    End Class
End Namespace
