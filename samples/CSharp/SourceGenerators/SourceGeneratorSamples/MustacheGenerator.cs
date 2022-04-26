namespace Mustache;

[Generator]
public class MustacheGenerator : IIncrementalGenerator
{
    private const string attributeSource = @"
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple=true)]
    internal sealed class MustacheAttribute: System.Attribute
    {
        public string Name { get; }
        public string Template { get; }
        public string Hash { get; }
        public MustacheAttribute(string name, string template, string hash)
            => (Name, Template, Hash) = (name, template, hash);
    }
";

    static string SourceFileFromMustachePath(string name, string template, string hash)
    {
        Func<object, string> tree = HandlebarsDotNet.Handlebars.Compile(template);
        object @object = Newtonsoft.Json.JsonConvert.DeserializeObject(hash);
        string mustacheText = tree(@object);

        StringBuilder sb = new ();
        sb.Append($@"
namespace Mustache {{

    public static partial class Constants {{

        public const string {name} = @""{mustacheText.Replace("\"", "\"\"")}"";
    }}
}}
");
        return sb.ToString();
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(
            ctx => ctx.AddSource("Mustache_MainAttributes__.g.cs", attributeSource));

        var maybeTemplateData = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (s, t) => s is AttributeSyntax attrib
                                 && attrib.ArgumentList?.Arguments.Count == 3,
            transform: GetTemplateData);

        var templateData = maybeTemplateData
            .Where(t => t is not null)
            .Select((t, _) => t!.Value);

        context.RegisterSourceOutput(templateData,
            (ctx, data) => ctx.AddSource(
                $"{data.name}.g.cs",
                SourceFileFromMustachePath(data.name, data.template, data.hash)));
    }

    internal static (string name, string template, string hash)?
        GetTemplateData(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var attrib = (AttributeSyntax)context.Node;
        
        if(attrib.ArgumentList is not null
           && context.SemanticModel.GetTypeInfo(attrib).Type?.ToDisplayString() == "MustacheAttribute")
        {
            string name = context.SemanticModel.GetConstantValue(attrib.ArgumentList.Arguments[0].Expression).ToString();
            string template = context.SemanticModel.GetConstantValue(attrib.ArgumentList.Arguments[1].Expression).ToString();
            string hash = context.SemanticModel.GetConstantValue(attrib.ArgumentList.Arguments[2].Expression).ToString();
            return (name, template, hash);
        }
        return null;
    }
}
