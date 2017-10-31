// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Sample.Analyzers.Test
{
    [TestClass]
    public class SyntaxNodeAnalyzerUnitTests : DiagnosticVerifier
    {
        [TestMethod]
        public void SyntaxNodeAnalyzerTest()
        {
            string test = @"
class C
{
    public void M()
    {
        var implicitTypedLocal = 0;
        int explicitTypedLocal = 1;
    }
}";
            DiagnosticResult expected = new DiagnosticResult
            {
                Id = DiagnosticIds.SyntaxNodeAnalyzerRuleId,
                Message = string.Format(SyntaxNodeAnalyzer.MessageFormat, "implicitTypedLocal"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 13) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new SyntaxNodeAnalyzer();
    }
}
