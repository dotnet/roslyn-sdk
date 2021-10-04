// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Defines a <see cref="CodeFixProvider"/> which does not support any diagnostic IDs or register code fixes for any
    /// diagnostics.
    /// </summary>
#pragma warning disable RS1016 // "This helper type for unit testing is never actually provided as an analyzer for projects to consume."
    public sealed class EmptyCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray<string>.Empty;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
            => Task.FromResult(true);
    }
}
