Option Explicit On
Option Infer On
Option Strict On

Imports System.Collections.Immutable
Imports System.Text

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace SourceGeneratorSamples

  <Generator(LanguageNames.VisualBasic)>
  Public Class MustacheGenerator
    Implements ISourceGenerator

    Public Sub Initialize(context As GeneratorInitializationContext) Implements ISourceGenerator.Initialize
      ' No initialization required
    End Sub

    Public Sub Execute(context As GeneratorExecutionContext) Implements ISourceGenerator.Execute

      Dim attributeSource = "Option Explicit On
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

      context.AddSource("Mustache_MainAttributes__", SourceText.From(attributeSource, Encoding.UTF8))

      Dim compilation = context.Compilation

      Dim options = GetMustacheOptions(compilation)
      Dim namesSources = SourceFilesFromMustachePaths(options)

      For Each entry In namesSources
        context.AddSource($"Mustache{entry.Item1}", SourceText.From(entry.Item2, Encoding.UTF8))
      Next

    End Sub

    Private Shared Iterator Function GetMustacheOptions(compilation As Compilation) As IEnumerable(Of (String, String, String))

      ' Get all Mustache attributes

      Dim allNodes = compilation.SyntaxTrees.SelectMany(Function(s) s.GetRoot().DescendantNodes())


      Dim allAttributes = allNodes.Where(Function(d) d.IsKind(SyntaxKind.Attribute)).OfType(Of AttributeSyntax)()
      Dim attributes = allAttributes.Where(Function(d) d.Name.ToString() = "Mustache").ToImmutableArray()

      Dim models = compilation.SyntaxTrees.[Select](Function(st) compilation.GetSemanticModel(st))
      For Each att In attributes

        Dim mustacheName = ""
        Dim template = ""
        Dim hash = ""
        Dim index = 0

        If att.ArgumentList Is Nothing Then
          Throw New Exception("Can't be null here")
        End If

        Dim m = compilation.GetSemanticModel(att.SyntaxTree)

        For Each arg In att.ArgumentList.Arguments

          Dim expr As ExpressionSyntax = Nothing
          If TypeOf arg Is SimpleArgumentSyntax Then
            expr = TryCast(arg, SimpleArgumentSyntax).Expression
          End If
          If expr Is Nothing Then
            Continue For
          End If

          Dim t = m.GetTypeInfo(expr)
          Dim v = m.GetConstantValue(expr)
          If index = 0 Then
            mustacheName = v.ToString()
          ElseIf index = 1 Then
            template = v.ToString()
          Else
            hash = v.ToString()
          End If
          index += 1

        Next

        Yield (mustacheName, template, hash)

      Next

    End Function

    Private Shared Function SourceFileFromMustachePath(name As String, template As String, hash As String) As String
      Dim tree = HandlebarsDotNet.Handlebars.Compile(template)
      Dim o = Newtonsoft.Json.JsonConvert.DeserializeObject(hash)
      Dim mustacheText = tree(o)
      Return GenerateMustacheClass(name, mustacheText)
    End Function

    Private Shared Iterator Function SourceFilesFromMustachePaths(pathsData As IEnumerable(Of (Name As String, Template As String, Hash As String))) As IEnumerable(Of (Name As String, Code As String))
      For Each entry In pathsData
        Yield (entry.Name, SourceFileFromMustachePath(entry.Name, entry.Template, entry.Hash))
      Next
    End Function

    Private Shared Function GenerateMustacheClass(className As String, mustacheText As String) As String
      Return $"

Namespace Global.Mustache

    Partial Public Module Constants

        Public Const {className} As String = ""{mustacheText.Replace("""", """""")}""

    End Module

End Namespace"
    End Function

  End Class

End Namespace