using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using CSharpSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VisualBasicSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace SourceGeneratorSamples
{
    [Generator(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class AutoNotifyGenerator : ISourceGenerator
    {
        private const string attributeTextCSharp = @"
using System;
namespace AutoNotify
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class AutoNotifyAttribute : Attribute
    {
        public AutoNotifyAttribute()
        {
        }
        public string PropertyName { get; set; }
    }
}
";
        private const string attributeTextVisualBasic = @"
Imports System

Namespace Global.AutoNotify
    <AttributeUsage(AttributeTargets.Field, Inherited:=False, AllowMultiple:=False)>
    Friend NotInheritable Class AutoNotifyAttribute
        Inherits Attribute

        Public Sub New()
        End Sub

        Public Property PropertyName As String
    End Class
End Namespace
";

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // add the attribute text
            context.AddSource("AutoNotifyAttribute", SourceText.From(context.Compilation.Language == LanguageNames.CSharp ? attributeTextCSharp : attributeTextVisualBasic, Encoding.UTF8));

            // retreive the populated receiver 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            // we're going to create a new compilation that contains the attribute.
            // TODO: we should allow source generators to provide source during initialize, so that this step isn't required.
            ParseOptions options = context.Compilation.SyntaxTrees.First().Options;
            Compilation compilation = context.Compilation.AddSyntaxTrees(
                context.Compilation.Language == LanguageNames.CSharp
                ? CSharpSyntaxTree.ParseText(SourceText.From(attributeTextCSharp, Encoding.UTF8), (CSharpParseOptions)options)
                : VisualBasicSyntaxTree.ParseText(SourceText.From(attributeTextVisualBasic, Encoding.UTF8), (VisualBasicParseOptions)options));

            // get the newly bound attribute, and INotifyPropertyChanged
            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("AutoNotify.AutoNotifyAttribute");
            INamedTypeSymbol notifySymbol = compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged");

            // loop over the candidate fields, and keep the ones that are actually annotated
            List<IFieldSymbol> fieldSymbols = new List<IFieldSymbol>();
            foreach (CSharpSyntax.FieldDeclarationSyntax field in receiver.CSharpCandidateFields)
            {
                SemanticModel model = compilation.GetSemanticModel(field.SyntaxTree);
                foreach (CSharpSyntax.VariableDeclaratorSyntax variable in field.Declaration.Variables)
                {
                    // Get the symbol being decleared by the field, and keep it if its annotated
                    IFieldSymbol fieldSymbol = model.GetDeclaredSymbol(variable) as IFieldSymbol;
                    if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
                    {
                        fieldSymbols.Add(fieldSymbol);
                    }
                }
            }

            foreach (VisualBasicSyntax.FieldDeclarationSyntax field in receiver.VisualBasicCandidateFields)
            {
                SemanticModel model = compilation.GetSemanticModel(field.SyntaxTree);
                foreach (VisualBasicSyntax.VariableDeclaratorSyntax variable in field.Declarators)
                {
                    foreach (VisualBasicSyntax.ModifiedIdentifierSyntax name in variable.Names)
                    {
                        // Get the symbol being decleared by the field, and keep it if its annotated
                        IFieldSymbol fieldSymbol = model.GetDeclaredSymbol(name) as IFieldSymbol;
                        if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
                        {
                            fieldSymbols.Add(fieldSymbol);
                        }
                    }
                }
            }

            // group the fields by class, and generate the source
            foreach (IGrouping<INamedTypeSymbol, IFieldSymbol> group in fieldSymbols.GroupBy(f => f.ContainingType))
            {
                string classSource = ProcessClass(group.Key, group.ToList(), attributeSymbol, notifySymbol, context);
                string extension = compilation.Language == LanguageNames.CSharp ? "cs" : "vb";
                context.AddSource($"{group.Key.Name}_autoNotify.{extension}", SourceText.From(classSource, Encoding.UTF8));
            }
        }

        private string ProcessClass(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields, ISymbol attributeSymbol, ISymbol notifySymbol, GeneratorExecutionContext context)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return null; //TODO: issue a diagnostic that it must be top level
            }

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            // begin building the generated source
            StringBuilder source;
            if (context.Compilation.Language == LanguageNames.CSharp)
            {
                source = new StringBuilder($@"
namespace {namespaceName}
{{
    public partial class {classSymbol.Name} : {notifySymbol.ToDisplayString()}
    {{
");
            }
            else
            {
                source = new StringBuilder($@"
Namespace Global.{namespaceName}
    Partial Public Class {classSymbol.Name}
        Implements {notifySymbol.ToDisplayString()}
");
            }

            // if the class doesn't implement INotifyPropertyChanged already, add it
            if (!classSymbol.Interfaces.Contains(notifySymbol))
            {
                if (context.Compilation.Language == LanguageNames.CSharp)
                {
                    source.Append("public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;");
                }
                else
                {
                    source.Append("Public Event PropertyChanged As System.ComponentModel.PropertyChangedEventHandler Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged");
                }
            }

            // create properties for each field 
            foreach (IFieldSymbol fieldSymbol in fields)
            {
                ProcessField(source, context.Compilation.Language, fieldSymbol, attributeSymbol);
            }

            if (context.Compilation.Language == LanguageNames.CSharp)
            {
                source.Append("} }");
            }
            else
            {
                source.Append(@"
    End Class
End Namespace
");
            }

            return source.ToString();
        }

        private void ProcessField(StringBuilder source, string language, IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
        {
            // get the name and type of the field
            string fieldName = fieldSymbol.Name;
            ITypeSymbol fieldType = fieldSymbol.Type;

            // get the AutoNotify attribute from the field, and any associated data
            AttributeData attributeData = fieldSymbol.GetAttributes().Single(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
            TypedConstant overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

            string propertyName = chooseName(fieldName, overridenNameOpt);
            if (propertyName.Length == 0 || propertyName == fieldName)
            {
                //TODO: issue a diagnostic that we can't process this field
                return;
            }

            if (language == LanguageNames.CSharp)
            {
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
            }
            else
            {
                source.Append($@"
Public Property {propertyName} As {fieldType}
    Get
        Return Me.{fieldName}
    End Get

    Set(value As {fieldType})
        Me.{fieldName} = value
        RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs(NameOf({propertyName})))
    End Set
End Property

");
            }

            string chooseName(string fieldName, TypedConstant overridenNameOpt)
            {
                if (!overridenNameOpt.IsNull)
                {
                    return overridenNameOpt.Value.ToString();
                }

                fieldName = fieldName.TrimStart('_');
                if (fieldName.Length == 0)
                    return string.Empty;

                if (fieldName.Length == 1)
                    return fieldName.ToUpper();

                return fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
            }

        }

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        class SyntaxReceiver : ISyntaxReceiver
        {
            public List<CSharpSyntax.FieldDeclarationSyntax> CSharpCandidateFields { get; } = new List<CSharpSyntax.FieldDeclarationSyntax>();

            public List<VisualBasicSyntax.FieldDeclarationSyntax> VisualBasicCandidateFields { get; } = new List<VisualBasicSyntax.FieldDeclarationSyntax>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // any field with at least one attribute is a candidate for property generation
                if (syntaxNode is CSharpSyntax.FieldDeclarationSyntax csharpFieldDeclarationSyntax
                    && csharpFieldDeclarationSyntax.AttributeLists.Count > 0)
                {
                    CSharpCandidateFields.Add(csharpFieldDeclarationSyntax);
                }
                else if (syntaxNode is VisualBasicSyntax.FieldDeclarationSyntax visualBasicFieldDeclarationSyntax
                    && visualBasicFieldDeclarationSyntax.AttributeLists.Count > 0)
                {
                    VisualBasicCandidateFields.Add(visualBasicFieldDeclarationSyntax);
                }
            }
        }
    }
}
