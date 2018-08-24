// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Testing.MSTest
{
    public static class CodefixVerifier
    {
        public static CodefixVerifier<TAnalyzer, TCodefix> Create<TAnalyzer, TCodefix>()
            where TAnalyzer : DiagnosticAnalyzer, new()
            where TCodefix : CodeFixProvider, new()
        {
            return new CodefixVerifier<TAnalyzer, TCodefix>();
        }
    }
}
