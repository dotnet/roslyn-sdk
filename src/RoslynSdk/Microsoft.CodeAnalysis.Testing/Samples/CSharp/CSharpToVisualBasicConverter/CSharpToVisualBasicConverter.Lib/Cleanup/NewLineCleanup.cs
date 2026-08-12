// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpToVisualBasicConverter.Cleanup
{
    internal class NewLineCleanup : CSharpSyntaxRewriter
    {
        private readonly SyntaxTree syntaxTree;

        public NewLineCleanup(SyntaxTree syntaxTree)
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

            bool changed;

            do
            {
                changed = false;
                if ((token.HasLeadingTrivia && token.LeadingTrivia.Count >= 2) ||
                    (token.HasTrailingTrivia && token.TrailingTrivia.Count >= 2))
                {
                    var newLeadingTrivia = RemoveBlankLines(token.LeadingTrivia, ref changed);
                    var newTrailingTrivia = RemoveBlankLines(token.TrailingTrivia, ref changed);

                    if (changed)
                    {
                        token = token.WithLeadingTrivia(SyntaxFactory.TriviaList(newLeadingTrivia));
                        token = token.WithTrailingTrivia(SyntaxFactory.TriviaList(newTrailingTrivia));
                    }
                }
            }
            while (changed);

            return token;
        }

        private static List<SyntaxTrivia> RemoveBlankLines(SyntaxTriviaList trivia, ref bool changed)
        {
            var newTrivia = new List<SyntaxTrivia>();

            for (var i = 0; i < trivia.Count;)
            {
                var trivia1 = trivia.ElementAt(i);
                newTrivia.Add(trivia1);

                if (i < trivia.Count - 1)
                {
                    var trivia2 = trivia.ElementAt(i + 1);

                    if (trivia1.IsKind(SyntaxKind.EndOfLineTrivia) &&
                        trivia2.IsKind(SyntaxKind.EndOfLineTrivia))
                    {
                        changed = true;
                        i += 2;
                        continue;
                    }
                }

                i++;
            }

            return newTrivia;
        }
    }
}
