// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace APISamples
{
    public class Compilations
    {
        [Fact]
        public void EndToEndCompileAndRun()
        {
            var expression = "6 * 7";
            var text = @"public class Calculator
{
    public static object Evaluate()
    {
        return $;
    } 
}".Replace("$", expression);

            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var compilation = CSharpCompilation.Create(
                "calc.dll",
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                syntaxTrees: new[] { tree },
                references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

            Assembly compiledAssembly;
            using (var stream = new MemoryStream())
            {
                var compileResult = compilation.Emit(stream);
                compiledAssembly = Assembly.Load(stream.GetBuffer());
            }

            var calculator = compiledAssembly.GetType("Calculator");
            var evaluate = calculator.GetMethod("Evaluate");
            var answer = evaluate.Invoke(null, null).ToString();

            Assert.Equal("42", answer);
        }

        [Fact]
        public void GetErrorsAndWarnings()
        {
            var text = @"class Program
{
    static int Main(string[] args)
    {
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var compilation = CSharpCompilation
                .Create("program.exe")
                .AddSyntaxTrees(tree)
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            IEnumerable<Diagnostic> errorsAndWarnings = compilation.GetDiagnostics();
            Assert.Single(errorsAndWarnings);
            var error = errorsAndWarnings.First();
            Assert.Equal(
                "'Program.Main(string[])': not all code paths return a value",
                error.GetMessage(CultureInfo.InvariantCulture));
            var errorLocation = error.Location;
            Assert.Equal(4, error.Location.SourceSpan.Length);
            var programText = errorLocation.SourceTree.GetText();
            Assert.Equal("Main", programText.ToString(errorLocation.SourceSpan));
            var span = error.Location.GetLineSpan();
            Assert.Equal(15, span.StartLinePosition.Character);
            Assert.Equal(2, span.StartLinePosition.Line);
        }
    }
}
