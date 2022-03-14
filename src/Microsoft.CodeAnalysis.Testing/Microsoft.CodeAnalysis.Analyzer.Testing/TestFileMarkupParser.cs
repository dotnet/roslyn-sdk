// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// <para>To aid with testing, we define a special type of text file that can encode additional
    /// information in it.  This prevents a test writer from having to carry around multiple sources
    /// of information that must be reconstituted.  For example, instead of having to keep around the
    /// contents of a file <em>and</em> and the location of the cursor, the tester can just provide a
    /// string with the <c>$$</c> character in it.  This allows for easy creation of "FIT" tests where all
    /// that needs to be provided are strings that encode every bit of state necessary in the string
    /// itself.</para>
    ///
    /// <para>The current set of encoded features we support are:</para>
    ///
    /// <list type="bullet">
    ///   <item>
    ///     <term><c>$$</c></term>
    ///     <description>A position in the file. The number of times this is allowed to appear varies depending on the
    ///     specific call.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>[|</c> ... <c>|]</c></term>
    ///     <description>A span of text in the file. There can be many of these and they can be nested and/or overlap
    ///     the <c>$$</c> position.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>{|Name:</c> ... <c>|}</c></term>
    ///     <description>A span of text in the file annotated with an identifier. There can be many of these, including
    ///     ones with the same name.</description>
    ///   </item>
    /// </list>
    ///
    /// <para>Additional encoded features can be added on a case by case basis.</para>
    /// </summary>
    public static class TestFileMarkupParser
    {
        public static void GetPositionsAndSpans(string input, out string output, out ImmutableArray<int> positions, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans)
            => TestMarkupParser.Default.GetPositionsAndSpans(input, out output, out positions, out spans);

        public static void GetPositionAndSpans(string input, out string output, out int? cursorPosition, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans)
            => TestMarkupParser.Default.GetPositionAndSpans(input, out output, out cursorPosition, out spans);

        public static void GetPositionAndSpans(string input, out int? cursorPosition, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans)
            => TestMarkupParser.Default.GetPositionAndSpans(input, out cursorPosition, out spans);

        public static void GetPositionAndSpans(string input, out string output, out int cursorPosition, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans)
            => TestMarkupParser.Default.GetPositionAndSpans(input, out output, out cursorPosition, out spans);

        public static void GetSpans(string input, out string output, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans)
            => TestMarkupParser.Default.GetSpans(input, out output, out spans);

        public static void GetPositionAndSpans(string input, out string output, out int? cursorPosition, out ImmutableArray<TextSpan> spans)
            => TestMarkupParser.Default.GetPositionAndSpans(input, out output, out cursorPosition, out spans);

        public static void GetPositionAndSpans(string input, out int? cursorPosition, out ImmutableArray<TextSpan> spans)
            => TestMarkupParser.Default.GetPositionAndSpans(input, out cursorPosition, out spans);

        public static void GetPositionAndSpans(string input, out string output, out int cursorPosition, out ImmutableArray<TextSpan> spans)
            => TestMarkupParser.Default.GetPositionAndSpans(input, out output, out cursorPosition, out spans);

        /// <summary>
        /// Process markup containing exactly one position.
        /// </summary>
        /// <param name="input">The input markup.</param>
        /// <param name="output">The output, with markup syntax removed.</param>
        /// <param name="cursorPosition">The location of the <c>$$</c> position in <paramref name="input"/>.</param>
        /// <exception cref="ArgumentException">If <paramref name="input"/> does not contain exactly one position,
        /// indicated by <c>$$</c>.</exception>
        public static void GetPosition(string input, out string output, out int cursorPosition)
            => TestMarkupParser.Default.GetPosition(input, out output, out cursorPosition);

        public static void GetPositionAndSpan(string input, out string output, out int cursorPosition, out TextSpan span)
            => TestMarkupParser.Default.GetPositionAndSpan(input, out output, out cursorPosition, out span);

        public static void GetSpans(string input, out string output, out ImmutableArray<TextSpan> spans)
            => TestMarkupParser.Default.GetSpans(input, out output, out spans);

        public static void GetSpan(string input, out string output, out TextSpan span)
            => TestMarkupParser.Default.GetSpan(input, out output, out span);

        public static string CreateTestFile(string code, int position)
        {
            return CreateTestFile(code, ImmutableArray.Create(position), ImmutableDictionary<string, ImmutableArray<TextSpan>>.Empty);
        }

        public static string CreateTestFile(string code, int? position, ImmutableArray<TextSpan> spans)
        {
            return CreateTestFile(code, position, ImmutableDictionary<string, ImmutableArray<TextSpan>>.Empty.Add(string.Empty, spans));
        }

        public static string CreateTestFile(string code, int? position, ImmutableDictionary<string, ImmutableArray<TextSpan>> spans)
        {
            var positions = position is object ? ImmutableArray.Create(position.Value) : ImmutableArray<int>.Empty;
            return CreateTestFile(code, positions, spans.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.ToImmutableArray()));
        }

        public static string CreateTestFile(string code, ImmutableArray<int> positions, ImmutableDictionary<string, ImmutableArray<TextSpan>> spans)
        {
            var sb = new StringBuilder();
            var anonymousSpans = spans.GetValueOrDefault(string.Empty, ImmutableArray<TextSpan>.Empty);

            for (var i = 0; i <= code.Length; i++)
            {
                if (positions.Contains(i))
                {
                    sb.Append(TestMarkupParser.PositionString);
                }

                AddSpanString(sb, spans.Where(kvp => kvp.Key != string.Empty), i, start: true);
                AddSpanString(sb, spans.Where(kvp => kvp.Key?.Length == 0), i, start: true);
                AddSpanString(sb, spans.Where(kvp => kvp.Key?.Length == 0), i, start: false);
                AddSpanString(sb, spans.Where(kvp => kvp.Key != string.Empty), i, start: false);

                if (i < code.Length)
                {
                    sb.Append(code[i]);
                }
            }

            return sb.ToString();
        }

        private static void AddSpanString(
            StringBuilder sb,
            IEnumerable<KeyValuePair<string, ImmutableArray<TextSpan>>> items,
            int position,
            bool start)
        {
            foreach (var (name, spans) in items)
            {
                foreach (var span in spans)
                {
                    if (start && span.Start == position)
                    {
                        if (name.Length == 0)
                        {
                            sb.Append(TestMarkupParser.SpanStartString);
                        }
                        else
                        {
                            sb.Append(TestMarkupParser.NamedSpanStartString);
                            sb.Append(name);
                            sb.Append(':');
                        }
                    }
                    else if (!start && span.End == position)
                    {
                        if (name.Length == 0)
                        {
                            sb.Append(TestMarkupParser.SpanEndString);
                        }
                        else
                        {
                            sb.Append(TestMarkupParser.NamedSpanEndString);
                        }
                    }
                }
            }
        }
    }
}
