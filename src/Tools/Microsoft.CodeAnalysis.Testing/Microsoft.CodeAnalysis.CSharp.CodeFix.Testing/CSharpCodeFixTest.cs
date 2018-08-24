﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing
{
    public class CSharpCodeFixTest<TAnalyzer, TCodeFix, TVerifier> : CodeFixTest<TVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix  : CodeFixProvider, new()
        where TVerifier : IVerifier, new()
    {
        protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
            => new[] { new TCodeFix() };

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            => new[] { new TAnalyzer() };

        protected override string DefaultFileExt => "cs";
        public override string Language => LanguageNames.CSharp;

        protected override CompilationOptions CreateCompilationOptions()
            => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);
    }
}
