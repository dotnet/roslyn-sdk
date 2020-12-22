using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SourceGeneratorSamples
{
    [Generator(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class HelloWorldGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // begin creating the source we'll inject into the users compilation
            StringBuilder sourceBuilder;
            if (context.Compilation.Language == LanguageNames.CSharp)
            {
                sourceBuilder = new StringBuilder(@"
using System;
namespace HelloWorldGenerated
{
    public static class HelloWorld
    {
        public static void SayHello()
        {
            Console.WriteLine(""Hello from generated code!"");
            Console.WriteLine(""The following syntax trees existed in the compilation that created this program:"");
");
            }
            else
            {
                sourceBuilder = new StringBuilder(@"
Imports System
Namespace Global.HelloWorldGenerated
    Public Module HelloWorld
        Public Sub SayHello()
            Console.WriteLine(""Hello from generated code!"")
            Console.WriteLine(""The following syntax trees existed in the compilation that created this program:"")
");
            }

            // using the context, get a list of syntax trees in the users compilation
            IEnumerable<SyntaxTree> syntaxTrees = context.Compilation.SyntaxTrees;

            // add the filepath of each tree to the class we're building
            foreach (SyntaxTree tree in syntaxTrees)
            {
                if (context.Compilation.Language == LanguageNames.CSharp)
                {
                    sourceBuilder.AppendLine($@"Console.WriteLine(@"" - {tree.FilePath}"");");
                }
                else
                {
                    sourceBuilder.AppendLine($@"Console.WriteLine("" - {tree.FilePath}"")");
                }
            }

            // finish creating the source to inject
            if (context.Compilation.Language == LanguageNames.CSharp)
            {
                sourceBuilder.Append(@"
        }
    }
}
");
            }
            else
            {
                sourceBuilder.Append(@"
        End Sub
    End Module
End Namespace
");
            }

            // inject the created source into the users compilation
            context.AddSource("helloWorldGenerated", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required
        }
    }
}
