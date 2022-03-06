// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// A default verifier for incremental source generators.
    /// </summary>
    /// <typeparam name="TIncrementalGenerator">The <see cref="IIncrementalGenerator"/> to test.</typeparam>
    /// <typeparam name="TTest">The test implementation to use.</typeparam>
    /// <typeparam name="TVerifier">The type of verifier to use.</typeparam>
    public class IncrementalGeneratorVerifier<TIncrementalGenerator, TTest, TVerifier>
           where TIncrementalGenerator : IIncrementalGenerator, new()
           where TTest : IncrementalGeneratorTest<TVerifier>, new()
           where TVerifier : IVerifier, new()
    {
    }
}
