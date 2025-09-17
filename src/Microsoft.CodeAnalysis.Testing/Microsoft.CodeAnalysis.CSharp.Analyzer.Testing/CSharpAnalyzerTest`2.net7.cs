// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing;

public partial class CSharpAnalyzerTest<TAnalyzer, TVerifier>
{
    // For net9.0 the second parameter could be 'params ReadOnlySpan<DiagnosticResult> expectedDiagnostics'
    public static CSharpAnalyzerTest<TAnalyzer, TVerifier> Create([StringSyntax("C#-test")] string source, params DiagnosticResult[] expectedDiagnostics)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, TVerifier> { TestCode = source };
        test.ExpectedDiagnostics.AddRange(expectedDiagnostics);

        return test;
    }
}
