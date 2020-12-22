using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using CSharp = Microsoft.CodeAnalysis.CSharp;
using CSharpSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VisualBasic = Microsoft.CodeAnalysis.VisualBasic;
using VisualBasicSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

#nullable enable

namespace Mustache
{
    [Generator(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class MustacheGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            string attributeSource;
            if (context.Compilation.Language == LanguageNames.CSharp)
            {
                attributeSource = @"
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
            }
            else
            {
                attributeSource = @"
Namespace Global
    <System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple:=True)>
    Friend NotInheritable Class MustacheAttribute
        Inherits System.Attribute

        Public ReadOnly Property Name As String
        Public ReadOnly Property Template As String
        Public ReadOnly Property Hash As String
        Public Sub New(name As String, template As String, hash As String)
            Me.Name = name
            Me.Template = template
            Me.Hash = hash
        End Sub
    End Class
End Namespace
";
            }

            context.AddSource("Mustache_MainAttributes__", SourceText.From(attributeSource, Encoding.UTF8));

            Compilation compilation = context.Compilation;

            IEnumerable<(string, string, string)> options = GetMustacheOptions(compilation);
            IEnumerable<(string, string)> namesSources = SourceFilesFromMustachePaths(compilation.Language, options);

            foreach ((string name, string source) in namesSources)
            {
                context.AddSource($"Mustache{name}", SourceText.From(source, Encoding.UTF8));
            }
        }

        static IEnumerable<(string, string, string)> GetMustacheOptions(Compilation compilation)
        {
            // Get all Mustache attributes
            IEnumerable<SyntaxNode>? allNodes = compilation.SyntaxTrees.SelectMany(s => s.GetRoot().DescendantNodes());
            if (compilation.Language == LanguageNames.CSharp)
            {
                IEnumerable<CSharpSyntax.AttributeSyntax> allAttributes = allNodes.Where((d) => d.IsKind(CSharp.SyntaxKind.Attribute)).OfType<CSharpSyntax.AttributeSyntax>();
                ImmutableArray<CSharpSyntax.AttributeSyntax> attributes = allAttributes.Where(d => d.Name.ToString() == "Mustache")
                    .ToImmutableArray();

                IEnumerable<SemanticModel> models = compilation.SyntaxTrees.Select(st => compilation.GetSemanticModel(st));
                foreach (CSharpSyntax.AttributeSyntax att in attributes)
                {
                    string mustacheName = "", template = "", hash = "";
                    int index = 0;

                    if (att.ArgumentList is null) throw new Exception("Can't be null here");

                    SemanticModel m = compilation.GetSemanticModel(att.SyntaxTree);

                    foreach (CSharpSyntax.AttributeArgumentSyntax arg in att.ArgumentList.Arguments)
                    {
                        CSharpSyntax.ExpressionSyntax expr = arg.Expression;

                        TypeInfo t = m.GetTypeInfo(expr);
                        Optional<object?> v = m.GetConstantValue(expr);
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
            else
            {
                IEnumerable<VisualBasicSyntax.AttributeSyntax> allAttributes = allNodes.Where((d) => d.IsKind(VisualBasic.SyntaxKind.Attribute)).OfType<VisualBasicSyntax.AttributeSyntax>();
                ImmutableArray<VisualBasicSyntax.AttributeSyntax> attributes = allAttributes.Where(d => d.Name.ToString() == "Mustache")
                    .ToImmutableArray();

                IEnumerable<SemanticModel> models = compilation.SyntaxTrees.Select(st => compilation.GetSemanticModel(st));
                foreach (VisualBasicSyntax.AttributeSyntax att in attributes)
                {
                    string mustacheName = "", template = "", hash = "";
                    int index = 0;

                    if (att.ArgumentList is null) throw new Exception("Can't be null here");

                    SemanticModel m = compilation.GetSemanticModel(att.SyntaxTree);

                    foreach (VisualBasicSyntax.ArgumentSyntax arg in att.ArgumentList.Arguments)
                    {
                        if (!(arg is VisualBasicSyntax.SimpleArgumentSyntax { Expression: var expr }))
                        {
                            continue;
                        }

                        TypeInfo t = m.GetTypeInfo(expr);
                        Optional<object?> v = m.GetConstantValue(expr);
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
        }
        static string SourceFileFromMustachePath(string languageName, string name, string template, string hash)
        {
            Func<object, string> tree = HandlebarsDotNet.Handlebars.Compile(template);
            object @object = Newtonsoft.Json.JsonConvert.DeserializeObject(hash);
            string mustacheText = tree(@object);

            return GenerateMustacheClass(languageName, name, mustacheText);
        }

        static IEnumerable<(string, string)> SourceFilesFromMustachePaths(string languageName, IEnumerable<(string, string, string)> pathsData)
        {
            foreach ((string name, string template, string hash) in pathsData)
            {
                yield return (name, SourceFileFromMustachePath(languageName, name, template, hash));
            }
        }

        private static string GenerateMustacheClass(string languageName, string className, string mustacheText)
        {
            StringBuilder sb = new StringBuilder();
            if (languageName == LanguageNames.CSharp)
            {
                sb.Append($@"
namespace Mustache {{

    public static partial class Constants {{

        public const string {className} = @""{mustacheText.Replace("\"", "\"\"")}"";
    }}
}}
");
            }
            else
            {
                sb.Append($@"
Namespace Global.Mustache
    Partial Public Module Constants
        Public Const {className} As String = ""{mustacheText.Replace("\"", "\"\"")}""
    End Module
End Namespace
");
            }

            return sb.ToString();

        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required
        }
    }
}
