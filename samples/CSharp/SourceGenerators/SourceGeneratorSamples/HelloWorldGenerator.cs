using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SourceGeneratorSamples;

[Generator]
public class HelloWorldGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Select class syntax nodes and collect their type symbols.
        var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
           predicate: IsClassDeclaration,
           transform: GetTypeSymbols
           ).Collect();

        // Register a function to generate the code using the collected type symbols.
        context.RegisterSourceOutput(classDeclarations, GenerateSource);
    }

    // Predicate function: just pick up the class syntax nodes.
    private bool IsClassDeclaration(SyntaxNode s, CancellationToken t) => s is ClassDeclarationSyntax;

    // Transform function goes from SyntaxNode to ITypeSymbol.
    private ITypeSymbol GetTypeSymbols(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is ITypeSymbol typeSymbol)
            return typeSymbol;

        return null;
    }

    // Main function to generate the source code.
    private void GenerateSource(SourceProductionContext context, ImmutableArray<ITypeSymbol> typeSymbols)
    {
        // Begin creating the source we'll inject into the users compilation.
        StringBuilder sourceBuilder = new(@"
using System;
namespace HelloWorldGenerated
{
    public static class HelloWorld
    {
        public static void SayHello() 
        {
            Console.WriteLine(""Hello from generated code!"");
            Console.WriteLine(""The following classes existed in the compilation that created this program:"");
");

        // Print out each symbol we find.
        foreach (var symbol in typeSymbols)
        {
            // TODO: Why can the symbol be null?
            if (symbol is null)
                continue;

            sourceBuilder.AppendLine($"Console.WriteLine(\"{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}\");");
        }

        // Finish creating the source to inject.
        sourceBuilder.Append(@"
        }
    }
}");
        context.AddSource($"hello_world.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }

}
