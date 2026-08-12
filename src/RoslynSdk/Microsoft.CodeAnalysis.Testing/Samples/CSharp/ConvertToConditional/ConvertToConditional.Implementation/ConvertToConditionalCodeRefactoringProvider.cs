// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace ConvertToConditional
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ConvertToConditionalCodeRefactoringProvider)), Shared]
    public class ConvertToConditionalCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(textSpan.Start);

            // Only trigger if the text span is within the 'if' keyword token of an if-else statement.

            if (token.Kind() != SyntaxKind.IfKeyword ||
                !token.Span.IntersectsWith(textSpan.Start) ||
                !token.Span.IntersectsWith(textSpan.End))
            {
                return;
            }

            if (!(token.Parent is IfStatementSyntax ifStatement) || ifStatement.Else == null)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (ReturnConditionalAnalyzer.TryGetNewReturnStatement(ifStatement, semanticModel, out var returnStatement))
            {
                var action =
                    new ConvertToConditionalCodeAction("Convert to conditional expression",
                                                       (c) => Task.FromResult(ConvertToConditional(document, semanticModel, ifStatement, returnStatement, c)));
                context.RegisterRefactoring(action);
            }
        }

        private Document ConvertToConditional(Document document,
                                              SemanticModel semanticModel,
                                              IfStatementSyntax ifStatement,
                                              StatementSyntax replacementStatement,
                                              CancellationToken cancellationToken)
        {
            var oldRoot = semanticModel.SyntaxTree.GetRoot();
            var newRoot = oldRoot.ReplaceNode(
                oldNode: ifStatement,
                newNode: replacementStatement.WithAdditionalAnnotations(Formatter.Annotation));

            return document.WithSyntaxRoot(newRoot);
        }

        private class ConvertToConditionalCodeAction : CodeAction
        {
            private readonly string title;
            private readonly Func<CancellationToken, Task<Document>> createChangedDocument;

            public ConvertToConditionalCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
            {
                this.title = title;
                this.createChangedDocument = createChangedDocument;
            }

            public override string Title { get { return title; } }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return createChangedDocument(cancellationToken);
            }
        }
    }
}
