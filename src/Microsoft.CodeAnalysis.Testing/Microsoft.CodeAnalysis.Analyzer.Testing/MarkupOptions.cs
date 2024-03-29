﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Specifies additional options for the markup parser.
    /// </summary>
    [Flags]
    public enum MarkupOptions
    {
        /// <summary>
        /// No additional markup options are specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Use the first matching diagnostic descriptor when multiple diagnostics match the syntax. By default, this
        /// option is not specified and the markup parser will fail when the syntax does not represent a <em>unique</em>
        /// descriptor.
        /// </summary>
        UseFirstDescriptor = 0x0001,

        /// <summary>
        /// Ignore position indicators (<c>$$</c>) in markup processing. Spans and named spans are still supported in markup.
        /// </summary>
        /// <remarks>
        /// This flag makes it easier to write tests for code containing interpolated raw strings (<see href="https://github.com/dotnet/roslyn-sdk/issues/1067">dotnet/roslyn-sdk#1067</see>).
        /// </remarks>
        TreatPositionIndicatorsAsCode = 0x0002,
    }
}
