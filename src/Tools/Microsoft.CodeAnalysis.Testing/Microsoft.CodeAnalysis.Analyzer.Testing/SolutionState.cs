// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    public class SolutionState
    {
        private readonly string _defaultPrefix;
        private readonly string _defaultExtension;

        public SolutionState(string defaultPrefix, string defaultExtension, MarkupMode markupMode)
        {
            _defaultPrefix = defaultPrefix;
            _defaultExtension = defaultExtension;

            Sources = new SourceFileList(defaultPrefix, defaultExtension);
            MarkupHandling = markupMode;
        }

        public StateInheritanceMode InheritanceMode { get; set; } = StateInheritanceMode.AutoInherit;

        /// <summary>
        /// Gets the set of source files for analyzer or code fix testing. Files may be added to this list using one of
        /// the <see cref="SourceFileList.Add(string)"/> methods.
        /// </summary>
        public SourceFileList Sources { get; }

        public SourceFileCollection AdditionalFiles { get; } = new SourceFileCollection();

        public List<Func<IEnumerable<(string filename, SourceText content)>>> AdditionalFilesFactories { get; } = new List<Func<IEnumerable<(string filename, SourceText content)>>>();

        /// <summary>
        /// Gets the list of diagnostics expected in the source(s) and/or additonal files.
        /// </summary>
        public List<DiagnosticResult> ExpectedDiagnostics { get; } = new List<DiagnosticResult>();

        /// <summary>
        /// Gets or sets a value indicating the manner in which markup syntax is treated within test inputs and outputs.
        /// The default value is <see cref="MarkupMode.Allow"/> for original (input) sources or
        /// <see cref="MarkupMode.IgnoreFixable"/> for fixed (output) sources.
        /// </summary>
        /// <remarks>
        /// <para>Diagnostics expressed using markup are combined with explicitly-specified expected diagnostics.</para>
        ///
        /// <para>Supported markup syntax includes the following:</para>
        ///
        /// <list type="bullet">
        /// <item><description><c>[|text|]</c>: indicates that a diagnostic is reported for <c>text</c>. The diagnostic
        /// descriptor is located via <see cref="AnalyzerTest{TVerifier}.GetDiagnosticAnalyzers"/>. This syntax may only
        /// be used when the first analyzer provided by <see cref="AnalyzerTest{TVerifier}.GetDiagnosticAnalyzers"/>
        /// supports a single diagnostic.</description></item>
        /// <item><description><c>{|ID1:text|}</c>: indicates that a diagnostic with ID <c>ID1</c> is reported for
        /// <c>text</c>. The diagnostic descriptor for <c>ID1</c> is located via
        /// <see cref="AnalyzerTest{TVerifier}.GetDiagnosticAnalyzers"/>. If no matching descriptor is found, the
        /// diagnostic is assumed to be a compiler-reported diagnostic with the specified ID and severity
        /// <see cref="DiagnosticSeverity.Error"/>.</description></item>
        /// </list>
        /// </remarks>
        public MarkupMode MarkupHandling { get; set; }

        /// <summary>
        /// Applies the <see cref="InheritanceMode"/> using a specified base state.
        /// </summary>
        /// <remarks>
        /// <para>This method evaluates <see cref="AdditionalFilesFactories"/>, and places the resulting additional
        /// files in the <see cref="AdditionalFiles"/> collection of the result before returning.</para>
        /// </remarks>
        /// <param name="baseState">The base state to inherit from, or <see langword="null"/> if the current state is
        /// the root state.</param>
        /// <param name="fixableDiagnostics">The set of diagnostic IDs to treat as fixable. Fixable diagnostics present
        /// in the <see cref="ExpectedDiagnostics"/> collection of the base state are never inherited.</param>
        /// <returns>A new <see cref="SolutionState"/> representing the current state with inherited values applied
        /// where appropriate. The <see cref="InheritanceMode"/> of the result is
        /// <see cref="StateInheritanceMode.Explicit"/>.</returns>
        public SolutionState WithInheritedValuesApplied(SolutionState baseState, ImmutableArray<string> fixableDiagnostics)
        {
            Debug.Assert(
                InheritanceMode == StateInheritanceMode.AutoInherit || InheritanceMode == StateInheritanceMode.Explicit,
                $"Unexpected inheritance mode: {InheritanceMode}");

            if (baseState?.AdditionalFilesFactories.Count > 0)
            {
                throw new InvalidOperationException("The base state should already have its inheritance state evaluated prior to its use as a base state.");
            }

            var result = new SolutionState(_defaultPrefix, _defaultExtension, MarkupHandling);
            if (InheritanceMode == StateInheritanceMode.AutoInherit && baseState != null)
            {
                if (Sources.Count == 0)
                {
                    result.Sources.AddRange(baseState.Sources);
                }

                if (AdditionalFiles.Count == 0)
                {
                    result.AdditionalFiles.AddRange(baseState.AdditionalFiles);
                }

                if (ExpectedDiagnostics.Count == 0)
                {
                    result.ExpectedDiagnostics.AddRange(baseState.ExpectedDiagnostics.Where(diagnostic => !fixableDiagnostics.Contains(diagnostic.Id)));
                }
            }

            result.InheritanceMode = StateInheritanceMode.Explicit;
            result.Sources.AddRange(Sources);
            result.AdditionalFiles.AddRange(AdditionalFiles);
            result.ExpectedDiagnostics.AddRange(ExpectedDiagnostics);
            result.AdditionalFiles.AddRange(AdditionalFilesFactories.SelectMany(factory => factory()));
            return result;
        }

        /// <summary>
        /// Processes the markup syntax for this <see cref="SolutionState"/> according to the current
        /// <see cref="MarkupHandling"/>, and returns a new <see cref="SolutionState"/> with the <see cref="Sources"/>,
        /// <see cref="AdditionalFiles"/>, and <see cref="ExpectedDiagnostics"/> updated accordingly.
        /// </summary>
        /// <param name="defaultDiagnostic">The diagnostic descriptor to use for markup spans without an explicit name,
        /// or <see langword="null"/> if no such default exists.</param>
        /// <param name="supportedDiagnostics">The diagnostics supported by analyzers used by the test.</param>
        /// <param name="fixableDiagnostics">The set of diagnostic IDs to treat as fixable. This value is only used when
        /// <see cref="MarkupHandling"/> is <see cref="MarkupMode.IgnoreFixable"/>.</param>
        /// <param name="defaultPath">The default file path for diagnostics reported in source code.</param>
        /// <returns>A new <see cref="SolutionState"/> with all markup processing completed according to the current
        /// <see cref="MarkupHandling"/>. The <see cref="MarkupHandling"/> of the returned instance is
        /// <see cref="MarkupMode.None"/>.</returns>
        /// <exception cref="InvalidOperationException">If <see cref="InheritanceMode"/> is not
        /// <see cref="StateInheritanceMode.Explicit"/>.</exception>
        public SolutionState WithProcessedMarkup(DiagnosticDescriptor defaultDiagnostic, ImmutableArray<DiagnosticDescriptor> supportedDiagnostics, ImmutableArray<string> fixableDiagnostics, string defaultPath)
        {
            if (InheritanceMode != StateInheritanceMode.Explicit)
            {
                throw new InvalidOperationException("Inheritance processing must complete before markup processing.");
            }

            (var expected, var testSources) = ProcessMarkupSources(Sources, ExpectedDiagnostics, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, defaultPath);
            var (additionalExpected, additionalFiles) = ProcessMarkupSources(AdditionalFiles.Concat(AdditionalFilesFactories.SelectMany(factory => factory())), expected, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, defaultPath);

            var result = new SolutionState(_defaultPrefix, _defaultExtension, MarkupMode.None);
            result.InheritanceMode = StateInheritanceMode.Explicit;
            result.Sources.AddRange(testSources);
            result.AdditionalFiles.AddRange(additionalFiles);
            result.ExpectedDiagnostics.AddRange(additionalExpected);
            return result;
        }

        private (DiagnosticResult[], (string filename, SourceText content)[]) ProcessMarkupSources(
            IEnumerable<(string filename, SourceText content)> sources,
            IEnumerable<DiagnosticResult> explicitDiagnostics,
            DiagnosticDescriptor defaultDiagnostic,
            ImmutableArray<DiagnosticDescriptor> supportedDiagnostics,
            ImmutableArray<string> fixableDiagnostics,
            string defaultPath)
        {
            if (MarkupHandling == MarkupMode.None)
            {
                return (explicitDiagnostics.Select(diagnostic => diagnostic.WithDefaultPath(defaultPath)).ToOrderedArray(), sources.ToArray());
            }

            var sourceFiles = new List<(string filename, SourceText content)>();
            var diagnostics = new List<DiagnosticResult>(explicitDiagnostics.Select(diagnostic => diagnostic.WithDefaultPath(defaultPath)));
            foreach ((var filename, var content) in sources)
            {
                TestFileMarkupParser.GetPositionsAndSpans(content.ToString(), out var output, out var positions, out var namedSpans);
                sourceFiles.Add((filename, content.Replace(new TextSpan(0, content.Length), output)));
                if (positions.Count == 0 && namedSpans.Count == 0)
                {
                    // No markup notation in this input
                    continue;
                }

                if (MarkupHandling == MarkupMode.Ignore)
                {
                    // The source contained markup, which was removed and ignored
                    continue;
                }

                var sourceText = SourceText.From(output, content.Encoding, content.ChecksumAlgorithm);
                foreach (var position in positions)
                {
                    var diagnostic = CreateDiagnosticForPosition(defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, string.Empty, filename, sourceText, position);
                    if (!diagnostic.HasValue)
                    {
                        continue;
                    }

                    diagnostics.Add(diagnostic.Value);
                }

                foreach ((var name, var spans) in namedSpans)
                {
                    foreach (var span in spans)
                    {
                        var diagnostic = CreateDiagnosticForSpan(defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, name, filename, sourceText, span);
                        if (!diagnostic.HasValue)
                        {
                            continue;
                        }

                        diagnostics.Add(diagnostic.Value);
                    }
                }
            }

            return (diagnostics.ToOrderedArray(), sourceFiles.ToArray());
        }

        private DiagnosticResult? CreateDiagnosticForPosition(
            DiagnosticDescriptor defaultDiagnostic,
            ImmutableArray<DiagnosticDescriptor> supportedDiagnostics,
            ImmutableArray<string> fixableDiagnostics,
            string diagnosticId,
            string filename,
            SourceText content,
            int position)
        {
            var diagnosticResult = CreateDiagnostic(defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, diagnosticId);
            if (diagnosticResult == null)
            {
                return null;
            }

            var linePosition = content.Lines.GetLinePosition(position);
            return diagnosticResult.Value.WithLocation(filename, linePosition);
        }

        private DiagnosticResult? CreateDiagnosticForSpan(
            DiagnosticDescriptor defaultDiagnostic,
            ImmutableArray<DiagnosticDescriptor> supportedDiagnostics,
            ImmutableArray<string> fixableDiagnostics,
            string diagnosticId,
            string filename,
            SourceText content,
            TextSpan span)
        {
            var diagnosticResult = CreateDiagnostic(defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, diagnosticId);
            if (diagnosticResult == null)
            {
                return null;
            }

            var linePositionSpan = content.Lines.GetLinePositionSpan(span);
            return diagnosticResult.Value.WithSpan(new FileLinePositionSpan(filename, linePositionSpan));
        }

        private DiagnosticResult? CreateDiagnostic(
            DiagnosticDescriptor defaultDiagnostic,
            ImmutableArray<DiagnosticDescriptor> supportedDiagnostics,
            ImmutableArray<string> fixableDiagnostics,
            string diagnosticId)
        {
            DiagnosticResult diagnosticResult;
            if (string.IsNullOrEmpty(diagnosticId))
            {
                if (defaultDiagnostic is null)
                {
                    throw new InvalidOperationException("Markup syntax can only omit the diagnostic ID if the first analyzer only supports a single diagnostic");
                }

                if (MarkupHandling == MarkupMode.IgnoreFixable && fixableDiagnostics.Contains(defaultDiagnostic.Id))
                {
                    return null;
                }

                diagnosticResult = new DiagnosticResult(defaultDiagnostic);
            }
            else
            {
                if (MarkupHandling == MarkupMode.IgnoreFixable && fixableDiagnostics.Contains(diagnosticId))
                {
                    return null;
                }

                var descriptor = supportedDiagnostics.SingleOrDefault(d => d.Id == diagnosticId);
                if (descriptor != null)
                {
                    diagnosticResult = new DiagnosticResult(descriptor);
                }
                else
                {
                    // This must be a compiler error
                    diagnosticResult = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error);
                }
            }

            return diagnosticResult.WithMessage(null);
        }
    }
}
