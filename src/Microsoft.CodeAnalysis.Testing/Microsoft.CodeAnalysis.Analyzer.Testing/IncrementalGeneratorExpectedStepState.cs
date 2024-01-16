﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Testing
{
    public class IncrementalGeneratorExpectedStepState
    {
        public List<IncrementalStepExpectedRunReason> InputRunReasons { get; } = new List<IncrementalStepExpectedRunReason>();

        public List<IncrementalStepExpectedRunReason> OutputRunReasons { get; } = new List<IncrementalStepExpectedRunReason>();
    }
}