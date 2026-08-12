using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using RefOutModifier.Properties;

namespace Roslyn.Samples.AddOrRemoveRefOutModifier
{
    internal class RemoveOutOrRefCodeAction : CodeAction
    {
        private readonly Document document;
        private readonly SemanticModel semanticModel;
        private readonly ArgumentSyntax argument;
        private readonly IEnumerable<ParameterSyntax> parameters;

        public static bool Applicable(SemanticModel semanticModel, ArgumentSyntax argument, IEnumerable<ParameterSyntax> parameters)
        {
            var method = argument.AncestorAndSelf<BaseMethodDeclarationSyntax>();
            if (method == null ||
                method.Body == null)
            {
                return false;
            }

            if (argument.RefOrOutKeyword.Kind() == SyntaxKind.RefKeyword)
            {
                return true;
            }

            Debug.Assert(argument.RefOrOutKeyword.Kind() == SyntaxKind.OutKeyword);

            var symbolInfo = semanticModel.GetSymbolInfo(argument.Expression);
            if (!(symbolInfo.Symbol != null && symbolInfo.Symbol.Kind == SymbolKind.Local))
            {
                return true;
            }

            // for local, make sure it is definitely assigned before removing "out" keyword
            var invocation = argument.AncestorAndSelf<InvocationExpressionSyntax>();
            if (invocation == null)
            {
                return false;
            }

            var range = GetStatementRangeForFlowAnalysis<StatementSyntax>(method.Body, TextSpan.FromBounds(method.Body.OpenBraceToken.Span.End, invocation.Span.Start));
            var dataFlow = semanticModel.AnalyzeDataFlow(range.Item1, range.Item2);
            foreach (var symbol in dataFlow.AlwaysAssigned)
            {
                if (symbolInfo.Symbol == symbol)
                {
                    return true;
                }
            }

            return false;
        }

        private static Tuple<T, T> GetStatementRangeForFlowAnalysis<T>(SyntaxNode node, TextSpan textSpan) where T : SyntaxNode
        {
            T firstStatement = null;
            T lastStatement = null;

            foreach (var stmt in node.DescendantNodesAndSelf().OfType<T>())
            {
                if (firstStatement == null && stmt.Span.Start >= textSpan.Start)
                {
                    firstStatement = stmt;
                }

                if (firstStatement != null && stmt.Span.End <= textSpan.End && stmt.Parent == firstStatement.Parent)
                {
                    lastStatement = stmt;
                }
            }

            if (firstStatement == null || lastStatement == null)
            {
                return null;
            }

            return new Tuple<T, T>(firstStatement, lastStatement);
        }

        public RemoveOutOrRefCodeAction(
            Document document,
            SemanticModel semanticModel,
            ArgumentSyntax argument,
            IEnumerable<ParameterSyntax> parameters)
        {
            this.document = document;
            this.semanticModel = semanticModel;
            this.argument = argument;
            this.parameters = parameters;
        }

        public override string Title
        {
            get { return Resources.RemoveOutOrRefTitle; }
        }

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            var map = new Dictionary<SyntaxToken, SyntaxToken>
            {
                { argument.RefOrOutKeyword, default }
            };

            var tokenBeforeArgumentModifier = argument.RefOrOutKeyword.GetPreviousToken(includeSkipped: true);
            map.Add(tokenBeforeArgumentModifier,
                    tokenBeforeArgumentModifier.MergeTrailingTrivia(argument.RefOrOutKeyword)
                                               .WithAdditionalAnnotations(Formatter.Annotation));

            foreach (var parameter in parameters)
            {
                var outOrRefModifier = parameter.Modifiers.FirstOrDefault(t => t.Kind() == SyntaxKind.OutKeyword || t.Kind() == SyntaxKind.RefKeyword);
                if (outOrRefModifier.Kind() == SyntaxKind.None)
                {
                    continue;
                }

                map.Add(outOrRefModifier, default);

                var tokenBeforeParameterModifier = outOrRefModifier.GetPreviousToken(includeSkipped: true);
                map.Add(tokenBeforeParameterModifier, tokenBeforeParameterModifier.MergeTrailingTrivia(outOrRefModifier)
                                                                                  .WithAdditionalAnnotations(Formatter.Annotation));
            }

            var root = (SyntaxNode)await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceTokens(map.Keys, (o, n) => map[o]);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
