// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Sample.Analyzers.SyntaxTreeAnalyzer>;

namespace Sample.Analyzers.Test
{
    public class SyntaxTreeAnalyzerUnitTests
    {
        [Fact]
        public async Task SyntaxTreeAnalyzerTest()
        {
            string test = @"
class C
{
    public void M()
    {
    }
}";
            DiagnosticResult expected = Verify.Diagnostic().WithArguments("Test0.cs");

            await new CSharpAnalyzerTest<SyntaxTreeAnalyzer, XUnitVerifier>
            {
                TestCode = test,
                SolutionTransforms =
                {
                    (solution, projectId) =>
                    {
                        CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose);
                        return solution.WithProjectParseOptions(projectId, parseOptions);
                    },
                }
            }.RunAsync();

            await new CSharpAnalyzerTest<SyntaxTreeAnalyzer, XUnitVerifier>
            {
                TestCode = test,
                ExpectedDiagnostics = { expected },
                SolutionTransforms =
                {
                    (solution, projectId) =>
                    {
                        CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.None);
                        return solution.WithProjectParseOptions(projectId, parseOptions);
                    },
                }
            }.RunAsync();

            await new CSharpAnalyzerTest<SyntaxTreeAnalyzer, XUnitVerifier>
            {
                TestCode = test,
                ExpectedDiagnostics = { expected },
                SolutionTransforms =
                {
                    (solution, projectId) =>
                    {
                        CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Parse);
                        return solution.WithProjectParseOptions(projectId, parseOptions);
                    },
                }
            }.RunAsync();
        }
    }
}
