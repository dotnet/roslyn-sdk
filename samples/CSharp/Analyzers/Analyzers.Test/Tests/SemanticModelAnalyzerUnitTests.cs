// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Sample.Analyzers.SemanticModelAnalyzer>;

namespace Sample.Analyzers.Test
{
    public class SemanticModelAnalyzerUnitTests
    {
        [Fact]
        public async Task SemanticModelAnalyzerTest()
        {
            string test = @"
class C
{
    public async int M()
    {
    }
}";
            DiagnosticResult[] expected =
            {
                Verify.Diagnostic().WithArguments("Test0.cs", 1),
            };
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    }
}
