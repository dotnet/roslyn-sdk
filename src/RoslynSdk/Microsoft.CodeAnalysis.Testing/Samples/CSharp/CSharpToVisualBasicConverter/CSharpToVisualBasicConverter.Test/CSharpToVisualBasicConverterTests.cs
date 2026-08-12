// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using CSharpToVisualBasicConverter;
using CSharpToVisualBasicConverter.UnitTests.TestFiles;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CSharpToVisualBasicConverter.UnitTests.Converting
{
    public class CSharpToVisualBasicConverterTests
    {
        [Fact(Skip = "Not Yet Implemented")]
        public void TestAllConstructs()
        {
            var csharpConstructs = TestFilesHelper.GetFile("AllConstructs.cs");
            var vbActualConstructs = Converter.Convert(SyntaxFactory.ParseSyntaxTree(csharpConstructs));

            var vbActual = vbActualConstructs.ToFullString();
            var vbExpected = TestFilesHelper.GetFile("AllConstructs.txt");
            Assert.Equal(vbExpected, vbActual);
        }

        [Fact]
        public void TestParseAddExpression()
        {
            var csharpCode = "1+2";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal("1 + 2", vbNode);
        }

        [Fact]
        public void TestParseInvocationExpression()
        {
            var csharpCode = " Console . WriteLine ( ) ";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal("Console.WriteLine()", vbNode);
        }

        [Fact]
        public void TestParseLambdaExpression()
        {
            var csharpCode = "a => b + c";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal("Function(a) b + c", vbNode);
        }

        [Fact]
        public void TestParseReturnStatement()
        {
            var csharpCode = " return Console . WriteLine ( ) ; ";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal("Return Console.WriteLine()", vbNode);
        }

        [Fact]
        public void TestParseFieldNoModifier()
        {
            var csharpCode = "class Test { int i; }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(
@"Class Test

    Dim i As Integer
End Class
", vbNode);
        }

        [Fact]
        public void TestParseStaticClass()
        {
            var csharpCode = "static class Test { }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(
@"Module Test
End Module
", vbNode);
        }

        [Fact]
        public void TestParseObjectInitializerTwoInitializers()
        {
            var csharpCode = "new object { X = null, Y = null }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal("New Object With {.X = Nothing, .Y = Nothing}", vbNode);
        }

        [Fact]
        public void TestParseAnonymousTypeTwoInitializers()
        {
            var csharpCode = "new { X = null, Y = null }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal("New With {.X = Nothing, .Y = Nothing}", vbNode);
        }

        [Fact]
        public void TestParseCollectionInitializer()
        {
            var csharpCode = "new Dictionary<int,string> { { 0, \"\"} }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal("New Dictionary(Of Integer, String) From {{0, \"\"}}", vbNode);
        }

        [Fact]
        public void TestParseAbstractClass()
        {
            var csharpCode = "abstract class Test { }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(
@"MustInherit Class Test
End Class
", vbNode);
        }

        [Fact]
        public void TestParseExtensionMethod()
        {
            var csharpCode = "static class Test { public static int Foo(this string s) { } }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(
@"Module Test

    <System.Runtime.CompilerServices.Extension>
    Public Function Foo(s As String) As Integer
    End Function
End Module
", vbNode);
        }

        [Fact(Skip = "Not Yet Implemented")]
        public void TestParseDocComments()
        {
            var csharpCode =
@"
    /// <summary>
    /// On the Insert tab, the galleries include items that are designed to coordinate with the
    /// overall look of your document. You can use these galleries to insert tables, headers,
    /// footers, lists, cover pages, and other document building blocks. When you create pictures,
    /// charts, or diagrams, they also coordinate with your current document look.
    /// </summary>
    class Test
    {
        /// <summary>
        /// You can easily change the formatting of selected text in the document text by choosing a
        /// look for the selected text from the Quick Styles gallery on the Home tab. You can also
        /// format text directly by using the other controls on the Home tab. Most controls offer a
        /// choice of using the look from the current theme or using a format that you specify directly.
        /// </summary>
        void Foo()
        {
        }
    }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(
@"''' <summary>
''' On the Insert tab, the galleries include items that are designed to coordinate with the
''' overall look of your document. You can use these galleries to insert tables, headers,
''' footers, lists, cover pages, and other document building blocks. When you create pictures,
''' charts, or diagrams, they also coordinate with your current document look.
''' </summary>
Class Test

    ''' <summary>
    ''' You can easily change the formatting of selected text in the document text by choosing a
    ''' look for the selected text from the Quick Styles gallery on the Home tab. You can also
    ''' format text directly by using the other controls on the Home tab. Most controls offer a
    ''' choice of using the look from the current theme or using a format that you specify directly.
    ''' </summary>
    Sub Foo()
    End Sub
End Class

", vbNode);
        }

        [Fact]
        public void TestParseExtensionMethodDocComment()
        {
            var csharpCode =
@"static class C
{
    /// <summary>
    /// Method summary
    /// </summary>
    static void M(this object o)
    {
    }
}
";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(
@"Module C

    ''' <summary>
    ''' Method summary
    ''' </summary>
    <System.Runtime.CompilerServices.Extension>
    Sub M(o As Object)
    End Sub
End Module
", vbNode);
        }

        [Fact]
        public void TestForStatement1()
        {
            var csharpCode = "for (int i = 0; i < 10; i++) { }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(@"For i = 0 To 10 - 1
Next", vbNode);
        }

        [Fact]
        public void TestForStatement2()
        {
            var csharpCode = "for (int i = 0; i <= 10; i++) { }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(@"For i = 0 To 10
Next", vbNode);
        }

        [Fact]
        public void TestForStatement3()
        {
            var csharpCode = "for (int i = 0; i <= 10; i += 1) { }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(@"For i = 0 To 10 Step 1
Next", vbNode);
        }

        [Fact]
        public void TestForStatement4()
        {
            var csharpCode = "for (int i = 0; i <= 10; i += 2) { }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(@"For i = 0 To 10 Step 2
Next", vbNode);
        }

        [Fact]
        public void TestForStatement5()
        {
            var csharpCode = "for (var i = 0; i <= 10; i += 2) { }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(@"For i = 0 To 10 Step 2
Next", vbNode);
        }

        [Fact]
        public void TestForStatement6()
        {
            var csharpCode = "for (; i <= 10; i += 2) { }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(@"While i <= 10
    i += 2
End While", vbNode);
        }

        [Fact]
        public void TestForStatement7()
        {
            var csharpCode = "for (var i = 0; ; i += 2) { }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(@"Dim i = 0
While True
    i += 2
End While", vbNode);
        }

        [Fact]
        public void TestForStatement8()
        {
            var csharpCode = "for (var i = 0; i <= 10; ) { }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(@"Dim i = 0
While i <= 10
End While", vbNode);
        }

        [Fact]
        public void TestForStatement9()
        {
            var csharpCode = "for (int i = 0; i <= 10; i++) Console.WriteLine(a);";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(@"For i = 0 To 10
    Console.WriteLine(a)
Next", vbNode);
        }

        [Fact]
        public void TestForStatement10()
        {
            var csharpCode = "for (int i = 0; i <= 10; i++) { Console.WriteLine(a); }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(@"For i = 0 To 10
    Console.WriteLine(a)
Next", vbNode);
        }

        [Fact]
        public void TestForStatement11()
        {
            var csharpCode = "for (int i = 0; i <= 10; i++) { Console.WriteLine(a); Console.WriteLine(b); }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(@"For i = 0 To 10
    Console.WriteLine(a)
    Console.WriteLine(b)
Next", vbNode);
        }

        [Fact]
        public void TestForStatement12()
        {
            var csharpCode = "for (int i = x; i >= 0; i--) { }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(@"For i = x To 0 Step -1
Next", vbNode);
        }

        [Fact]
        public void TestForStatement13()
        {
            var csharpCode = "for (int i = x; i > y; i -= 2) { }";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(@"For i = x To y + 1 Step -2
Next", vbNode);
        }

        [Fact]
        public void TestAsyncModifier()
        {
            var csharpCode =
@"
class C 
{
    async void M()
    {
    }

    async Task N()
    {
    }

    async Task<int> O()
    {
    }
}";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(
@"Class C

    Async Sub M()
    End Sub

    Async Function N() As Task
    End Function

    Async Function O() As Task(Of Integer)
    End Function
End Class
",
vbNode);
        }

        [Fact(Skip = "Not Yet Implemented")]
        public void TestAwaitExpression()
        {
            var csharpCode =
@"async void Button1_Click(object sender, EventArgs e)
{
    ResultsTextBox.Text = await httpClient.DownloadStringTaskAsync(""http://somewhere.com/"");
}";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(
@"Async Sub Button1_Click(sender As Object, e As EventArgs)
    ResultsTextBox.Text = Await httpClient.DownloadStringTaskAsync(""http://somewhere.com/"")
End Sub
",
vbNode);
        }

        [Fact(Skip = "Not Yet Implemented")]
        public void TestAwaitStatement()
        {
            var csharpCode =
@"async void Button1_Click(object sender, EventArgs e)
{
    await BeepAsync();
}";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(
@"Async Sub Button1_Click(sender As Object, e As EventArgs)
    Await BeepAsync()
End Sub
",
vbNode);
        }

        [Fact(Skip = "Not Yet Implemented")]
        public void TestAsyncLambdas()
        {
            // TODO: In C#, whether an async lambda is void returning or Task returning cannot be determined syntactically.
            //       When semantic-aware translation is implemented we should revisit this to ensure that we translate accurately.
            //       In the meantime let's prefer to translate async lambdas as Async Function lambdas in VB, since they're most common
            //       and recommended.
            var csharpCode =
@"void M()
{
    Task.Run(async () => {});
    Task.Run(async () => await NAsync());
}";
            var vbNode = Converter.Convert(csharpCode);

            Assert.Equal(
@"Sub M()
    Task.Run(Async Function()
    End Function)
    Task.Run(Async Function() Await NAsync())
End Sub
",
vbNode);
        }
    }
}
