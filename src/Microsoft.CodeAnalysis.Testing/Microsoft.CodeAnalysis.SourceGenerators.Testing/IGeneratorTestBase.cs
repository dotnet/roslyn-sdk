// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// This type is mainly used for declaring that a SourceGeneratorTest has global options set.
    ///
    /// It is also used for end users to be able to define their own generic generator
    /// test run method inside of their unit test projects to more easily run tests for their source generator.
    /// </summary>
    public interface IGeneratorTestBase
    {
        /// <summary>
        /// Gets the additional global options that will appear in the context.AnalyzerConfigOptions.GlobalOptions object.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1414:Tuple types in signatures should have element names", Justification = "The tuple names in the global options are unknown.")]
        public List<(string, string)> GlobalOptions { get; }
    }
}
