// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Sample.Analyzers.Test
{
    public class SemanticModelAnalyzerUnitTests
        : DiagnosticVerifier
    {
        [Fact]
        public void SemanticModelAnalyzerTest()
        {
            string test = @"
class C
{
    public async int M()
    {
    }
}";
            DiagnosticResult expected = new DiagnosticResult
            {
                Id = DiagnosticIds.SemanticModelAnalyzerRuleId,
                Message = string.Format(SemanticModelAnalyzer.MessageFormat, "Test0.cs", 1),
                Severity = DiagnosticSeverity.Warning
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new SemanticModelAnalyzer();
    }
}
