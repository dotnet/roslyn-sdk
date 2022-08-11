﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing.TestAnalyzers;
using Xunit;
using CSharpTest = Microsoft.CodeAnalysis.Testing.TestAnalyzers.CSharpAnalyzerTest<
    Microsoft.CodeAnalysis.Testing.TestAnalyzers.HighlightBracesAnalyzer>;
using VerifyCS = Microsoft.CodeAnalysis.Testing.AnalyzerVerifier<
    Microsoft.CodeAnalysis.Testing.TestAnalyzers.HighlightBracesAnalyzer,
    Microsoft.CodeAnalysis.Testing.TestAnalyzers.CSharpAnalyzerTest<Microsoft.CodeAnalysis.Testing.TestAnalyzers.HighlightBracesAnalyzer>,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using VisualBasicTest = Microsoft.CodeAnalysis.Testing.TestAnalyzers.VisualBasicAnalyzerTest<
    Microsoft.CodeAnalysis.Testing.TestAnalyzers.HighlightBracesAnalyzer>;

namespace Microsoft.CodeAnalysis.Testing
{
    public class MultipleProjectsTests
    {
        [Fact]
        public async Task TwoCSharpProjects_Independent()
        {
            await new CSharpTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"public class Derived1 : {|CS0246:Base2|} [|{|] }",
                        @"public class Base1 [|{|] }",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary"] =
                        {
                            Sources =
                            {
                                @"public class Derived2 : {|CS0246:Base1|} [|{|] }",
                                @"public class Base2 [|{|] }",
                            },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TwoVisualBasicProjects_Independent()
        {
            await new VisualBasicTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"Public Class Derived1 : Inherits {|BC30002:Base2|} : End Class",
                        @"Public Class Base1 : End Class",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary"] =
                        {
                            Sources =
                            {
                                @"Public Class Derived2 : Inherits {|BC30002:Base1|} : End Class",
                                @"Public Class Base2 : End Class",
                            },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task OneCSharpProjectOneVisualBasicProject_Independent()
        {
            await new CSharpTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"public class Derived1 : {|CS0246:Base2|} [|{|] }",
                        @"public class Base1 [|{|] }",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary", LanguageNames.VisualBasic] =
                        {
                            Sources =
                            {
                                @"Public Class Derived2 : Inherits {|BC30002:Base1|} : End Class",
                                @"Public Class Base2 : End Class",
                            },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task OneVisualBasicProjectOneCSharpProject_Independent()
        {
            await new VisualBasicTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"Public Class Derived1 : Inherits {|BC30002:Base2|} : End Class",
                        @"Public Class Base1 : End Class",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary", LanguageNames.CSharp] =
                        {
                            Sources =
                            {
                                @"public class Derived2 : {|CS0246:Base1|} [|{|] }",
                                @"public class Base2 [|{|] }",
                            },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TwoCSharpProjects_IndependentWithMarkupLocations()
        {
            await new CSharpTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"public class Derived1 : {|#0:Base2|} {|#1:{|} }",
                        @"public class Base1 {|#2:{|} }",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary"] =
                        {
                            Sources =
                            {
                                @"public class Derived2 : {|#3:Base1|} {|#4:{|} }",
                                @"public class Base2 {|#5:{|} }",
                            },
                        },
                    },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(1,25): error CS0246: The type or namespace name 'Base2' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithLocation(0).WithArguments("Base2"),

                        // /0/Test0.cs(1,31): warning Brace: message
                        VerifyCS.Diagnostic().WithLocation(1),

                        // /0/Test1.cs(1,20): warning Brace: message
                        VerifyCS.Diagnostic().WithLocation(2),

                        // /Secondary/Test0.cs(1,25): error CS0246: The type or namespace name 'Base1' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithLocation(3).WithArguments("Base1"),

                        // /Secondary/Test0.cs(1,31): warning Brace: message
                        VerifyCS.Diagnostic().WithLocation(4),

                        // /Secondary/Test1.cs(1,20): warning Brace: message
                        VerifyCS.Diagnostic().WithLocation(5),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TwoCSharpProjects_PrimaryReferencesSecondary()
        {
            // TestProject references Secondary
            await new CSharpTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"public class Derived1 : Base2 [|{|] }",
                        @"public class Base1 [|{|] }",
                    },
                    AdditionalProjectReferences = { "Secondary", },
                    AdditionalProjects =
                    {
                        ["Secondary"] =
                        {
                            Sources =
                            {
                                @"public class Derived2 : {|CS0246:Base1|} [|{|] }",
                                @"public class Base2 [|{|] }",
                            },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TwoVisualBasicProjects_PrimaryReferencesSecondary()
        {
            // TestProject references Secondary
            await new VisualBasicTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"Public Class Derived1 : Inherits Base2 : End Class",
                        @"Public Class Base1 : End Class",
                    },
                    AdditionalProjectReferences = { "Secondary", },
                    AdditionalProjects =
                    {
                        ["Secondary"] =
                        {
                            Sources =
                            {
                                @"Public Class Derived2 : Inherits {|BC30002:Base1|} : End Class",
                                @"Public Class Base2 : End Class",
                            },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task OneCSharpProjectOneVisualBasicProject_PrimaryReferencesSecondary()
        {
            // TestProject references Secondary
            await new CSharpTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"public class Type1 [|{|] object field = new Type3(); }",
                        @"public class Type2 [|{|] }",
                    },
                    AdditionalProjectReferences = { "Secondary", },
                    AdditionalProjects =
                    {
                        ["Secondary", LanguageNames.VisualBasic] =
                        {
                            Sources =
                            {
                                @"Public Class Type3 : Private field As Object = New {|BC30002:Type1|}() : End Class",
                                @"Public Class Type4 : End Class",
                            },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task OneVisualBasicProjectOneCSharpProject_PrimaryReferencesSecondary()
        {
            // TestProject references Secondary
            await new VisualBasicTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"Public Class Type1 : Private field As Object = New Type3() : End Class",
                        @"Public Class Type2 : End Class",
                    },
                    AdditionalProjectReferences = { "Secondary", },
                    AdditionalProjects =
                    {
                        ["Secondary", LanguageNames.CSharp] =
                        {
                            Sources =
                            {
                                @"public class Type3 [|{|] object field = new {|CS0246:Type1|}(); }",
                                @"public class Type4 [|{|] }",
                            },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TwoCSharpProjects_SecondaryReferencesPrimary()
        {
            // Secondary references TestProject
            await new CSharpTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"public class Derived1 : {|CS0246:Base2|} [|{|] }",
                        @"public class Base1 [|{|] }",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary"] =
                        {
                            Sources =
                            {
                                @"public class Derived2 : Base1 [|{|] }",
                                @"public class Base2 [|{|] }",
                            },
                            AdditionalProjectReferences = { "TestProject" },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TwoVisualBasicProjects_SecondaryReferencesPrimary()
        {
            // TestProject references Secondary
            await new VisualBasicTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"Public Class Derived1 : Inherits {|BC30002:Base2|} : End Class",
                        @"Public Class Base1 : End Class",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary"] =
                        {
                            Sources =
                            {
                                @"Public Class Derived2 : Inherits Base1 : End Class",
                                @"Public Class Base2 : End Class",
                            },
                            AdditionalProjectReferences = { "TestProject" },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task OneCSharpProjectOneVisualBasicProject_SecondaryReferencesPrimary()
        {
            // TestProject references Secondary
            await new CSharpTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"public class Type1 [|{|] object field = new {|CS0246:Type3|}(); }",
                        @"public class Type2 [|{|] }",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary", LanguageNames.VisualBasic] =
                        {
                            Sources =
                            {
                                @"Public Class Type3 : Private field As Object = New Type1() : End Class",
                                @"Public Class Type4 : End Class",
                            },
                            AdditionalProjectReferences = { "TestProject" },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task OneVisualBasicProjectOneCSharpProject_SecondaryReferencesPrimary()
        {
            // TestProject references Secondary
            await new VisualBasicTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"Public Class Type1 : Private field As Object = New {|BC30002:Type3|}() : End Class",
                        @"Public Class Type2 : End Class",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary", LanguageNames.CSharp] =
                        {
                            Sources =
                            {
                                @"public class Type3 [|{|] object field = new Type1(); }",
                                @"public class Type4 [|{|] }",
                            },
                            AdditionalProjectReferences = { "TestProject" },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TwoCSharpProjects_DefaultPaths()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<EmptyDiagnosticAnalyzer>
                {
                    TestState =
                    {
                        Sources =
                        {
                            @"public class Derived1 : Base1 { }",
                        },
                        AdditionalProjects =
                        {
                            ["Secondary"] =
                            {
                                Sources =
                                {
                                    @"public class Derived2 : Base2 { }",
                                },
                            },
                        },
                    },
                }.RunAsync();
            });

            var expected =
                "Mismatch between number of diagnostics returned, expected \"0\" actual \"2\"" + Environment.NewLine
                + Environment.NewLine
                + "Diagnostics:" + Environment.NewLine
                + "// /0/Test0.cs(1,25): error CS0246: The type or namespace name 'Base1' could not be found (are you missing a using directive or an assembly reference?)" + Environment.NewLine
                + "DiagnosticResult.CompilerError(\"CS0246\").WithSpan(1, 25, 1, 30).WithArguments(\"Base1\")," + Environment.NewLine
                + "// /Secondary/Test0.cs(1,25): error CS0246: The type or namespace name 'Base2' could not be found (are you missing a using directive or an assembly reference?)" + Environment.NewLine
                + "DiagnosticResult.CompilerError(\"CS0246\").WithSpan(\"/Secondary/Test0.cs\", 1, 25, 1, 30).WithArguments(\"Base2\")," + Environment.NewLine
                + Environment.NewLine;

            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TwoProjectsWithAdditionalReferences()
        {
            MetadataReference additionalReference = CSharpCompilation
                .Create("ExtraAssembly", references: await ReferenceAssemblies.Default.ResolveAsync(LanguageNames.CSharp, cancellationToken: default))
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(@"public class Base {}"))
                .ToMetadataReference();

            await new CSharpAnalyzerTest<EmptyDiagnosticAnalyzer>
            {
                TestState =
                {
                    Sources =
                    {
                        @"public class Derived1 : Base { }",
                    },
                    AdditionalReferences =
                    {
                        additionalReference,
                    },
                    AdditionalProjects =
                    {
                        ["Secondary"] =
                        {
                            Sources =
                            {
                                @"public class Derived2 : Base { }",
                            },
                            AdditionalReferences =
                            {
                                additionalReference,
                            },
                        },
                    },
                },
            }.RunAsync();
        }
    }
}
