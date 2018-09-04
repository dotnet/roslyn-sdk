﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

#if !NETSTANDARD1_5
using System;
#endif

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Metadata references used to create test projects.
    /// </summary>
    public static class MetadataReferences
    {
#if NET452
        public static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location).WithAliases(ImmutableArray.Create("global", "corlib"));
        public static readonly MetadataReference SystemReference = MetadataReference.CreateFromFile(typeof(System.Diagnostics.Debug).GetTypeInfo().Assembly.Location).WithAliases(ImmutableArray.Create("global", "system"));
        public static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location);
        public static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).GetTypeInfo().Assembly.Location);
        public static readonly MetadataReference SystemCollectionsImmutableReference = MetadataReference.CreateFromFile(typeof(ImmutableArray).GetTypeInfo().Assembly.Location);
        public static readonly MetadataReference MicrosoftVisualBasicReference = MetadataReference.CreateFromFile(typeof(Microsoft.VisualBasic.Strings).GetTypeInfo().Assembly.Location);
#endif

#if NETSTANDARD1_5 || NETSTANDARD2_0
        public static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.FullName).WithAliases(ImmutableArray.Create("global", "corlib"));
        public static readonly MetadataReference SystemReference = MetadataReference.CreateFromFile(typeof(System.Diagnostics.Debug).GetTypeInfo().Assembly.FullName).WithAliases(ImmutableArray.Create("global", "system"));
        public static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.FullName);
        public static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).GetTypeInfo().Assembly.FullName);
        public static readonly MetadataReference SystemCollectionsImmutableReference = MetadataReference.CreateFromFile(typeof(ImmutableArray).GetTypeInfo().Assembly.FullName);
        public static readonly MetadataReference MicrosoftVisualBasicReference = MetadataReference.CreateFromFile(typeof(Microsoft.VisualBasic.Strings).GetTypeInfo().Assembly.FullName);
#endif
        public static readonly MetadataReference SystemRuntimeReference;
        public static readonly MetadataReference SystemValueTupleReference;

        static MetadataReferences()
        {
#if NETSTANDARD1_5
            if (typeof(string).GetTypeInfo().Assembly.ExportedTypes.Any(x => x.Name == "System.ValueTuple"))
            {
                // mscorlib contains ValueTuple, so no need to add a separate reference
                SystemRuntimeReference = null;
                SystemValueTupleReference = null;
            }
#elif NETSTANDARD2_0
            // mscorlib contains ValueTuple, so no need to add a separate reference
            SystemRuntimeReference = null;
            SystemValueTupleReference = null;
#elif NET452
            var systemRuntime = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(x => x.GetName().Name == "System.Runtime");
            if (systemRuntime != null)
            {
                SystemRuntimeReference = MetadataReference.CreateFromFile(systemRuntime.Location);
            }

            var systemValueTuple = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(x => x.GetName().Name == "System.ValueTuple");
            if (systemValueTuple != null)
            {
                SystemValueTupleReference = MetadataReference.CreateFromFile(systemValueTuple.Location);
            }
#else
#error Unsupported target framework.
#endif
        }
    }
}
