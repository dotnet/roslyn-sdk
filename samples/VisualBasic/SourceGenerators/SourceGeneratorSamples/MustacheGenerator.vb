Option Explicit On
Option Infer On
Option Strict On

Imports Microsoft.CodeAnalysis

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace SourceGeneratorSamples

    <Generator(LanguageNames.VisualBasic)>
    Public Class MustacheGenerator
        Implements ISourceGenerator

        Const attributeSource = "Option Explicit On
Option Strict On
Option Infer On

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
"

        Public Sub Execute(context As GeneratorExecutionContext) Implements ISourceGenerator.Execute
            Dim rx = CType(context.SyntaxContextReceiver, SyntaxReceiver)
            For Each element In rx.TemplateInfo
                Dim source = SourceFileFromMustachePath(element.name, element.template, element.hash)
                context.AddSource($"Mustache{element.name}", source)
            Next
        End Sub

        Private Shared Function SourceFileFromMustachePath(name As String, template As String, hash As String) As String
            Dim tree = HandlebarsDotNet.Handlebars.Compile(template)
            Dim [object] = Newtonsoft.Json.JsonConvert.DeserializeObject(hash)
            Dim mustacheText = tree([object])

            Return $"
Namespace Global.Mustache

    Partial Public Module Constants

        Public Const {name} As String = ""{mustacheText.Replace("""", """""")}""

    End Module

End Namespace"
        End Function

        Public Sub Initialize(context As GeneratorInitializationContext) Implements ISourceGenerator.Initialize
            context.RegisterForPostInitialization(Sub(pi) pi.AddSource("Mustache_MainAttributes__", attributeSource))
            context.RegisterForSyntaxNotifications(Function() New SyntaxReceiver())
        End Sub

        Private Class SyntaxReceiver
            Implements ISyntaxContextReceiver

            Public TemplateInfo As New List(Of (name As String, template As String, hash As String))

            Public Sub OnVisitSyntaxNode(context As GeneratorSyntaxContext) Implements ISyntaxContextReceiver.OnVisitSyntaxNode
                If context.Node.Kind() = SyntaxKind.Attribute Then
                    Dim attrib = CType(context.Node, AttributeSyntax)
                    If attrib.ArgumentList?.Arguments.Count = 3 AndAlso
                       context.SemanticModel.GetTypeInfo(attrib).Type?.ToDisplayString() = "MustacheAttribute" Then
                        Dim name = context.SemanticModel.GetConstantValue(attrib.ArgumentList.Arguments(0).GetExpression).ToString()
                        Dim template = context.SemanticModel.GetConstantValue(attrib.ArgumentList.Arguments(1).GetExpression).ToString()
                        Dim hash = context.SemanticModel.GetConstantValue(attrib.ArgumentList.Arguments(2).GetExpression).ToString()
                        TemplateInfo.Add((name, template, hash))
                    End If
                End If
            End Sub
        End Class

    End Class

End Namespace
