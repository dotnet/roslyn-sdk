using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.MSTest.CodeFixVerifier<
    $saferootprojectname$.$saferootidentifiername$Analyzer,
    $saferootprojectname$.CSharp$saferootidentifiername$CodeFixProvider>;
using VerifyVB = Microsoft.CodeAnalysis.VisualBasic.Testing.MSTest.CodeFixVerifier<
    $saferootprojectname$.$saferootidentifiername$Analyzer,
    $saferootprojectname$.VisualBasic$saferootidentifiername$CodeFixProvider>;

namespace $safeprojectname$
{
    [TestClass]
    public class $saferootidentifiername$UnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1_CSharp()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestMethod1_VisualBasic()
        {
            var test = @"";

            await VerifyVB.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2_CSharp()
        {
            var test = @"
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
    }";

            var fixtest = @"
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
    }";

            var expected = VerifyCS.Diagnostic("$saferootidentifiername$").WithLocation(0).WithArguments("TypeName");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task TestMethod2_VisualBasic()
        {
            var test = @"
Class {|#0:TypeName|}

    Sub Main()

    End Sub

End Class";

            var fixtest = @"
Class TYPENAME

    Sub Main()

    End Sub

End Class";

            var expected = VerifyVB.Diagnostic("$saferootidentifiername$").WithLocation(0).WithArguments("TypeName");
            await VerifyVB.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
