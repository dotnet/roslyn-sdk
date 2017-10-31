// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Sample.Analyzers.Test
{
    [TestClass]
    public class CompilationStartedAnalyzerWithCompilationWideAnalysisUnitTests
        : DiagnosticVerifier
    {
        [TestMethod]
        public void CompilationStartedAnalyzerWithCompilationWideAnalysisTest()
        {
            string test = @"
namespace MyNamespace
{
    public class UnsecureMethodAttribute : System.Attribute { }

    public interface ISecureType { }

    public interface IUnsecureInterface
    {
        [UnsecureMethodAttribute]
        void F();
    }

    class MyInterfaceImpl1 : IUnsecureInterface
    {
        public void F() {}
    }

    class MyInterfaceImpl2 : IUnsecureInterface, ISecureType
    {
        public void F() {}
    }

    class MyInterfaceImpl3 : ISecureType
    {
        public void F() {}
    }
}";
            DiagnosticResult expected = new DiagnosticResult
            {
                Id = DiagnosticIds.CompilationStartedAnalyzerWithCompilationWideAnalysisRuleId,
                Message = string.Format(
                    CompilationStartedAnalyzerWithCompilationWideAnalysis.MessageFormat,
                    "MyInterfaceImpl2",
                    CompilationStartedAnalyzerWithCompilationWideAnalysis.SecureTypeInterfaceName,
                    "IUnsecureInterface"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 19, 11) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer CSharpDiagnosticAnalyzer => new CompilationStartedAnalyzerWithCompilationWideAnalysis();
    }
}
