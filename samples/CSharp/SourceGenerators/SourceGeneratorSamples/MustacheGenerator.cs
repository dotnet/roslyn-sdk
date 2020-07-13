using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable IDE0008 // Use explicit type
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
    public class MustacheAttribute: System.Attribute
    {
        public string Name { get; }
        public string Template { get; }
        public string Hash { get; }
        public MustacheAttribute(string name, string template, string hash)
            => (Name, Template, Hash) = (name, template, hash);
    }
";
            context.AddSource("Mustache_MainAttributes__", SourceText.From(attributeSource, Encoding.UTF8));

            var compilation = context.Compilation;

            var options = GetMustacheOptions(compilation);
            var namesSources = SourceFilesFromMustachePaths(options);

            foreach (var (name, source) in namesSources)
            {
                context.AddSource($"Mustache{name}", SourceText.From(source, Encoding.UTF8));
            }
        }

        static IEnumerable<(string, string, string)> GetMustacheOptions(Compilation compilation)
        {
            // Get all CSV attributes
            IEnumerable<SyntaxNode>? allNodes = compilation.SyntaxTrees.SelectMany(s => s.GetRoot().DescendantNodes());
            var allAttributes = allNodes.Where((d) => d.IsKind(SyntaxKind.Attribute)).OfType<AttributeSyntax>();
            var attributes = allAttributes.Where(d => d.Name.ToString() == "Mustache")
                .ToImmutableArray();

            var models = compilation.SyntaxTrees.Select(st => compilation.GetSemanticModel(st));
            foreach (var att in attributes)
            {
                string mustacheName = "", template = "", hash = "";
                int index = 0;

                if(att.ArgumentList is null) throw new Exception("Can't be null here");

                var m = compilation.GetSemanticModel(att.SyntaxTree);

                foreach (var arg in att.ArgumentList.Arguments)
                {
                    var expr = arg.Expression;

                    var t = m.GetTypeInfo(expr);
                    var v = m.GetConstantValue(expr);
                    if(index == 0) {
                        mustacheName = v.ToString();
                    }
                    else if(index == 1) {
                        template = v.ToString();
                    } else { 
                        hash = v.ToString();
                    }
                    index += 1;
                }
                yield return (mustacheName, template, hash);
            }
        }
        static string SourceFileFromMustachePath(string name, string template, string hash)
        {
            var tree = HandlebarsDotNet.Handlebars.Compile(template);
            var @object = Newtonsoft.Json.JsonConvert.DeserializeObject(hash);
            var mustacheText = tree(@object);

            return GenerateMustacheClass(name, mustacheText) ;
        }

        static IEnumerable<(string, string)> SourceFilesFromMustachePaths(IEnumerable<(string, string, string)> pathsData) {

            foreach (var (name, template, hash) in pathsData)
            {
                yield return (name, SourceFileFromMustachePath(name, template, hash));
            }
        }

        private static string GenerateMustacheClass(string className, string mustacheText)
        {

            var sb = new StringBuilder();
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
