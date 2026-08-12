// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace ConsoleClassifier
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var workspace = new AdhocWorkspace();
            var solution = workspace.CurrentSolution;
            var project = solution.AddProject("projectName", "assemblyName", LanguageNames.CSharp);
            var document = project.AddDocument("name.cs",
    @"class C
{
static void Main()
{
WriteLine(""Hello, World!"");
}
}");
            document = await Formatter.FormatAsync(document);
            var text = await document.GetTextAsync();

            var classifiedSpans = await Classifier.GetClassifiedSpansAsync(document, TextSpan.FromBounds(0, text.Length));
            Console.BackgroundColor = ConsoleColor.Black;

            var ranges = classifiedSpans.Select(classifiedSpan =>
                new Range(classifiedSpan, text.GetSubText(classifiedSpan.TextSpan).ToString()));

            ranges = FillGaps(text, ranges);

            foreach (var range in ranges)
            {
                switch (range.ClassificationType)
                {
                    case "keyword":
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        break;
                    case "class name":
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;
                    case "string":
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                }

                Console.Write(range.Text);
            }

            Console.ResetColor();
            Console.WriteLine();
        }

        private static IEnumerable<Range> FillGaps(SourceText text, IEnumerable<Range> ranges)
        {
            const string WhitespaceClassification = null;
            var current = 0;
            Range previous = null;

            foreach (var range in ranges)
            {
                var start = range.TextSpan.Start;
                if (start > current)
                {
                    yield return new Range(WhitespaceClassification, TextSpan.FromBounds(current, start), text);
                }

                if (previous == null || range.TextSpan != previous.TextSpan)
                {
                    yield return range;
                }

                previous = range;
                current = range.TextSpan.End;
            }

            if (current < text.Length)
            {
                yield return new Range(WhitespaceClassification, TextSpan.FromBounds(current, text.Length), text);
            }
        }
    }
}
