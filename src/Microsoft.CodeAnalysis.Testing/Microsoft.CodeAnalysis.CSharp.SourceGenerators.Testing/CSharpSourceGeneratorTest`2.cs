// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing
{
    public class CSharpSourceGeneratorTest<TSourceGenerator, TVerifier> : SourceGeneratorTest<TVerifier>, IGeneratorTestBase
        where TSourceGenerator : new()
        where TVerifier : IVerifier, new()
    {
        protected override IEnumerable<Type> GetSourceGenerators()
            => new Type[] { typeof(TSourceGenerator) };

        protected override string DefaultFileExt => "cs";

        public override string Language => LanguageNames.CSharp;

        /// <summary>
        /// Gets the global options to be used in <see cref="GetAnalyzerOptions"/>.
        /// This can be appended to by the user to provide additional options.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1414:Tuple types in signatures should have element names", Justification = "The tuple names in the global options are unknown.")]
        public List<(string, string)> GlobalOptions { get; } = new();

        /// <summary>
        /// Gets or sets the C# language version used for the test. The default is <see cref="LanguageVersion.Default"/>.
        /// </summary>
        public LanguageVersion LanguageVersion { get; set; } =
            Enum.TryParse("Default", out LanguageVersion version) ? version : LanguageVersion.CSharp6;

        protected override CompilationOptions CreateCompilationOptions()
            => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);

        protected override ParseOptions CreateParseOptions()
            => new CSharpParseOptions(LanguageVersion, DocumentationMode.Diagnose);

        protected override AnalyzerOptions GetAnalyzerOptions(Project project)
            => new(
                project.AnalyzerOptions.AdditionalFiles,
                new OptionsProvider(project.AnalyzerOptions.AnalyzerConfigOptionsProvider, GlobalOptions));
    }
}
