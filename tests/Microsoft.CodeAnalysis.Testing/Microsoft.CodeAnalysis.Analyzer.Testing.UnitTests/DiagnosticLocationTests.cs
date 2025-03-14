﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.TestAnalyzers;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class DiagnosticLocationTests
    {
        private static DiagnosticResult Diagnostic<TAnalyzer>()
            where TAnalyzer : DiagnosticAnalyzer, new()
            => AnalyzerVerifier<TAnalyzer, CSharpAnalyzerTest<TAnalyzer>, DefaultVerifier>.Diagnostic();

        [Fact]
        public async Task TestDiagnosticMatchesCorrectSpan()
        {
            await new CSharpAnalyzerTest<HighlightBraceSpanAnalyzer>
            {
                TestCode = @"class TestClass [|{|] }",
            }.RunAsync();
        }

        [Fact]
        public async Task TestDiagnosticMatchesCorrectLocation()
        {
            await new CSharpAnalyzerTest<HighlightBraceSpanAnalyzer>
            {
                TestCode = @"class TestClass $${ }",
            }.RunAsync();
        }

        [Fact]
        public async Task TestDiagnosticWithoutLocation()
        {
            await new CSharpAnalyzerTest<ReportCompilationDiagnosticAnalyzer>
            {
                TestCode = @"class TestClass { }",
                ExpectedDiagnostics = { new DiagnosticResult(ReportCompilationDiagnosticAnalyzer.Descriptor) },
            }.RunAsync();
        }

        [Fact]
        public async Task TestDiagnosticExplicitWithoutLocation()
        {
            await new CSharpAnalyzerTest<ReportCompilationDiagnosticAnalyzer>
            {
                TestCode = @"class TestClass { }",
                ExpectedDiagnostics = { new DiagnosticResult(ReportCompilationDiagnosticAnalyzer.Descriptor).WithNoLocation() },
            }.RunAsync();
        }

        [Fact]
        [WorkItem(207, "https://github.com/dotnet/roslyn-sdk/issues/207")]
        public async Task TestDiagnosticDoesNotMatchIncorrectSpan()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<HighlightBraceSpanAnalyzer>
                {
                    TestCode = @"class TestClass [||]{ }",
                }.RunAsync();
            });

            var expected =
                """
                Expected diagnostic to end at column "17" was actually at column "18"

                Expected diagnostic:
                    // /0/Test0.cs(1,17,1,17): warning Brace
                VerifyCS.Diagnostic().WithSpan(1, 17, 1, 17),

                Actual diagnostic:
                    // /0/Test0.cs(1,17): warning Brace: message
                VerifyCS.Diagnostic().WithSpan(1, 17, 1, 18),


                """.ReplaceLineEndings();
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestDiagnosticDoesNotMatchIncorrectLocation()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<HighlightBraceSpanAnalyzer>
                {
                    TestCode = @"class TestClass {$$ }",
                }.RunAsync();
            });

            var expected =
                """
                Expected diagnostic to start at column "18" was actually at column "17"

                Expected diagnostic:
                    // /0/Test0.cs(1,18): warning Brace
                VerifyCS.Diagnostic().WithLocation(1, 18),

                Actual diagnostic:
                    // /0/Test0.cs(1,17): warning Brace: message
                VerifyCS.Diagnostic().WithSpan(1, 17, 1, 18),


                """.ReplaceLineEndings();
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestDiagnosticDoesNotMatchMissingLocation()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<HighlightBraceSpanAnalyzer>
                {
                    TestCode = @"class TestClass { }",
                    ExpectedDiagnostics = { Diagnostic<HighlightBraceSpanAnalyzer>() },
                }.RunAsync();
            });

            var expected =
                """
                Expected a project diagnostic with no location:

                Expected diagnostic:
                    // warning Brace: message
                new DiagnosticResult(HighlightBraceSpanAnalyzer.Brace),

                Actual diagnostic:
                    // /0/Test0.cs(1,17): warning Brace: message
                VerifyCS.Diagnostic().WithSpan(1, 17, 1, 18),


                """.ReplaceLineEndings();
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestDiagnosticDoesNotMatchNoLocation()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<HighlightBraceSpanAnalyzer>
                {
                    TestCode = @"class TestClass { }",
                    ExpectedDiagnostics = { Diagnostic<HighlightBraceSpanAnalyzer>().WithNoLocation() },
                }.RunAsync();
            });

            var expected =
                """
                Expected a project diagnostic with no location:

                Expected diagnostic:
                    // warning Brace: message
                new DiagnosticResult(HighlightBraceSpanAnalyzer.Brace),

                Actual diagnostic:
                    // /0/Test0.cs(1,17): warning Brace: message
                VerifyCS.Diagnostic().WithSpan(1, 17, 1, 18),


                """.ReplaceLineEndings();
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestZeroWidthDiagnosticMatchesCorrectSpan()
        {
            await new CSharpAnalyzerTest<HighlightBracePositionAnalyzer>
            {
                TestCode = @"class TestClass [||]{ }",
            }.RunAsync();
        }

        [Fact]
        public async Task TestZeroWidthDiagnosticMatchesCorrectLocation()
        {
            await new CSharpAnalyzerTest<HighlightBracePositionAnalyzer>
            {
                TestCode = @"class TestClass $${ }",
            }.RunAsync();
        }

        [Fact]
        public async Task TestZeroWidthDiagnosticDoesNotMatchIncorrectSpan()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<HighlightBracePositionAnalyzer>
                {
                    TestCode = @"class TestClass [|{|] }",
                }.RunAsync();
            });

            var expected =
                """
                Expected diagnostic to end at column "18" was actually at column "17"

                Expected diagnostic:
                    // /0/Test0.cs(1,17,1,18): warning Brace
                VerifyCS.Diagnostic().WithSpan(1, 17, 1, 18),

                Actual diagnostic:
                    // /0/Test0.cs(1,17): warning Brace: message
                VerifyCS.Diagnostic().WithSpan(1, 17, 1, 17),


                """.ReplaceLineEndings();
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestZeroWidthDiagnosticDoesNotMatchIncorrectLocation()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<HighlightBracePositionAnalyzer>
                {
                    TestCode = @"class TestClass {$$ }",
                }.RunAsync();
            });

            var expected =
                """
                Expected diagnostic to start at column "18" was actually at column "17"

                Expected diagnostic:
                    // /0/Test0.cs(1,18): warning Brace
                VerifyCS.Diagnostic().WithLocation(1, 18),

                Actual diagnostic:
                    // /0/Test0.cs(1,17): warning Brace: message
                VerifyCS.Diagnostic().WithSpan(1, 17, 1, 17),


                """.ReplaceLineEndings();
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestAdditionalUnnecessaryLocations()
        {
            await new CSharpAnalyzerTest<HighlightBraceSpanWithEndMarkedUnnecessaryAnalyzer>
            {
                TestCode = @"class TestClass {|#0:{|} {|#1:}|}",
                ExpectedDiagnostics =
                {
                    Diagnostic<HighlightBraceSpanWithEndMarkedUnnecessaryAnalyzer>().WithLocation(0).WithLocation(1, DiagnosticLocationOptions.UnnecessaryCode),
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestAdditionalUnnecessaryLocationsIgnored()
        {
            await new CSharpAnalyzerTest<HighlightBraceSpanWithEndMarkedUnnecessaryAnalyzer>
            {
                TestCode = @"class TestClass {|#0:{|} {|#1:}|}",
                ExpectedDiagnostics =
                {
                    Diagnostic<HighlightBraceSpanWithEndMarkedUnnecessaryAnalyzer>().WithLocation(0).WithOptions(DiagnosticOptions.IgnoreAdditionalLocations),
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestAdditionalUnnecessaryLocationRequiresExpectedMarkedUnnecessary()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<HighlightBraceSpanWithEndMarkedUnnecessaryAnalyzer>
                {
                    TestCode = @"class TestClass {|#0:{|} {|#1:}|}",
                    ExpectedDiagnostics =
                    {
                        Diagnostic<HighlightBraceSpanWithEndMarkedUnnecessaryAnalyzer>().WithLocation(0).WithLocation(1),
                    },
                }.RunAsync();
            });

            var expected =
                """
                Expected diagnostic additional location index "0" to not be marked unnecessary, but was instead marked unnecessary.
                """.ReplaceLineEndings();
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestAdditionalUnnecessaryLocationRequiresDescriptorMarkedUnnecessary()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<HighlightBraceSpanWithEndMarkedUnnecessaryButDescriptorNotAnalyzer>
                {
                    TestCode = @"class TestClass {|#0:{|} {|#1:}|}",
                    ExpectedDiagnostics =
                    {
                        Diagnostic<HighlightBraceSpanWithEndMarkedUnnecessaryButDescriptorNotAnalyzer>().WithLocation(0).WithLocation(1, DiagnosticLocationOptions.UnnecessaryCode),
                    },
                }.RunAsync();
            });

            var expected =
                """
                Diagnostic reported extended unnecessary locations, but the descriptor is not marked as unnecessary code.
                """.ReplaceLineEndings();
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestAdditionalUnnecessaryLocationNotJsonArray()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<HighlightBraceSpanWithEndMarkedUnnecessaryNotJsonArrayAnalyzer>
                {
                    TestCode = @"class TestClass {|#0:{|} {|#1:}|}",
                    ExpectedDiagnostics =
                    {
                        Diagnostic<HighlightBraceSpanWithEndMarkedUnnecessaryNotJsonArrayAnalyzer>().WithLocation(0).WithLocation(1, DiagnosticLocationOptions.UnnecessaryCode),
                    },
                }.RunAsync();
            });

            var expected =
                """
                Expected encoded unnecessary locations to be a valid JSON array of non-negative integers: Text
                """.ReplaceLineEndings();
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestAdditionalUnnecessaryLocationIndexOutOfRange()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<HighlightBraceSpanWithEndMarkedUnnecessaryIndexOutOfRangeAnalyzer>
                {
                    TestCode = @"class TestClass {|#0:{|} {|#1:}|}",
                    ExpectedDiagnostics =
                    {
                        Diagnostic<HighlightBraceSpanWithEndMarkedUnnecessaryIndexOutOfRangeAnalyzer>().WithLocation(0).WithLocation(1, DiagnosticLocationOptions.UnnecessaryCode),
                    },
                }.RunAsync();
            });

            var expected =
                """
                All unnecessary indices in the diagnostic must be valid indices in AdditionalLocations [0-1): [1]
                """.ReplaceLineEndings();
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class HighlightBracePositionAnalyzer : AbstractHighlightBracesAnalyzer
        {
            protected override Diagnostic CreateDiagnostic(SyntaxToken token)
            {
                var location = token.GetLocation();
                return CodeAnalysis.Diagnostic.Create(Descriptor, Location.Create(location.SourceTree!, new TextSpan(location.SourceSpan.Start, 0)));
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class HighlightBraceSpanAnalyzer : AbstractHighlightBracesAnalyzer
        {
            protected override Diagnostic CreateDiagnostic(SyntaxToken token)
            {
                return CodeAnalysis.Diagnostic.Create(Descriptor, token.GetLocation());
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class HighlightBraceSpanWithEndMarkedUnnecessaryAnalyzer : AbstractHighlightBraceSpanWithEndMarkedUnnecessaryAnalyzer
        {
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class HighlightBraceSpanWithEndMarkedUnnecessaryNotJsonArrayAnalyzer : AbstractHighlightBraceSpanWithEndMarkedUnnecessaryAnalyzer
        {
            protected override string UnnecessaryLocationsValue => "Text";
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class HighlightBraceSpanWithEndMarkedUnnecessaryIndexOutOfRangeAnalyzer : AbstractHighlightBraceSpanWithEndMarkedUnnecessaryAnalyzer
        {
            protected override string UnnecessaryLocationsValue => "[1]";
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class HighlightBraceSpanWithEndMarkedUnnecessaryButDescriptorNotAnalyzer : AbstractHighlightBraceSpanWithEndMarkedUnnecessaryAnalyzer
        {
            public HighlightBraceSpanWithEndMarkedUnnecessaryButDescriptorNotAnalyzer()
                : base(customTags: new string[0])
            {
            }
        }

        private abstract class AbstractHighlightBraceSpanWithEndMarkedUnnecessaryAnalyzer : AbstractHighlightBracesAnalyzer
        {
            public AbstractHighlightBraceSpanWithEndMarkedUnnecessaryAnalyzer(string[]? customTags = null)
                : base(customTags: customTags ?? new[] { WellKnownDiagnosticTags.Unnecessary })
            {
            }

            protected virtual string UnnecessaryLocationsValue => "[0]";

            protected override Diagnostic CreateDiagnostic(SyntaxToken token)
            {
                var endLocation = token.Parent switch
                {
                    CSharp.Syntax.ClassDeclarationSyntax classDeclaration => classDeclaration.CloseBraceToken.GetLocation(),
                    _ => throw new NotSupportedException(),
                };

                var additionalLocations = new[] { endLocation };
                var properties = ImmutableDictionary.Create<string, string?>().Add(WellKnownDiagnosticTags.Unnecessary, UnnecessaryLocationsValue);
                return CodeAnalysis.Diagnostic.Create(Descriptor, token.GetLocation(), additionalLocations, properties);
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class ReportCompilationDiagnosticAnalyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor Descriptor =
                new("Brace", "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

                context.RegisterCompilationAction(HandleCompilation);
            }

            private void HandleCompilation(CompilationAnalysisContext context)
            {
                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(Descriptor, location: null));
            }
        }
    }
}
