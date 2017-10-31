// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Sample.Analyzers.Test
{
    [TestClass]
    public class SymbolAnalyzerUnitTests : DiagnosticVerifier
    {
        [TestMethod]
        public void SymbolAnalyzerTest()
        {
            string test = @"
class BadOne
{
    public void BadOne() {}
}

class GoodOne
{
}";
            DiagnosticResult expected = new DiagnosticResult
            {
                Id = DiagnosticIds.SymbolAnalyzerRuleId,
                Message = string.Format(SymbolAnalyzer.MessageFormat, "BadOne"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 2, 7) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new SymbolAnalyzer();
    }
}
