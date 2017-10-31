// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Sample.Analyzers.Test
{
    [TestClass]
    public class CompilationStartedAnalyzerUnitTests
            : DiagnosticVerifier
    {
        [TestMethod]
        public void CompilationStartedAnalyzerTest()
        {
            string test = @"
namespace MyInterfaces
{
    public interface Interface {}
    class MyInterfaceImpl : Interface
    {
    }
    class MyInterfaceImpl2 : Interface
    {
    }
}";
            DiagnosticResult expected = new DiagnosticResult
            {
                Id = DiagnosticIds.CompilationStartedAnalyzerRuleId,
                Message = string.Format(
                    CompilationStartedAnalyzer.MessageFormat, 
                    "MyInterfaceImpl2", 
                    CompilationStartedAnalyzer.DontInheritInterfaceTypeName),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 11) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new CompilationStartedAnalyzer();
    }
}
