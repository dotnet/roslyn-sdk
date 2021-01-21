﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing.Model
{
    /// <summary>
    /// Represents an evaluated <see cref="ProjectState"/>.
    /// </summary>
    public sealed class EvaluatedProjectState
    {
        public EvaluatedProjectState(ProjectState state, ReferenceAssemblies defaultReferenceAssemblies)
            : this(
                state.Name,
                state.AssemblyName,
                state.Language,
                state.ReferenceAssemblies ?? defaultReferenceAssemblies,
                state.OutputKind ?? OutputKind.DynamicallyLinkedLibrary,
                state.DocumentationMode ?? DocumentationMode.Diagnose,
                state.Sources.ToImmutableArray(),
                state.AdditionalFiles.ToImmutableArray(),
                state.AdditionalProjectReferences.ToImmutableArray(),
                state.AdditionalReferences.ToImmutableArray())
        {
        }

        private EvaluatedProjectState(
            string name,
            string assemblyName,
            string language,
            ReferenceAssemblies referenceAssemblies,
            OutputKind outputKind,
            DocumentationMode documentationMode,
            ImmutableArray<(string filename, SourceText content)> sources,
            ImmutableArray<(string filename, SourceText content)> additionalFiles,
            ImmutableArray<string> additionalProjectReferences,
            ImmutableArray<MetadataReference> additionalReferences)
        {
            Name = name;
            AssemblyName = assemblyName;
            Language = language;
            ReferenceAssemblies = referenceAssemblies;
            OutputKind = outputKind;
            DocumentationMode = documentationMode;
            Sources = sources;
            AdditionalFiles = additionalFiles;
            AdditionalProjectReferences = additionalProjectReferences;
            AdditionalReferences = additionalReferences;
        }

        public string Name { get; }

        public string AssemblyName { get; }

        public string Language { get; }

        public ReferenceAssemblies ReferenceAssemblies { get; }

        public OutputKind OutputKind { get; }

        public DocumentationMode DocumentationMode { get; }

        public ImmutableArray<(string filename, SourceText content)> Sources { get; }

        public ImmutableArray<(string filename, SourceText content)> AdditionalFiles { get; }

        public ImmutableArray<string> AdditionalProjectReferences { get; }

        public ImmutableArray<MetadataReference> AdditionalReferences { get; }

        public EvaluatedProjectState WithSources(ImmutableArray<(string filename, SourceText content)> sources)
        {
            if (sources == Sources)
            {
                return this;
            }

            return With(sources: sources);
        }

        private EvaluatedProjectState With(
            Optional<string> name = default,
            Optional<string> assemblyName = default,
            Optional<string> language = default,
            Optional<ReferenceAssemblies> referenceAssemblies = default,
            Optional<OutputKind> outputKind = default,
            Optional<DocumentationMode> documentationMode = default,
            Optional<ImmutableArray<(string filename, SourceText content)>> sources = default,
            Optional<ImmutableArray<(string filename, SourceText content)>> additionalFiles = default,
            Optional<ImmutableArray<string>> additionalProjectReferences = default,
            Optional<ImmutableArray<MetadataReference>> additionalReferences = default)
        {
            return new EvaluatedProjectState(
                GetValueOrDefault(name, Name),
                GetValueOrDefault(assemblyName, AssemblyName),
                GetValueOrDefault(language, Language),
                GetValueOrDefault(referenceAssemblies, ReferenceAssemblies),
                GetValueOrDefault(outputKind, OutputKind),
                GetValueOrDefault(documentationMode, DocumentationMode),
                GetValueOrDefault(sources, Sources),
                GetValueOrDefault(additionalFiles, AdditionalFiles),
                GetValueOrDefault(additionalProjectReferences, AdditionalProjectReferences),
                GetValueOrDefault(additionalReferences, AdditionalReferences));
        }

        private static T GetValueOrDefault<T>(Optional<T> optionalValue, T defaultValue)
        {
            return optionalValue.HasValue ? optionalValue.Value : defaultValue;
        }
    }
}
