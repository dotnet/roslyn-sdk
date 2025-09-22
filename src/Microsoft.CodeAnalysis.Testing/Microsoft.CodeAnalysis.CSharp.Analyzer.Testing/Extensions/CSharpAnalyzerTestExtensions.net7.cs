// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing;

public static class CSharpAnalyzerTestExtensions
{
    public static CSharpAnalyzerTest<TAnalyzer, TVerifier> WithSource<TAnalyzer, TVerifier>(this CSharpAnalyzerTest<TAnalyzer, TVerifier> test, [StringSyntax("C#-test")] string source)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TVerifier : IVerifier, new()
    {
        test.TestState.Sources.Add(source);
        return test;
    }
}
