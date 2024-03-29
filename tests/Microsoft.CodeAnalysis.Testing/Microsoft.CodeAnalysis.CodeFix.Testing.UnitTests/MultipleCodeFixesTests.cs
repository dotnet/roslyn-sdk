﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.TestFixes;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Code fix tests for scenarios where multiple fixes are available.
    /// </summary>
    public class MultipleCodeFixesTests
    {
        [Fact]
        [WorkItem(171, "https://github.com/dotnet/roslyn-sdk/issues/171")]
        public async Task TestDefaultSelection()
        {
            var testCode =
                """

                class TestClass {
                  int field = [|0|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int field = 1;
                }

                """;

            // A single CodeFixProvider provides three actions
            var codeFixes = ImmutableArray.Create(ImmutableArray.Create(1, 2, 3));
            await new CSharpTest(codeFixes)
            {
                TestCode = testCode,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestDefaultSelectionMultipleFixers()
        {
            var testCode =
                """

                class TestClass {
                  int field = [|0|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int field = 1;
                }

                """;

            // Three CodeFixProviders provide three actions
            var codeFixes = ImmutableArray.Create(
                ImmutableArray.Create(1),
                ImmutableArray.Create(2),
                ImmutableArray.Create(3));
            await new CSharpTest(codeFixes)
            {
                TestCode = testCode,
                FixedCode = fixedCode,
            }.RunAsync();
        }

#if !NETCOREAPP1_1 && !NET46
        [Fact]
        public async Task TestDefaultSelectionNestedFixers()
        {
            var testCode =
                """

                class TestClass {
                  int field = [|0|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int field = 1;
                }

                """;

            // Three CodeFixProviders provide three actions
            var codeFixes = ImmutableArray.Create(
                ImmutableArray.Create(1),
                ImmutableArray.Create(2),
                ImmutableArray.Create(3));
            await new CSharpTest(codeFixes, nested: true)
            {
                TestCode = testCode,
                FixedCode = fixedCode,
            }.RunAsync();
        }
#endif

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [WorkItem(171, "https://github.com/dotnet/roslyn-sdk/issues/171")]
        public async Task TestSelectionByIndex(int index)
        {
            var testCode =
                """

                class TestClass {
                  int field = [|0|];
                }

                """;
            var fixedCode =
                $$"""

                class TestClass {
                  int field = {{index + 1}};
                }

                """;

            // A single CodeFixProvider provides three actions
            var codeFixes = ImmutableArray.Create(ImmutableArray.Create(1, 2, 3));
            await new CSharpTest(codeFixes)
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                CodeActionIndex = index,
            }.RunAsync();
        }

        [Theory]
        [InlineData(0, "ReplaceZeroFix_1")]
        [InlineData(1, "ReplaceZeroFix_2")]
        [InlineData(2, "ReplaceZeroFix_3")]
        [WorkItem(171, "https://github.com/dotnet/roslyn-sdk/issues/171")]
        public async Task TestSelectionByEquivalenceKey(int index, string equivalenceKey)
        {
            var testCode =
                """

                class TestClass {
                  int field = [|0|];
                }

                """;
            var fixedCode =
                $$"""

                class TestClass {
                  int field = {{index + 1}};
                }

                """;

            // A single CodeFixProvider provides three actions
            var codeFixes = ImmutableArray.Create(ImmutableArray.Create(1, 2, 3));
            await new CSharpTest(codeFixes)
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                CodeActionEquivalenceKey = equivalenceKey,
            }.RunAsync();
        }

        [Theory]
        [InlineData(0, "ReplaceZeroFix_1")]
        [InlineData(1, "ReplaceZeroFix_2")]
        [InlineData(2, "ReplaceZeroFix_3")]
        [WorkItem(171, "https://github.com/dotnet/roslyn-sdk/issues/171")]
        public async Task TestIndexAndEquivalenceKeyMatch(int index, string equivalenceKey)
        {
            var testCode =
                """

                class TestClass {
                  int field = [|0|];
                }

                """;
            var fixedCode =
                $$"""

                class TestClass {
                  int field = {{index + 1}};
                }

                """;

            // A single CodeFixProvider provides three actions
            var codeFixes = ImmutableArray.Create(ImmutableArray.Create(1, 2, 3));
            await new CSharpTest(codeFixes)
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                CodeActionIndex = index,
                CodeActionEquivalenceKey = equivalenceKey,
            }.RunAsync();
        }

        [Theory]
        [InlineData(0, "ReplaceZeroFix_1")]
        [InlineData(1, "ReplaceZeroFix_2")]
        [InlineData(2, "ReplaceZeroFix_3")]
        public async Task TestIndexAndEquivalenceKeyMatchMultipleFixers(int index, string equivalenceKey)
        {
            var testCode =
                """

                class TestClass {
                  int field = [|0|];
                }

                """;
            var fixedCode =
                $$"""

                class TestClass {
                  int field = {{index + 1}};
                }

                """;

            // Three CodeFixProviders provide three actions
            var codeFixes = ImmutableArray.Create(
                ImmutableArray.Create(1),
                ImmutableArray.Create(2),
                ImmutableArray.Create(3));
            await new CSharpTest(codeFixes)
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                CodeActionIndex = index,
                CodeActionEquivalenceKey = equivalenceKey,
            }.RunAsync();
        }

        [Fact]
        [WorkItem(171, "https://github.com/dotnet/roslyn-sdk/issues/171")]
        public async Task TestIndexAndEquivalenceKeyMismatch()
        {
            var testCode =
                """

                class TestClass {
                  int field = [|0|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int field = 2;
                }

                """;

            // A single CodeFixProvider provides three actions
            var codeFixes = ImmutableArray.Create(ImmutableArray.Create(1, 2, 3));
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest(codeFixes)
                {
                    TestCode = testCode,
                    FixedCode = fixedCode,
                    CodeActionIndex = 1,
                    CodeActionEquivalenceKey = "ReplaceZeroFix_1",
                }.RunAsync();
            });

            Assert.Equal(
                """
                Context: Iterative code fix application
                The code action equivalence key and index must be consistent when both are specified.
                """.ReplaceLineEndings(),
                exception.Message);
        }

        [Fact]
        public async Task TestAdditionalVerificationSuccess()
        {
            var testCode =
                """

                class TestClass {
                  int field = [|0|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int field = 2;
                }

                """;

            // A single CodeFixProvider provides three actions
            var codeFixes = ImmutableArray.Create(ImmutableArray.Create(1, 2, 3));
            await new CSharpTest(codeFixes)
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = "ReplaceZeroFix_2",
                CodeActionVerifier = (codeAction, verifier) => verifier.Equal("ThisToBase", codeAction.Title),
            }.RunAsync();
        }

        [Fact]
        public async Task TestAdditionalVerificationFailure()
        {
            var testCode =
                """

                class TestClass {
                  int field = [|0|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int field = 2;
                }

                """;

            // A single CodeFixProvider provides three actions
            var codeFixes = ImmutableArray.Create(ImmutableArray.Create(1, 2, 3));
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest(codeFixes)
                {
                    TestCode = testCode,
                    FixedCode = fixedCode,
                    CodeActionIndex = 1,
                    CodeActionEquivalenceKey = "ReplaceZeroFix_2",
                    CodeActionVerifier = (codeAction, verifier) => verifier.Equal("Expected title", codeAction.Title),
                }.RunAsync();
            });

            Assert.Equal(
                """
                Context: Iterative code fix application
                items not equal.  expected:'Expected title' actual:'ThisToBase'
                """.ReplaceLineEndings(),
                exception.Message);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class LiteralZeroAnalyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor Descriptor =
                new("LiteralZero", "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

                context.RegisterSyntaxNodeAction(HandleNumericLiteralExpression, SyntaxKind.NumericLiteralExpression);
            }

            private void HandleNumericLiteralExpression(SyntaxNodeAnalysisContext context)
            {
                var node = (LiteralExpressionSyntax)context.Node;
                if (node.Token.ValueText == "0")
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, node.Token.GetLocation()));
                }
            }
        }

        [ExportCodeFixProvider(LanguageNames.CSharp)]
        [PartNotDiscoverable]
        private class ReplaceZeroFix : CodeFixProvider
        {
            private readonly ImmutableArray<int> _replacements;
            private readonly bool _nested;

            public ReplaceZeroFix(ImmutableArray<int> replacements, bool nested)
            {
                Debug.Assert(replacements.All(replacement => replacement >= 0), $"Assertion failed: {nameof(replacements)}.All(replacement => replacement >= 0)");
                _replacements = replacements;
                _nested = nested;
            }

            public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(LiteralZeroAnalyzer.Descriptor.Id);

            public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

            public override Task RegisterCodeFixesAsync(CodeFixContext context)
            {
                foreach (var diagnostic in context.Diagnostics)
                {
                    var fixes = new List<CodeAction>();
                    foreach (var replacement in _replacements)
                    {
                        fixes.Add(CodeAction.Create(
                            "ThisToBase",
                            cancellationToken => CreateChangedDocument(context.Document, diagnostic.Location.SourceSpan, replacement, cancellationToken),
                            $"{nameof(ReplaceZeroFix)}_{replacement}"));
                    }

                    if (_nested)
                    {
#if NETCOREAPP2_0 || NETCOREAPP3_1 || NET472
#pragma warning disable RS1010 // Create code actions should have a unique EquivalenceKey for FixAll occurrences support. (https://github.com/dotnet/roslyn-analyzers/issues/3475)
                        fixes = new List<CodeAction> { CodeAction.Create("Container", fixes.ToImmutableArray(), isInlinable: false) };
#pragma warning restore RS1010 // Create code actions should have a unique EquivalenceKey for FixAll occurrences support.
#else
                        throw new NotSupportedException("Nested code actions are not supported on this framework.");
#endif
                    }

                    foreach (var fix in fixes)
                    {
                        context.RegisterCodeFix(fix, diagnostic);
                    }
                }

                return Task.CompletedTask;
            }

            private async Task<Document> CreateChangedDocument(Document document, TextSpan sourceSpan, int replacement, CancellationToken cancellationToken)
            {
                var tree = (await document.GetSyntaxTreeAsync(cancellationToken))!;
                var root = await tree.GetRootAsync(cancellationToken);
                var token = root.FindToken(sourceSpan.Start);
                var newToken = SyntaxFactory.Literal(token.LeadingTrivia, replacement.ToString(), replacement, token.TrailingTrivia);
                return document.WithSyntaxRoot(root.ReplaceToken(token, newToken));
            }
        }

        private class CSharpTest : CSharpCodeFixTest<LiteralZeroAnalyzer, EmptyCodeFixProvider>
        {
            private readonly ImmutableArray<ImmutableArray<int>> _replacementGroups;
            private readonly bool _nested;

            public CSharpTest(ImmutableArray<ImmutableArray<int>> replacementGroups, bool nested = false)
            {
                _replacementGroups = replacementGroups;
                _nested = nested;
            }

            protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
            {
                foreach (var replacementGroup in _replacementGroups)
                {
                    yield return new ReplaceZeroFix(replacementGroup, _nested);
                }
            }
        }
    }
}
