// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Sample.Analyzers.SyntaxNodeAnalyzer>;

namespace Sample.Analyzers.Test
{
    public class SyntaxNodeAnalyzerUnitTests
    {
        [Fact]
        public async Task SyntaxNodeAnalyzerTest()
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
            DiagnosticResult expected = Verify.Diagnostic().WithArguments("implicitTypedLocal").WithLocation(6, 13);
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    }
}
