// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Sample.Analyzers.CompilationAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Sample.Analyzers.Test
{
    public class CompilationAnalyzerUnitTests
    {
        [Fact]
        public async Task CompilationAnalyzerTest()
        {
            var test = @"
class C
{
    public void M()
    {
    }
}";

            var specificOption =
                new KeyValuePair<string, ReportDiagnostic>(DiagnosticIds.SymbolAnalyzerRuleId, ReportDiagnostic.Error);

            await new CSharpAnalyzerTest<CompilationAnalyzer, DefaultVerifier>
            {
                TestCode = test,
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("CS5001").WithMessage("Program does not contain a static 'Main' method suitable for an entry point"),
                },
                SolutionTransforms =
                {
                    (solution, projectId) =>
                    {
                        var options = (CSharpCompilationOptions)solution.GetProject(projectId).CompilationOptions
                            .WithOutputKind(OutputKind.ConsoleApplication)
                            .WithSpecificDiagnosticOptions(new[] { specificOption });
                        return solution.WithProjectCompilationOptions(projectId, options);
                    },
                }
            }.RunAsync();

            specificOption = new KeyValuePair<string, ReportDiagnostic>(DiagnosticIds.SymbolAnalyzerRuleId, ReportDiagnostic.Suppress);
            await new CSharpAnalyzerTest<CompilationAnalyzer, DefaultVerifier>
            {
                TestCode = test,
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("CS5001").WithMessage("Program does not contain a static 'Main' method suitable for an entry point"),
                    Verify.Diagnostic().WithArguments(DiagnosticIds.SymbolAnalyzerRuleId),
                },
                SolutionTransforms =
                {
                    (solution, projectId) =>
                    {
                        var options = (CSharpCompilationOptions)solution.GetProject(projectId).CompilationOptions
                            .WithOutputKind(OutputKind.ConsoleApplication)
                            .WithSpecificDiagnosticOptions(new[] { specificOption });
                        return solution.WithProjectCompilationOptions(projectId, options);
                    },
                }
            }.RunAsync();
        }
    }
}
