// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;

namespace Sample.Analyzers.Test
{
    [TestClass]
    public class CompilationAnalyzerUnitTests
        : DiagnosticVerifier
    {
        [TestMethod]
        public void CompilationAnalyzerTest()
        {
            string test = @"
class C
{
    public void M()
    {
    }
}";
            DiagnosticResult expected = new DiagnosticResult
            {
                Id = DiagnosticIds.CompilationAnalyzerRuleId,
                Message = string.Format(CompilationAnalyzer.MessageFormat, DiagnosticIds.SymbolAnalyzerRuleId),
                Severity = DiagnosticSeverity.Warning
            };

            KeyValuePair<string, ReportDiagnostic> specificOption =
                new KeyValuePair<string, ReportDiagnostic>(DiagnosticIds.SymbolAnalyzerRuleId, ReportDiagnostic.Error);

            CSharpCompilationOptions compilationOptions =
                new CSharpCompilationOptions(OutputKind.ConsoleApplication,
                                             specificDiagnosticOptions: new[] { specificOption });
            VerifyCSharpDiagnostic(test, parseOptions: null, compilationOptions: compilationOptions);

            specificOption = new KeyValuePair<string, ReportDiagnostic>(DiagnosticIds.SymbolAnalyzerRuleId, ReportDiagnostic.Suppress);
            compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(new[] { specificOption });
            VerifyCSharpDiagnostic(test, parseOptions: null, compilationOptions: compilationOptions, expected: expected);
        }

        protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new CompilationAnalyzer();
    }
}
