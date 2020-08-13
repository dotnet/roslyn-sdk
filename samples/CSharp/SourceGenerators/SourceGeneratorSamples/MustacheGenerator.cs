using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Mustache
{
    [Generator]
    public class MustacheGenerator : ISourceGenerator
    {
        public void Execute(SourceGeneratorContext context)
        {
            string attributeSource = @"
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
            context.AddSource("Mustache_MainAttributes__", SourceText.From(attributeSource, Encoding.UTF8));

            Compilation compilation = context.Compilation;

            IEnumerable<(string, string, string)> options = GetMustacheOptions(compilation);
            IEnumerable<(string, string)> namesSources = SourceFilesFromMustachePaths(options);

            foreach ((string name, string source) in namesSources)
            {
                context.AddSource($"Mustache{name}", SourceText.From(source, Encoding.UTF8));
            }
        }

        static IEnumerable<(string, string, string)> GetMustacheOptions(Compilation compilation)
        {
            // Get all Mustache attributes
            IEnumerable<SyntaxNode>? allNodes = compilation.SyntaxTrees.SelectMany(s => s.GetRoot().DescendantNodes());
            IEnumerable<AttributeSyntax> allAttributes = allNodes.Where((d) => d.IsKind(SyntaxKind.Attribute)).OfType<AttributeSyntax>();
            ImmutableArray<AttributeSyntax> attributes = allAttributes.Where(d => d.Name.ToString() == "Mustache")
                .ToImmutableArray();

            IEnumerable<SemanticModel> models = compilation.SyntaxTrees.Select(st => compilation.GetSemanticModel(st));
            foreach (AttributeSyntax att in attributes)
            {
                string mustacheName = "", template = "", hash = "";
                int index = 0;

                if (att.ArgumentList is null) throw new Exception("Can't be null here");

                SemanticModel m = compilation.GetSemanticModel(att.SyntaxTree);

                foreach (AttributeArgumentSyntax arg in att.ArgumentList.Arguments)
                {
                    ExpressionSyntax expr = arg.Expression;

                    TypeInfo t = m.GetTypeInfo(expr);
                    Optional<object> v = m.GetConstantValue(expr);
                    if (index == 0)
                    {
                        mustacheName = v.ToString();
                    }
                    else if (index == 1)
                    {
                        template = v.ToString();
                    }
                    else
                    {
                        hash = v.ToString();
                    }
                    index += 1;
                }
                yield return (mustacheName, template, hash);
            }
        }
        static string SourceFileFromMustachePath(string name, string template, string hash)
        {
            Func<object, string> tree = HandlebarsDotNet.Handlebars.Compile(template);
            object @object = Newtonsoft.Json.JsonConvert.DeserializeObject(hash);
            string mustacheText = tree(@object);

            return GenerateMustacheClass(name, mustacheText);
        }

        static IEnumerable<(string, string)> SourceFilesFromMustachePaths(IEnumerable<(string, string, string)> pathsData)
        {

            foreach ((string name, string template, string hash) in pathsData)
            {
                yield return (name, SourceFileFromMustachePath(name, template, hash));
            }
        }

        private static string GenerateMustacheClass(string className, string mustacheText)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($@"
namespace Mustache {{

    public static partial class Constants {{

        public const string {className} = @""{mustacheText.Replace("\"", "\"\"")}"";
    }}
}}
");
            return sb.ToString();

        }

        public void Initialize(InitializationContext context)
        {
            // No initialization required
        }
    }
}
