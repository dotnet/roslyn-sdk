// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpToVisualBasicConverter.Cleanup
{
    internal class CurlyCleanup : CSharpSyntaxRewriter
    {
        private readonly SyntaxTree syntaxTree;

        public CurlyCleanup(SyntaxTree syntaxTree)
        {
            this.syntaxTree = syntaxTree;
        }

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            token = base.VisitToken(token);
            if (token.IsMissing)
            {
                return token;
            }

            if (!token.IsKind(SyntaxKind.CloseBraceToken))
            {
                return token;
            }

            var nextToken = token.GetNextToken(includeSkipped: true);

            var tokenLine = syntaxTree.GetText().Lines.IndexOf(token.Span.Start);
            var nextTokenLine = syntaxTree.GetText().Lines.IndexOf(nextToken.Span.Start);
            var nextTokenIsCloseBrace = nextToken.IsKind(SyntaxKind.CloseBraceToken);

            var expectedDiff = nextTokenIsCloseBrace ? 1 : 2;
            if (nextTokenLine == tokenLine + expectedDiff)
            {
                return token;
            }

            var nonNewLineTrivia = token.TrailingTrivia.Where(t => !t.IsKind(SyntaxKind.EndOfLineTrivia));
            var newTrivia = nonNewLineTrivia.Concat(Enumerable.Repeat(SyntaxFactory.EndOfLine("\r\n"), expectedDiff));

            return token.WithTrailingTrivia(SyntaxFactory.TriviaList(newTrivia));
        }
    }
}
