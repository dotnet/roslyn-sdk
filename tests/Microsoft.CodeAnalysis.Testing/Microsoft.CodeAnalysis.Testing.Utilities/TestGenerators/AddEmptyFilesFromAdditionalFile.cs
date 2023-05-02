// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

#if NET6_0_OR_GREATER

namespace Microsoft.CodeAnalysis.Testing.TestGenerators
{
    public class AddEmptyFilesFromAdditionalFile : IIncrementalGenerator
    {
        public const string GetFileText = nameof(GetFileText);

        public const string GetLinesFromFile = nameof(GetLinesFromFile);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(
                context.AdditionalTextsProvider
                    .Where(text => text.Path == "FilesToCreate.txt")
                    .Select((text, ct) =>
                    {
                        var source = text.GetText(ct);
                        var writer = new StringWriter();
                        source!.Write(writer, ct);
                        return writer.ToString();
                    })
                    .WithTrackingName(GetFileText)
                    .SelectMany((writer, ct) => writer.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    .WithTrackingName(GetLinesFromFile),
                (context, line) =>
                {
                    context.AddSource(line, string.Empty);
                });
        }
    }
}

#endif
