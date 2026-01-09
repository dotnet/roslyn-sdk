// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.Testing
{
    public class IncrementalGeneratorExpectedState
    {
        internal Dictionary<string, List<IncrementalGeneratorExpectedStepState>> ExpectedStepStates { get; } = new Dictionary<string, List<IncrementalGeneratorExpectedStepState>>();

        public List<IncrementalGeneratorExpectedStepState> this[string stepName]
        {
            get
            {
                return ExpectedStepStates.GetOrAdd(stepName, () => new List<IncrementalGeneratorExpectedStepState>());
            }
        }
    }
}
