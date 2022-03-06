// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Testing
{
    public abstract class SourceGeneratorTest<TVerifier> : IncrementalGeneratorTest<TVerifier>
        where TVerifier : IVerifier, new()
    {
        /// <summary>
        /// Returns the source generators being tested - to be implemented in non-abstract class.
        /// </summary>
        /// <returns>The <see cref="ISourceGenerator"/> to be used.</returns>
        protected abstract IEnumerable<ISourceGenerator> GetSourceGenerators();

        protected sealed override (IEnumerable<ISourceGenerator> sourceGenerators, IEnumerable<IIncrementalGenerator> incrementalGenerators) GetGenerators()
            => (GetSourceGenerators(), Enumerable.Empty<IIncrementalGenerator>());

        protected abstract GeneratorDriver CreateGeneratorDriver(Project project, ImmutableArray<ISourceGenerator> sourceGenerators);

        protected sealed override GeneratorDriver CreateGeneratorDriver(Project project, ImmutableArray<ISourceGenerator> sourceGenerators, ImmutableArray<IIncrementalGenerator> incrementalGenerators)
            => CreateGeneratorDriver(project, sourceGenerators);
    }
}
