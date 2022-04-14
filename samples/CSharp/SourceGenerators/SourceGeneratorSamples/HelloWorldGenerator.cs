namespace SourceGeneratorSamples;

[Generator]
public class HelloWorldGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Select class syntax nodes, transform to type symbols and collect their names.
        var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
           predicate: IsClassDeclaration,
           transform: GetTypeSymbols
           ).Select((t, c) => t.Name)
            .Collect();

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
    private void GenerateSource(SourceProductionContext context, ImmutableArray<string> typeNames)
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

        // Print out each symbol name we find.
        foreach (var name in typeNames)
        {
            // TODO: Why can the symbol be null?
            if (name is null)
                continue;

            sourceBuilder.AppendLine($"Console.WriteLine(\"{name}\");");
        }

        // Finish creating the source to inject.
        sourceBuilder.Append(@"
        }
    }
}");
        context.AddSource($"hello_world.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }

}
