Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.MSTest.CodeFixVerifier(
    Of $saferootprojectname$.$saferootidentifiername$Analyzer,
    $saferootprojectname$.CSharp.CSharp$saferootidentifiername$CodeFixProvider)
Imports VerifyVB = Microsoft.CodeAnalysis.VisualBasic.Testing.MSTest.CodeFixVerifier(
    Of $saferootprojectname$.$saferootidentifiername$Analyzer,
    $saferootprojectname$.VisualBasic.VisualBasic$saferootidentifiername$CodeFixProvider)

Namespace $safeprojectname$
    <TestClass>
    Public Class $saferootidentifiername$UnitTest

        'No diagnostics expected to show up
        <TestMethod>
        Public Async Function TestMethod1_CSharp() As Task
            Dim test = ""
            Await VerifyCS.VerifyAnalyzerAsync(test)
        End Function

        <TestMethod>
        Public Async Function TestMethod1_VisualBasic() As Task
            Dim test = ""
            Await VerifyVB.VerifyAnalyzerAsync(test)
        End Function

        'Diagnostic And CodeFix both triggered And checked for
        <TestMethod>
        Public Async Function TestMethod2_CSharp() As Task

            Dim test = "
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class {|#0:TypeName|}
        {   
        }
    }"

            Dim fixtest = "
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TYPENAME
        {   
        }
    }"

            Dim expected = VerifyCS.Diagnostic("$saferootidentifiername$").WithLocation(0).WithArguments("TypeName")
            Await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest)
        End Function

        <TestMethod>
        Public Async Function TestMethod2_VisualBasic() As Task

            Dim test = "
Class {|#0:TypeName|}

    Sub Main()

    End Sub

End Class"

            Dim fixtest = "
Class TYPENAME

    Sub Main()

    End Sub

End Class"

            Dim expected = VerifyVB.Diagnostic("$saferootidentifiername$").WithLocation(0).WithArguments("TypeName")
            Await VerifyVB.VerifyCodeFixAsync(test, expected, fixtest)
        End Function
    End Class
End Namespace
