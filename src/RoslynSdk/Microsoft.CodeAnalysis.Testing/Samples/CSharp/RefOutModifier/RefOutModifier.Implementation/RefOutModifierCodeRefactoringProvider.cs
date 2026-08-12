using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Samples.AddOrRemoveRefOutModifier
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(RefOutModifierCodeRefactoringProvider)), Shared]
    internal class RefOutModifierCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            // shouldn't have selection
            if (!textSpan.IsEmpty)
            {
                return;
            }

            // get applicable actions
            var finder = new ApplicableActionFinder(document, textSpan.Start, cancellationToken);
            (var span, var action) = await finder.GetSpanAndActionAsync().ConfigureAwait(false);
            if (action == null || !span.IntersectsWith(textSpan.Start))
            {
                return;
            }

            context.RegisterRefactoring(action);
        }
    }
}
