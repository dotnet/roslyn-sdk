// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.CodeAnalysis.Testing.TestGenerators
{
    public class CheckGlobalOption : ISourceGenerator
    {
        public static string? ExpectedKey { get; set; }

        public static string? ExpectedValue { get; set; }

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public virtual void Execute(GeneratorExecutionContext context)
        {
            Assert.NotNull(ExpectedKey);

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(ExpectedKey!, out var value))
            {
                Assert.Equal(ExpectedValue, value);
                context.AddSource("EmptyGeneratedFile", string.Empty);
            }
        }
    }
}
