﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

#if !NETSTANDARD1_5
using System;
#endif

namespace Roslyn.UnitTestFramework
{
    /// <summary>
    /// Metadata references used to create test projects.
    /// </summary>
    public static class MetadataReferences
    {
        public static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location).WithAliases(ImmutableArray.Create("global", "corlib"));
        public static readonly MetadataReference SystemReference = MetadataReference.CreateFromFile(typeof(System.Diagnostics.Debug).GetTypeInfo().Assembly.Location).WithAliases(ImmutableArray.Create("global", "system"));
        public static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location);
        public static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).GetTypeInfo().Assembly.Location);
        public static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).GetTypeInfo().Assembly.Location);

        public static readonly MetadataReference SystemRuntimeReference;
        public static readonly MetadataReference SystemValueTupleReference;

        static MetadataReferences()
        {
            if (typeof(string).GetTypeInfo().Assembly.GetType("System.ValueTuple", false) != null)
            {
                // mscorlib contains ValueTuple, so no need to add a separate reference
                SystemRuntimeReference = null;
                SystemValueTupleReference = null;
            }
            else
            {
#if !NETSTANDARD1_5
                Assembly systemRuntime = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(x => x.GetName().Name == "System.Runtime");
                if (systemRuntime != null)
                {
                    SystemRuntimeReference = MetadataReference.CreateFromFile(systemRuntime.Location);
                }

                Assembly systemValueTuple = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(x => x.GetName().Name == "System.ValueTuple");
                if (systemValueTuple != null)
                {
                    SystemValueTupleReference = MetadataReference.CreateFromFile(systemValueTuple.Location);
                }
#endif
            }
        }
    }
}
