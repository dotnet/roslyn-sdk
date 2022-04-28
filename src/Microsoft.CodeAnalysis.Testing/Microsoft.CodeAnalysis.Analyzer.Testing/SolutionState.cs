﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    public class SolutionState : ProjectState
    {
        public SolutionState(string name, string language, string defaultPrefix, string defaultExtension)
            : base(name, language, defaultPrefix, defaultExtension)
        {
            AdditionalProjects = new ProjectCollection(language, defaultExtension);
        }

        /// <summary>
        /// Gets or sets a value indicating the manner in which properties are inherited from base test states. When
        /// this property is not set to a specific value, the default varies according to the type of test state:
        /// <list type="bullet">
        /// <item><description>For original (input) sources, the default value is <see cref="StateInheritanceMode.Explicit"/>.</description></item>
        /// <item><description>For fixed (output) sources, the default value is <see cref="StateInheritanceMode.AutoInherit"/>.</description></item>
        /// <item><description>For uncorrected (output) sources, the default value is <see cref="StateInheritanceMode.AutoInheritAll"/>.</description></item>
        /// </list>
        /// </summary>
        public StateInheritanceMode? InheritanceMode { get; set; }

        /// <summary>
        /// Gets a collection of additional projects to include in the solution.
        /// </summary>
        public ProjectCollection AdditionalProjects { get; }

        /// <summary>
        /// Gets the list of diagnostics expected in the source(s) and/or additonal files.
        /// </summary>
        public List<DiagnosticResult> ExpectedDiagnostics { get; } = new List<DiagnosticResult>();

        /// <summary>
        /// Gets or sets a value indicating the manner in which markup syntax is treated within test inputs and outputs.
        /// When this property is not set to a specific value, the default varies according to the type of test state:
        /// <list type="bullet">
        /// <item><description>For original (input) sources, the default value is <see cref="MarkupMode.Allow"/>.</description></item>
        /// <item><description>For fixed (output) sources, the default value is <see cref="MarkupMode.IgnoreFixable"/>.</description></item>
        /// <item><description>For uncorrected (output) sources, the default value is <see cref="MarkupMode.Allow"/>.</description></item>
        /// </list>
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
        public MarkupMode? MarkupHandling { get; set; }

        /// <summary>
        /// Applies the <see cref="InheritanceMode"/> using a specified base state.
        /// </summary>
        /// <remarks>
        /// <para>This method evaluates <see cref="ProjectState.AdditionalFilesFactories"/>, and places the resulting
        /// additional files in the <see cref="ProjectState.AdditionalFiles"/> collection of the result before
        /// returning.</para>
        /// </remarks>
        /// <param name="baseState">The base state to inherit from, or <see langword="null"/> if the current state is
        /// the root state.</param>
        /// <param name="fixableDiagnostics">The set of diagnostic IDs to treat as fixable. Fixable diagnostics present
        /// in the <see cref="ExpectedDiagnostics"/> collection of the base state are only inherited for
        /// <see cref="StateInheritanceMode.AutoInheritAll"/>.</param>
        /// <returns>A new <see cref="SolutionState"/> representing the current state with inherited values applied
        /// where appropriate. The <see cref="InheritanceMode"/> of the result is
        /// <see cref="StateInheritanceMode.Explicit"/>.</returns>
        public SolutionState WithInheritedValuesApplied(SolutionState? baseState, ImmutableArray<string> fixableDiagnostics)
        {
            var inheritanceMode = InheritanceMode;
            var markupHandling = MarkupHandling;
            if (inheritanceMode == null || markupHandling == null)
            {
                if (baseState == null)
                {
                    inheritanceMode = inheritanceMode ?? StateInheritanceMode.Explicit;
                    markupHandling = markupHandling ?? MarkupMode.Allow;
                }
                else if (HasAnyContentChanges(willInherit: inheritanceMode != StateInheritanceMode.Explicit, this, baseState))
                {
                    inheritanceMode = inheritanceMode ?? StateInheritanceMode.AutoInherit;
                    markupHandling = markupHandling ?? MarkupMode.IgnoreFixable;
                }
                else
                {
                    inheritanceMode = inheritanceMode ?? StateInheritanceMode.AutoInheritAll;
                    markupHandling = markupHandling ?? baseState.MarkupHandling ?? MarkupMode.Allow;
                }
            }

            if (inheritanceMode != StateInheritanceMode.AutoInherit
                && inheritanceMode != StateInheritanceMode.Explicit
                && inheritanceMode != StateInheritanceMode.AutoInheritAll)
            {
                throw new InvalidOperationException($"Unexpected inheritance mode: {inheritanceMode}");
            }

            if (baseState?.AdditionalFilesFactories.Count > 0)
            {
                throw new InvalidOperationException("The base state should already have its inheritance state evaluated prior to its use as a base state.");
            }

            var result = new SolutionState(Name, Language, DefaultPrefix, DefaultExtension);

            result.ReferenceAssemblies = ReferenceAssemblies;
            result.OutputKind = OutputKind;
            result.DocumentationMode = DocumentationMode;

            if (inheritanceMode != StateInheritanceMode.Explicit && baseState != null)
            {
                result.ReferenceAssemblies ??= baseState.ReferenceAssemblies;
                result.OutputKind ??= baseState.OutputKind;
                result.DocumentationMode ??= baseState.DocumentationMode;

                if (Sources.Count == 0)
                {
                    result.Sources.AddRange(baseState.Sources);
                }

                if (GeneratedSources.Count == 0)
                {
                    result.GeneratedSources.AddRange(baseState.GeneratedSources);
                }

                if (AdditionalFiles.Count == 0)
                {
                    result.AdditionalFiles.AddRange(baseState.AdditionalFiles);
                }

                if (AnalyzerConfigFiles.Count == 0)
                {
                    result.AnalyzerConfigFiles.AddRange(baseState.AnalyzerConfigFiles);
                }

                if (AdditionalProjects.Count == 0)
                {
                    result.AdditionalProjects.AddRange(baseState.AdditionalProjects);
                }

                if (AdditionalProjectReferences.Count == 0)
                {
                    result.AdditionalProjectReferences.AddRange(baseState.AdditionalProjectReferences);
                }

                if (AdditionalReferences.Count == 0)
                {
                    result.AdditionalReferences.AddRange(baseState.AdditionalReferences);
                }

                if (ExpectedDiagnostics.Count == 0)
                {
                    if (inheritanceMode == StateInheritanceMode.AutoInherit)
                    {
                        result.ExpectedDiagnostics.AddRange(baseState.ExpectedDiagnostics.Where(diagnostic => !fixableDiagnostics.Contains(diagnostic.Id)));
                    }
                    else
                    {
                        result.ExpectedDiagnostics.AddRange(baseState.ExpectedDiagnostics);
                    }
                }
            }

            result.MarkupHandling = markupHandling;
            result.InheritanceMode = StateInheritanceMode.Explicit;
            result.Sources.AddRange(Sources);
            result.GeneratedSources.AddRange(GeneratedSources);
            result.AdditionalFiles.AddRange(AdditionalFiles);
            result.AnalyzerConfigFiles.AddRange(AnalyzerConfigFiles);
            result.AdditionalProjects.AddRange(AdditionalProjects);
            result.AdditionalProjectReferences.AddRange(AdditionalProjectReferences);
            result.AdditionalReferences.AddRange(AdditionalReferences);
            result.ExpectedDiagnostics.AddRange(ExpectedDiagnostics);
            result.AdditionalFiles.AddRange(AdditionalFilesFactories.SelectMany(factory => factory()));
            return result;
        }

        private static bool HasAnyContentChanges(bool willInherit, SolutionState state, SolutionState baseState)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (baseState == null)
            {
                throw new ArgumentNullException(nameof(baseState));
            }

            if ((!willInherit || state.Sources.Any()) && !ContentEqual(state.Sources, baseState.Sources))
            {
                return true;
            }

            if ((!willInherit || state.GeneratedSources.Any()) && !ContentEqual(state.GeneratedSources, baseState.GeneratedSources))
            {
                return true;
            }

            if ((!willInherit || state.AdditionalFiles.Any()) && !ContentEqual(state.AdditionalFiles, baseState.AdditionalFiles))
            {
                return true;
            }

            if ((!willInherit || state.AnalyzerConfigFiles.Any()) && !ContentEqual(state.AnalyzerConfigFiles, baseState.AnalyzerConfigFiles))
            {
                return true;
            }

            if ((!willInherit || state.AdditionalReferences.Any()) && !state.AdditionalReferences.SequenceEqual(baseState.AdditionalReferences))
            {
                return true;
            }

            return false;
        }

        private static bool ContentEqual(SourceFileCollection x, SourceFileCollection y)
        {
            if (x.Count != y.Count)
            {
                return false;
            }

            for (var i = 0; i < x.Count; i++)
            {
                if (x[i].filename != y[i].filename)
                {
                    return false;
                }

                if (!Equals(x[i].content.Encoding, y[i].content.Encoding))
                {
                    return false;
                }

                if (!x[i].content.ContentEquals(y[i].content))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Processes the markup syntax for this <see cref="SolutionState"/> according to the current
        /// <see cref="MarkupHandling"/>, and returns a new <see cref="SolutionState"/> with the
        /// <see cref="ProjectState.Sources"/>, <see cref="ProjectState.GeneratedSources"/>,
        /// <see cref="ProjectState.AdditionalFiles"/>, <see cref="ProjectState.AnalyzerConfigFiles"/>, and
        /// <see cref="ExpectedDiagnostics"/> updated accordingly.
        /// </summary>
        /// <param name="markupOptions">Additional options to apply during markup processing.</param>
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
        public SolutionState WithProcessedMarkup(MarkupOptions markupOptions, DiagnosticDescriptor? defaultDiagnostic, ImmutableArray<DiagnosticDescriptor> supportedDiagnostics, ImmutableArray<string> fixableDiagnostics, string defaultPath)
        {
            if (InheritanceMode != StateInheritanceMode.Explicit)
            {
                throw new InvalidOperationException("Inheritance processing must complete before markup processing.");
            }

            var markupLocations = ImmutableDictionary<string, FileLinePositionSpan>.Empty;
            (var expected, var testSources) = ProcessMarkupSources(Sources, ExpectedDiagnostics, ref markupLocations, markupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, defaultPath);
            var (additionalExpected2, testGeneratedSources) = ProcessMarkupSources(GeneratedSources, expected, ref markupLocations, markupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, defaultPath);
            var (additionalExpected1, additionalFiles) = ProcessMarkupSources(AdditionalFiles.Concat(AdditionalFilesFactories.SelectMany(factory => factory())), additionalExpected2, ref markupLocations, markupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, defaultPath);
            var (additionalExpected, analyzerConfigFiles) = ProcessMarkupSources(AnalyzerConfigFiles, additionalExpected1, ref markupLocations, markupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, defaultPath);

            var result = new SolutionState(Name, Language, DefaultPrefix, DefaultExtension);
            result.MarkupHandling = MarkupMode.None;
            result.InheritanceMode = StateInheritanceMode.Explicit;
            result.ReferenceAssemblies = ReferenceAssemblies;
            result.OutputKind = OutputKind;
            result.DocumentationMode = DocumentationMode;
            result.Sources.AddRange(testSources);
            result.GeneratedSources.AddRange(testGeneratedSources);
            result.AdditionalFiles.AddRange(additionalFiles);
            result.AnalyzerConfigFiles.AddRange(analyzerConfigFiles);

            foreach (var (projectName, projectState) in AdditionalProjects)
            {
                var (correctedIntermediateDiagnostics, additionalProjectSources) = ProcessMarkupSources(projectState.Sources, additionalExpected, ref markupLocations, markupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, defaultPath);
                var (correctedDiagnostics2, additionalProjectGeneratedSources) = ProcessMarkupSources(projectState.GeneratedSources, correctedIntermediateDiagnostics, ref markupLocations, markupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, defaultPath);
                var (correctedDiagnostics1, additionalProjectAdditionalFiles) = ProcessMarkupSources(projectState.AdditionalFiles.Concat(projectState.AdditionalFilesFactories.SelectMany(factory => factory())), correctedDiagnostics2, ref markupLocations, markupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, defaultPath);
                var (correctedDiagnostics, additionalProjectAnalyzerConfigFiles) = ProcessMarkupSources(projectState.AnalyzerConfigFiles, correctedDiagnostics1, ref markupLocations, markupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, defaultPath);

                var processedProjectState = new ProjectState(projectState);
                processedProjectState.Sources.Clear();
                processedProjectState.Sources.AddRange(additionalProjectSources);
                processedProjectState.GeneratedSources.Clear();
                processedProjectState.GeneratedSources.AddRange(additionalProjectGeneratedSources);
                processedProjectState.AdditionalFiles.Clear();
                processedProjectState.AdditionalFilesFactories.Clear();
                processedProjectState.AdditionalFiles.AddRange(additionalProjectAdditionalFiles);
                processedProjectState.AnalyzerConfigFiles.Clear();
                processedProjectState.AnalyzerConfigFiles.AddRange(additionalProjectAnalyzerConfigFiles);

                result.AdditionalProjects.Add(projectName, processedProjectState);
                additionalExpected = correctedDiagnostics;
            }

            for (var i = 0; i < additionalExpected.Length; i++)
            {
                additionalExpected[i] = additionalExpected[i].WithAppliedMarkupLocations(markupLocations);
            }

            result.AdditionalProjectReferences.AddRange(AdditionalProjectReferences);
            result.AdditionalReferences.AddRange(AdditionalReferences);
            result.ExpectedDiagnostics.AddRange(additionalExpected);
            return result;
        }

        private (DiagnosticResult[] expectedDiagnostics, (string filename, SourceText content)[] sources) ProcessMarkupSources(
            IEnumerable<(string filename, SourceText content)> sources,
            IEnumerable<DiagnosticResult> explicitDiagnostics,
            ref ImmutableDictionary<string, FileLinePositionSpan> markupLocations,
            MarkupOptions markupOptions,
            DiagnosticDescriptor? defaultDiagnostic,
            ImmutableArray<DiagnosticDescriptor> supportedDiagnostics,
            ImmutableArray<string> fixableDiagnostics,
            string defaultPath)
        {
            if (MarkupHandling is null)
            {
                throw new InvalidOperationException();
            }

            if (MarkupHandling == MarkupMode.None)
            {
                return (explicitDiagnostics.Select(diagnostic => diagnostic.WithDefaultPath(defaultPath)).ToArray(), sources.ToArray());
            }

            var sourceFiles = new List<(string filename, SourceText content)>();
            var diagnostics = new List<DiagnosticResult>(explicitDiagnostics.Select(diagnostic => diagnostic.WithDefaultPath(defaultPath)));
            foreach ((var filename, var content) in sources)
            {
                TestFileMarkupParser.GetPositionsAndSpans(content.ToString(), out var output, out var positions, out var namedSpans);
                sourceFiles.Add((filename, content.Replace(new TextSpan(0, content.Length), output)));
                if (positions.IsEmpty && namedSpans.IsEmpty)
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
                    var diagnostic = CreateDiagnosticForPosition(markupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, string.Empty, filename, sourceText, position);
                    if (!diagnostic.HasValue)
                    {
                        continue;
                    }

                    diagnostics.Add(diagnostic.Value);
                }

                foreach ((var name, var spans) in namedSpans.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    if (name.StartsWith("#"))
                    {
                        // This is an indexed location. Keep track of it for later processing.
                        if (markupLocations.ContainsKey(name)
                            || spans.Length != 1)
                        {
                            throw new InvalidOperationException($"Input contains multiple markup locations with key '{name}'");
                        }

                        var linePositionSpan = sourceText.Lines.GetLinePositionSpan(spans[0]);
                        markupLocations = markupLocations.Add(name, new FileLinePositionSpan(filename, linePositionSpan));
                        continue;
                    }

                    foreach (var span in spans)
                    {
                        var diagnostic = CreateDiagnosticForSpan(markupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, name, filename, sourceText, span);
                        if (!diagnostic.HasValue)
                        {
                            continue;
                        }

                        diagnostics.Add(diagnostic.Value);
                    }
                }
            }

            return (diagnostics.ToArray(), sourceFiles.ToArray());
        }

        private DiagnosticResult? CreateDiagnosticForPosition(
            MarkupOptions markupOptions,
            DiagnosticDescriptor? defaultDiagnostic,
            ImmutableArray<DiagnosticDescriptor> supportedDiagnostics,
            ImmutableArray<string> fixableDiagnostics,
            string diagnosticId,
            string filename,
            SourceText content,
            int position)
        {
            var diagnosticResult = CreateDiagnostic(markupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, diagnosticId);
            if (diagnosticResult == null)
            {
                return null;
            }

            var linePosition = content.Lines.GetLinePosition(position);
            return diagnosticResult.Value.WithLocation(filename, linePosition);
        }

        private DiagnosticResult? CreateDiagnosticForSpan(
            MarkupOptions markupOptions,
            DiagnosticDescriptor? defaultDiagnostic,
            ImmutableArray<DiagnosticDescriptor> supportedDiagnostics,
            ImmutableArray<string> fixableDiagnostics,
            string diagnosticId,
            string filename,
            SourceText content,
            TextSpan span)
        {
            var diagnosticResult = CreateDiagnostic(markupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, diagnosticId);
            if (diagnosticResult == null)
            {
                return null;
            }

            var linePositionSpan = content.Lines.GetLinePositionSpan(span);
            return diagnosticResult.Value.WithSpan(new FileLinePositionSpan(filename, linePositionSpan));
        }

        private DiagnosticResult? CreateDiagnostic(
            MarkupOptions markupOptions,
            DiagnosticDescriptor? defaultDiagnostic,
            ImmutableArray<DiagnosticDescriptor> supportedDiagnostics,
            ImmutableArray<string> fixableDiagnostics,
            string diagnosticId)
        {
            if (MarkupHandling is null)
            {
                throw new InvalidOperationException();
            }

            DiagnosticResult diagnosticResult;
            if (string.IsNullOrEmpty(diagnosticId))
            {
                if (defaultDiagnostic is null)
                {
                    throw new InvalidOperationException($"Markup syntax can only omit the diagnostic ID if the first analyzer only supports a single diagnostic. To customize the default value, override {nameof(AnalyzerTest<DefaultVerifier>)}<TVerifier>.{nameof(AnalyzerTest<DefaultVerifier>.GetDefaultDiagnostic)} or specify {nameof(MarkupOptions)}.{nameof(MarkupOptions.UseFirstDescriptor)}.");
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

                var descriptors = supportedDiagnostics.Where(d => d.Id == diagnosticId);
                var descriptor = descriptors.FirstOrDefault();
                if (descriptor != null)
                {
                    if (!markupOptions.HasFlag(MarkupOptions.UseFirstDescriptor)
                        && descriptors.Skip(1).Any())
                    {
                        throw new InvalidOperationException($"Multiple diagnostic descriptors with ID {diagnosticId} were found. Use the explicitly diagnostic creation syntax or specify {nameof(MarkupOptions)}.{nameof(MarkupOptions.UseFirstDescriptor)} to use the first matching diagnostic.");
                    }

                    diagnosticResult = new DiagnosticResult(descriptor);
                }
                else
                {
                    // This must be a compiler error
                    diagnosticResult = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error);
                }
            }

            return diagnosticResult.WithMessage(null).WithOptions(DiagnosticOptions.IgnoreAdditionalLocations | DiagnosticOptions.IgnoreSeverity);
        }
    }
}
