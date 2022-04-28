﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Testing.XUnit
{
    public static class CodeFixVerifier
    {
        public static CodeFixVerifier<TAnalyzer, TCodeFix> Create<TAnalyzer, TCodeFix>()
            where TAnalyzer : DiagnosticAnalyzer, new()
            where TCodeFix : CodeFixProvider, new()
        {
            return new CodeFixVerifier<TAnalyzer, TCodeFix>();
        }
    }
}
