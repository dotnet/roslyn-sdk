﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing
{
    public class CSharpIncrementalGeneratorTest<TIncrementalGenerator, TVerifier> : IncrementalGeneratorTest<TVerifier>
        where TIncrementalGenerator : IIncrementalGenerator, new()
        where TVerifier : IVerifier, new()
    {
        private static readonly LanguageVersion DefaultLanguageVersion =
            Enum.TryParse("Default", out LanguageVersion version) ? version : LanguageVersion.CSharp6;

        protected override (IEnumerable<ISourceGenerator> sourceGenerators, IEnumerable<IIncrementalGenerator> incrementalGenerators) GetGenerators()
            => (Enumerable.Empty<ISourceGenerator>(), new IIncrementalGenerator[] { new TIncrementalGenerator() });

        protected override string DefaultFileExt => "cs";

        public override string Language => LanguageNames.CSharp;

        protected override GeneratorDriver CreateGeneratorDriver(Project project, ImmutableArray<ISourceGenerator> sourceGenerators, ImmutableArray<IIncrementalGenerator> incrementalGenerators)
        {
            return CSharpGeneratorDriver.Create(
                sourceGenerators.Concat(incrementalGenerators.Select(g => g.AsSourceGenerator())),
                project.AnalyzerOptions.AdditionalFiles,
                (CSharpParseOptions)project.ParseOptions!,
                project.AnalyzerOptions.AnalyzerConfigOptionsProvider);
        }

        protected override CompilationOptions CreateCompilationOptions()
            => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);

        protected override ParseOptions CreateParseOptions()
            => new CSharpParseOptions(DefaultLanguageVersion, DocumentationMode.Diagnose);
    }
}
