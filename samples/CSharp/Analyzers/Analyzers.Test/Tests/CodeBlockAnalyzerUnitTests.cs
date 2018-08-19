// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Sample.Analyzers.Test
{
    public class CodeBlockAnalyzerUnitTests
        : DiagnosticVerifier
    {
        [Fact]
        public void CodeBlockAnalyzerTest()
        {
            string test = @"
class C
{
    public void M1()
    {
    }

    public virtual void M2()
    {
    }

    public int M3()
    {
    }
}";
            DiagnosticResult expected = new DiagnosticResult
            {
                Id = DiagnosticIds.CodeBlockAnalyzerRuleId,
                Message = string.Format(CodeBlockAnalyzer.MessageFormat, "M1"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 17) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new CodeBlockAnalyzer();
    }
}
