// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.UnitTestFramework.Test
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpSyntaxTreeDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "TEST01";
        public static readonly LocalizableString Title = "Test title";
        public static readonly LocalizableString MessageFormat = "Semicolons should be {0} by a space";
        public static readonly string Category = "Test";
        public static readonly DiagnosticSeverity DefaultSeverity = DiagnosticSeverity.Warning;
        public static readonly bool IsEnabledByDefault = true;
        public static readonly LocalizableString Description = "Test description";
        public static readonly string HelpLinkUri = "https://github.com/dotnet/roslyn-sdk";
        public static readonly ImmutableArray<string> CustomTags = ImmutableArray.Create(WellKnownDiagnosticTags.Unnecessary);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DefaultSeverity, IsEnabledByDefault, Description, HelpLinkUri, CustomTags.ToArray());

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(Descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxTreeAction(HandleSyntaxTree);
        }

        private static void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            SyntaxNode root = context.Tree.GetCompilationUnitRoot(context.CancellationToken);
            foreach (SyntaxToken token in root.DescendantTokens())
            {
                switch (token.Kind())
                {
                case SyntaxKind.SemicolonToken:
                    HandleSemicolonToken(context, token);
                    break;

                default:
                    break;
                }
            }
        }

        private static void HandleSemicolonToken(SyntaxTreeAnalysisContext context, SyntaxToken token)
        {
            // check for a following space
            bool missingFollowingSpace = true;
            if (token.HasTrailingTrivia)
            {
                if (token.TrailingTrivia.First().IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    missingFollowingSpace = false;
                }
            }

            if (missingFollowingSpace)
            {
                // semicolon should{} be {followed} by a space
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, token.GetLocation(), TokenSpacingProperties.InsertFollowing, "followed"));
            }
        }

        internal static class TokenSpacingProperties
        {
            internal const string LocationKey = "location";
            internal const string ActionKey = "action";
            internal const string LocationFollowing = "following";
            internal const string ActionInsert = "insert";

            internal static ImmutableDictionary<string, string> InsertFollowing { get; } =
                ImmutableDictionary<string, string>.Empty
                    .SetItem(LocationKey, LocationFollowing)
                    .SetItem(ActionKey, ActionInsert);
        }
    }
}
