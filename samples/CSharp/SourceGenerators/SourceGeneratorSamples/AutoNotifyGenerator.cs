namespace SourceGeneratorSamples;

[Generator]
public class AutoNotifyGenerator : IIncrementalGenerator
{
    private const string attributeText = @"
using System;
namespace AutoNotify
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    [System.Diagnostics.Conditional(""AutoNotifyGenerator_DEBUG"")]
    sealed class AutoNotifyAttribute : Attribute
    {
        public AutoNotifyAttribute()
        {
        }
        public string PropertyName { get; set; }
    }
}
";


    private string? ProcessClass(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields, ISymbol attributeSymbol, ISymbol notifySymbol)
    {
        if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            return null; //TODO: issue a diagnostic that it must be top level

        string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

        // begin building the generated source
        StringBuilder source = new ($@"
namespace {namespaceName}
{{
    public partial class {classSymbol.Name} : {notifySymbol.ToDisplayString()}
    {{
");

        // if the class doesn't implement INotifyPropertyChanged already, add it
        if (!classSymbol.Interfaces.Contains(notifySymbol, SymbolEqualityComparer.Default))
        {
            source.Append("public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;");
        }

        // create properties for each field 
        foreach (IFieldSymbol fieldSymbol in fields)
        {
            ProcessField(source, fieldSymbol, attributeSymbol);
        }

        source.Append("} }");
        return source.ToString();
    }

    private void ProcessField(StringBuilder source, IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
    {
        // get the name and type of the field
        string fieldName = fieldSymbol.Name;
        ITypeSymbol fieldType = fieldSymbol.Type;

        // get the AutoNotify attribute from the field, and any associated data
        AttributeData attributeData = fieldSymbol.GetAttributes().Single(ad => ad.AttributeClass!.Equals(attributeSymbol, SymbolEqualityComparer.Default));
        TypedConstant overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

        string propertyName = chooseName(fieldName, overridenNameOpt);
        if (propertyName.Length == 0 || propertyName == fieldName)
        {
            //TODO: issue a diagnostic that we can't process this field
            return;
        }

        source.Append($@"
public {fieldType} {propertyName} 
{{
    get 
    {{
        return this.{fieldName};
    }}

    set
    {{
        this.{fieldName} = value;
        this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof({propertyName})));
    }}
}}

");

        static string chooseName(string fieldName, TypedConstant overridenNameOpt)
        {
            if (!overridenNameOpt.IsNull)
            {
                return overridenNameOpt.Value!.ToString();
            }

            fieldName = fieldName.TrimStart('_');
            if (fieldName.Length == 0)
                return string.Empty;

            if (fieldName.Length == 1)
                return fieldName.ToUpper();

            return fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
        }

    }

    public void GenerateCode(SourceProductionContext context, (ImmutableArray<IFieldSymbol> symbols, Compilation compilation) source)
    {
        // get the added attribute, and INotifyPropertyChanged
        INamedTypeSymbol attributeSymbol = source.compilation.GetTypeByMetadataName("AutoNotify.AutoNotifyAttribute")!;
        INamedTypeSymbol notifySymbol    = source.compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged")!;

        // group the fields by class, and generate the source
        foreach (IGrouping<INamedTypeSymbol, IFieldSymbol> group in source.symbols.GroupBy<IFieldSymbol, INamedTypeSymbol>(f => f.ContainingType, SymbolEqualityComparer.Default))
        {
           string? classSource = ProcessClass(group.Key, group.ToList(), attributeSymbol, notifySymbol);
           if(classSource is not null)
               context.AddSource($"{group.Key.Name}_autoNotify.g.cs", SourceText.From(classSource, Encoding.UTF8));
        }
    }
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput((i) => i.AddSource("AutoNotifyAttribute", attributeText));

        var fieldsEnum = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, ct) => node is FieldDeclarationSyntax fieldDeclarationSyntax
                && fieldDeclarationSyntax.AttributeLists.Count > 0,
            transform: FieldsTransform);

        var fields = fieldsEnum.SelectMany((en, _) => en);

        var collected = fields.Collect()
                        .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(collected, GenerateCode);
    }


    IEnumerable<IFieldSymbol> FieldsTransform(GeneratorSyntaxContext context, CancellationToken ct)
    {
        var fieldDeclarationSyntax = (FieldDeclarationSyntax) context.Node;

        foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
        {
            IFieldSymbol? fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
            if (fieldSymbol!.GetAttributes().Any(ad => ad.AttributeClass!.ToDisplayString() == "AutoNotify.AutoNotifyAttribute"))
                yield return fieldSymbol;
        }
    }
}
