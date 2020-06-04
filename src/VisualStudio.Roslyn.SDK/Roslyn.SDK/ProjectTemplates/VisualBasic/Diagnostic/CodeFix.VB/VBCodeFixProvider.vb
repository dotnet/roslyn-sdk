Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasic$saferootidentifiername$CodeFixProvider)), [Shared]>
Public Class VisualBasic$saferootidentifiername$CodeFixProvider
    Inherits Abstract$saferootidentifiername$CodeFixProvider(Of TypeStatementSyntax)

    Protected Overrides Async Function MakeUppercaseAsync(document As Document, typeDeclaration As TypeStatementSyntax, cancellationToken As CancellationToken) As Task(Of Solution)
        ' Compute new uppercase name.
        Dim identifierToken = typeDeclaration.Identifier
        Dim newName = identifierToken.Text.ToUpperInvariant()

        ' Get the symbol representing the type to be renamed.
        Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken)
        Dim typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken)

        ' Produce a new solution that has all references to that type renamed, including the declaration.
        Dim originalSolution = document.Project.Solution
        Dim optionSet = originalSolution.Workspace.Options
        Dim newSolution = Await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(False)

        ' Return the new solution with the now-uppercase type name.
        Return newSolution
    End Function
End Class
