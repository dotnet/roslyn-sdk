namespace SourceGeneratorSamples;

[Generator]
public class HelloWorldGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Use this method to generate constant code (aka that doesn't depend on the syntax tree).
        context.RegisterPostInitializationOutput(context =>
            context.AddSource("AStaticFunc.g.cs", @"
                namespace HelloWorldGenerated;
                static class Printer {
                    internal static void PrintUpper(string s) => System.Console.WriteLine(s.ToUpper());
                }")
        );

        // Dependency pipeline. Select class syntax nodes, transform to type symbols and collect their names.
        var classNames = context.SyntaxProvider.CreateSyntaxProvider(
           predicate: IsClassDeclaration,
           transform: GetTypeSymbol
           ).Where(t => t is not null)
            .Select((t, c) => t!.Name)
            .Collect();

        // Register a function to generate the code using the collected type symbols.
        context.RegisterSourceOutput(classNames, GenerateSource);
    }

    // Predicate function: just pick up the class syntax nodes.
    private bool IsClassDeclaration(SyntaxNode s, CancellationToken t) => s is ClassDeclarationSyntax;

    // Transform function goes from SyntaxNode to ITypeSymbol.
    private ITypeSymbol? GetTypeSymbol(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is ITypeSymbol typeSymbol)
            return typeSymbol;

        return null;
    }

    // Main function to generate the source code.
    private void GenerateSource(SourceProductionContext context, ImmutableArray<string> typeNames)
    {
        // Begin creating the source we'll inject into the users compilation.
        StringBuilder sourceBuilder = new(@"
using System;
namespace HelloWorldGenerated;
    public static class HelloWorld
    {
        public static void SayHello() 
        {
            Printer.PrintUpper(""Hello from generated code!""); // Uses the Printer static class
            Console.WriteLine(""The following classes existed in the compilation that created this program:"");
");

        // Print out each symbol name we find.
        foreach (var name in typeNames)
            sourceBuilder.AppendLine($"Console.WriteLine(\"{name}\");");

        // Finish creating the source to inject.
        sourceBuilder.Append(@"
        }
    }");
        context.AddSource($"hello_world.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }

}
