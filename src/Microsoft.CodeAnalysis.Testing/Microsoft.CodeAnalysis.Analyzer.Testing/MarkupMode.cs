﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Indicates the manner in which markup syntax is treated within test inputs and outputs.
    /// </summary>
    /// <seealso cref="SolutionState.MarkupHandling"/>
    public enum MarkupMode
    {
        /// <summary>
        /// Markup syntax is disabled, and any syntax which could be treated as markup is preserved in the contents of
        /// sources and additional files.
        /// </summary>
        None,

        /// <summary>
        /// Markup syntax is allowed, but diagnostics suggested by markup syntax is ignored.
        /// </summary>
        Ignore,

        /// <summary>
        /// Markup syntax is allowed, but fixable diagnostics suggested by markup syntax are ignored.
        /// </summary>
        IgnoreFixable,

        /// <summary>
        /// Markup syntax is allowed, and all diagnostics indicated by markup syntax are preserved.
        /// </summary>
        Allow,
    }
}
