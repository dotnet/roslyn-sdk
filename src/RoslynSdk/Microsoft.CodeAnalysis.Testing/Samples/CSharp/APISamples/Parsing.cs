// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace APISamples
{
    public class Parsing
    {
        [Fact]
        public void TextParseTreeRoundtrip()
        {
            var text = "class C { void M() { } } // exact text round trip, including comments and whitespace";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            Assert.Equal(text, tree.ToString());
        }

        [Fact]
        public void DetermineValidIdentifierName()
        {
            ValidIdentifier("@class", true);
            ValidIdentifier("class", false);
        }

        private void ValidIdentifier(string identifier, bool expectedValid)
        {
            var token = SyntaxFactory.ParseToken(identifier);
            Assert.Equal(expectedValid,
                token.Kind() == SyntaxKind.IdentifierToken && token.Span.Length == identifier.Length);
        }

        [Fact]
        public void SyntaxFactsMethods()
        {
            Assert.Equal("protected internal", SyntaxFacts.GetText(Accessibility.ProtectedOrInternal));
            Assert.Equal("private protected", SyntaxFacts.GetText(Accessibility.ProtectedAndInternal));
            Assert.Equal("??", SyntaxFacts.GetText(SyntaxKind.QuestionQuestionToken));
            Assert.Equal("this", SyntaxFacts.GetText(SyntaxKind.ThisKeyword));

            Assert.Equal(SyntaxKind.CharacterLiteralExpression, SyntaxFacts.GetLiteralExpression(SyntaxKind.CharacterLiteralToken));
            Assert.Equal(SyntaxKind.CoalesceExpression, SyntaxFacts.GetBinaryExpression(SyntaxKind.QuestionQuestionToken));
            Assert.Equal(SyntaxKind.None, SyntaxFacts.GetBinaryExpression(SyntaxKind.UndefDirectiveTrivia));
            Assert.False(SyntaxFacts.IsPunctuation(SyntaxKind.StringLiteralToken));
        }

        [Fact]
        public void ParseTokens()
        {
            var tokens = SyntaxFactory.ParseTokens("class C { // trivia");
            var fullTexts = tokens.Select(token => token.ToFullString());

            Assert.True(fullTexts.SequenceEqual(new[]
            {
                "class ",
                "C ",
                "{ // trivia",
                "" // EOF
            }));
        }

        [Fact]
        public void ParseExpression()
        {
            var expression = SyntaxFactory.ParseExpression("1 + 2");
            if (expression.Kind() == SyntaxKind.AddExpression)
            {
                var binaryExpression = (BinaryExpressionSyntax)expression;
                var operatorToken = binaryExpression.OperatorToken;
                Assert.Equal("+", operatorToken.ToString());

                var left = binaryExpression.Left;
                Assert.Equal(SyntaxKind.NumericLiteralExpression, left.Kind());
            }
        }

        [Fact]
        public void IncrementalParse()
        {
            var oldText = SourceText.From("class C { }");
            var newText = oldText.WithChanges(new TextChange(new TextSpan(9, 0), "void M() { } "));

            var tree = SyntaxFactory.ParseSyntaxTree(oldText);

            var newTree = tree.WithChangedText(newText);

            Assert.Equal(newText.ToString(), newTree.ToString());
        }

        [Fact]
        public void PreprocessorDirectives()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"#if true
class A { }
#else
class B { }
#endif");
            var eof = tree.GetRoot().FindToken(tree.GetText().Length, false);
            Assert.True(eof.HasLeadingTrivia);
            Assert.False(eof.HasTrailingTrivia);
            Assert.True(eof.ContainsDirectives);

            var trivia = eof.LeadingTrivia;
            Assert.Equal(3, trivia.Count);
            Assert.Equal("#else", trivia.ElementAt(0).ToString());
            Assert.Equal(SyntaxKind.DisabledTextTrivia, trivia.ElementAt(1).Kind());
            Assert.Equal("#endif", trivia.ElementAt(2).ToString());

            var directive = tree.GetRoot().GetLastDirective();
            Assert.Equal("endif", directive.DirectiveNameToken.Value);

            directive = directive.GetPreviousDirective();
            Assert.Equal("else", directive.DirectiveNameToken.Value);
        }
    }
}
