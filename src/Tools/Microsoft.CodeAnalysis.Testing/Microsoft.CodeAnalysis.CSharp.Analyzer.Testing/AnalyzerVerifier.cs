﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing.Default
{
    public class AnalyzerVerifier<TAnalyzer> : CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
    }
}
