// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Testing.MSTest
{
    public static class IncrementalGeneratorVerifier
    {
        public static IncrementalGeneratorVerifier<TIncrementalGenerator> Create<TIncrementalGenerator>()
            where TIncrementalGenerator : IIncrementalGenerator, new()
        {
            return new IncrementalGeneratorVerifier<TIncrementalGenerator>();
        }
    }
}
