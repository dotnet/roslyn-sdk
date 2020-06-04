using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace $saferootprojectname$
{
    [ExportCodeFixProvider(LanguageNames.VisualBasic, Name = nameof(VisualBasic$saferootidentifiername$CodeFixProvider)), Shared]
    public class VisualBasic$saferootidentifiername$CodeFixProvider : Abstract$saferootidentifiername$CodeFixProvider<TypeStatementSyntax>
    {
        protected override async Task<Solution> MakeUppercaseAsync(Document document, TypeStatementSyntax typeDeclaration, CancellationToken cancellationToken)
        {
            // Compute new uppercase name.
            var identifierToken = typeDeclaration.Identifier;
            var newName = identifierToken.Text.ToUpperInvariant();

            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

            // Return the new solution with the now-uppercase type name.
            return newSolution;
        }
    }
}
