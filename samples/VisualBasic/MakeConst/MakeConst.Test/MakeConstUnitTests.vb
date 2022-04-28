﻿Imports Xunit
Imports Verify = Microsoft.CodeAnalysis.VisualBasic.Testing.XUnit.CodeFixVerifier(Of MakeConst.VisualBasic.MakeConstAnalyzer, MakeConst.VisualBasic.MakeConstCodeFixProvider)

Namespace MakeConst.Test
    Public Class UnitTest

        'No diagnostics expected to show up
        <Fact>
        Public Async Function TestMethod1() As Task
            Dim test = ""
            Await Verify.VerifyAnalyzerAsync(test)
        End Function

        'Diagnostic And CodeFix both triggered And checked for
        <Fact>
        Public Sub TestMethod2()


        End Sub

    End Class
End Namespace
