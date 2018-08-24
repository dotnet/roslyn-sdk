using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    public abstract partial class AnalyzerTest<TVerifier>
        where TVerifier : IVerifier, new()
    {
        protected TVerifier Verify { get; }

        protected virtual string DefaultFilePathPrefix { get; } = "Test";
        protected virtual string DefaultTestProjectName { get; } = "TestProject";
        protected virtual string DefaultFilePath => DefaultFilePathPrefix + 0 + "." + DefaultFileExt;
        protected abstract string DefaultFileExt { get; }

        protected AnalyzerTest()
        {
            Verify = new TVerifier();
            TestSources = new SourceFileList(DefaultFilePathPrefix, DefaultFileExt);
            FixedSources = new SourceFileList(DefaultFilePathPrefix, DefaultFileExt);
            BatchFixedSources = new SourceFileList(DefaultFilePathPrefix, DefaultFileExt);
        }

        /// <summary>
        /// Gets the set of input source files for analyzer or code fix testing. Files may be added to this list using
        /// one of the <see cref="SourceFileList.Add"/> methods.
        /// </summary>
        public SourceFileList TestSources { get; }

        /// <summary>
        /// Gets the language name used for the test.
        /// </summary>
        /// <value>
        /// The language name used for the test.
        /// </value>
        public abstract string Language { get; }

        /// <summary>
        /// Sets the input source file for analyzer or code fix testing.
        /// </summary>
        /// <seealso cref="TestSources"/>
        public string TestCode
        {
            set
            {
                if (value != null)
                {
                    TestSources.Add(value);
                }
            }
        }

        /// <summary>
        /// Gets the collection of inputs to provide to the XML documentation resolver.
        /// </summary>
        /// <remarks>
        /// <para>Files in this collection may be referenced via <c>&lt;include&gt;</c> elements in documentation
        /// comments.</para>
        /// </remarks>
        public Dictionary<string, string> XmlReferences { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets the list of diagnostics expected in the input source(s).
        /// </summary>
        public List<DiagnosticResult> ExpectedDiagnostics { get; } = new List<DiagnosticResult>();

        /// <summary>
        /// Gets the list of diagnostics expected after a code fix is applied.
        /// </summary>
        public List<DiagnosticResult> RemainingDiagnostics { get; } = new List<DiagnosticResult>();

        /// <summary>
        /// Gets the list of diagnostics expected after a Fix All operation.
        /// </summary>
        /// <remarks>
        /// <para>By default, Fix All operations are expected to produce the same result as incremental fix operations.
        /// This collection is only used when <see cref="BatchFixedSources"/> differs from
        /// <see cref="FixedSources"/>.</para>
        /// </remarks>
        public List<DiagnosticResult> BatchRemainingDiagnostics { get; } = new List<DiagnosticResult>();

        /// <summary>
        /// Gets or sets a value indicating whether exclusions for generated code should be tested automatically. The
        /// default value is <see langword="true"/>.
        /// </summary>
        public bool VerifyExclusions { get; set; } = true;

        /// <summary>
        /// Gets a collection of diagnostics to explicitly disable in the <see cref="CompilationOptions"/> for projects.
        /// </summary>
        public List<string> DisabledDiagnostics { get; } = new List<string>();

        /// <summary>
        /// Gets or sets the index of the code fix to apply.
        /// </summary>
        public int? CodeFixIndex { get; set; }

        /// <summary>
        /// Sets the expected output source file for code fix testing.
        /// </summary>
        /// <seealso cref="FixedSources"/>
        public string FixedCode
        {
            set
            {
                if (value != null)
                {
                    FixedSources.Add(value);
                }
            }
        }

        /// <summary>
        /// Gets the set of expected output files for code fix testing. Files may be added to this list using one of the
        /// <see cref="SourceFileList.Add"/> methods.
        /// </summary>
        public SourceFileList FixedSources { get; }

        /// <summary>
        /// Sets the expected output source file after a Fix All operation is applied.
        /// </summary>
        /// <seealso cref="BatchFixedSources"/>
        public string BatchFixedCode
        {
            set
            {
                if (value != null)
                {
                    BatchFixedSources.Add(value);
                }
            }
        }

        /// <summary>
        /// Gets the set of expected output files after a Fix All operation is applied. Files may be added to this list
        /// using one of the <see cref="SourceFileList.Add"/> methods.
        /// </summary>
        /// <remarks>
        /// <para>By default, Fix All operations are expected to produce the same result as incremental fix operations.
        /// If this collection is not specified for the test, <see cref="FixedSources"/> provides the expected test
        /// results for both incremental and Fix All scenarios.</para>
        /// </remarks>
        public SourceFileList BatchFixedSources { get; }

        /// <summary>
        /// Gets or sets the number of code fix iterations expected during code fix testing.
        /// </summary>
        /// <remarks>
        /// <para>Code fixes are applied until one of the following conditions are met:</para>
        ///
        /// <list type="bullet">
        /// <item><description>No diagnostics are reported in the input.</description></item>
        /// <item><description>No code fixes are provided for the diagnostics reported in the input.</description></item>
        /// <item><description>The code fix applied for the diagnostics does not produce a change in the source file(s).</description></item>
        /// <item><description>The maximum number of allowed iterations is exceeded.</description></item>
        /// </list>
        ///
        /// <para>If the number of iterations is positive, it represents an exact number of iterations: code fix tests
        /// will fail if the code fix required more or fewer iterations to complete. If the number of iterations is
        /// negative, the negation of the number of iterations is treated as an upper bound on the number of allowed
        /// iterations: code fix tests will fail only if the code fix required more iterations to complete. If the
        /// number of iterations is zero, the code fix test will validate that no code fixes are offered for the set of
        /// diagnostics reported in the original input.</para>
        ///
        /// <para>When the number of iterations is not specified, the value is automatically selected according to the
        /// current test configuration:</para>
        ///
        /// <list type="bullet">
        /// <item><description>If the expected code fix output equals the input sources, the default value is treated as <c>0</c>.</description></item>
        /// <item><description>Otherwise, the default value is treated as the negative of the number of fixable diagnostics appearing in the input source file(s).</description></item>
        /// </list>
        ///
        /// <note>
        /// <para>The default value for this property can be interpreted as "Iterative code fix operations are expected
        /// to complete in at most one operation for each fixable diagnostic in the input source has been applied.
        /// Completing in fewer iterations is acceptable."</para>
        /// </note>
        /// </remarks>
        public int? NumberOfIncrementalIterations { get; set; }

        /// <summary>
        /// Gets or sets the number of code fix iterations expected during code fix testing for Fix All scenarios.
        /// </summary>
        /// <remarks>
        /// <para>See the <see cref="NumberOfIncrementalIterations"/> property for an overview of the behavior of this
        /// property. If the number of Fix All iterations is not specified, the value is automatically selected
        /// according to the current test configuration:</para>
        ///
        /// <list type="bullet">
        /// <item><description>If the expected Fix All output equals the input sources, the default value is treated as <c>0</c>.</description></item>
        /// <item><description>Otherwise, the default value is treated as <c>1</c>.</description></item>
        /// </list>
        ///
        /// <note>
        /// <para>The default value for this property can be interpreted as "Fix All operations are expected to complete
        /// in the minimum number of iterations possible unless otherwise specified."</para>
        /// </note>
        /// </remarks>
        /// <seealso cref="NumberOfIncrementalIterations"/>
        public int? NumberOfFixAllIterations { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether markup can be used to identify diagnostics within expected inputs
        /// and outputs. The default value is <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// <para>Diagnostics expressed using markup are combined with explicitly-specified expected diagnostics.</para>
        ///
        /// <para>Supported markup syntax includes the following:</para>
        ///
        /// <list type="bullet">
        /// <item><description><c>[|text|]</c>: indicates that a diagnostic is reported for <c>text</c>. The diagnostic
        /// descriptor is located via <see cref="GetDiagnosticAnalyzers"/>. This syntax may only be used when the first
        /// analyzer provided by <see cref="GetDiagnosticAnalyzers"/> supports a single diagnostic.</description></item>
        /// <item><description><c>{|ID1:text|}</c>: indicates that a diagnostic with ID <c>ID1</c> is reported for
        /// <c>text</c>. The diagnostic descriptor for <c>ID1</c> is located via <see cref="GetDiagnosticAnalyzers"/>.
        /// If no matching descriptor is found, the diagnostic is assumed to be a compiler-reported diagnostic with the
        /// specified ID and severity <see cref="DiagnosticSeverity.Error"/>.</description></item>
        /// </list>
        /// </remarks>
        public bool AllowMarkup { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether new compiler diagnostics are allowed to appear in code fix outputs.
        /// The default value is <see langword="false"/>.
        /// </summary>
        public bool AllowNewCompilerDiagnostics { get; set; } = false;

        /// <summary>
        /// Gets a collection of transformation functions to apply to <see cref="Workspace.Options"/> during diagnostic
        /// or code fix test setup.
        /// </summary>
        public List<Func<OptionSet, OptionSet>> OptionsTransforms { get; } = new List<Func<OptionSet, OptionSet>>();

        /// <summary>
        /// Gets a collection of transformation functions to apply to a <see cref="Solution"/> during diagnostic or code
        /// fix test setup.
        /// </summary>
        public List<Func<Solution, ProjectId, Solution>> SolutionTransforms { get; } = new List<Func<Solution, ProjectId, Solution>>();

        public virtual async Task RunAsync(CancellationToken cancellationToken = default)
        {
            Verify.NotEmpty(nameof(TestSources), TestSources);

            (var expected, var testSources) = ProcessMarkupSources(TestSources, ExpectedDiagnostics);
            await VerifyDiagnosticsAsync(testSources, expected, cancellationToken).ConfigureAwait(false);
        }

        protected (DiagnosticResult[], (string filename, string content)[]) ProcessMarkupSources(IEnumerable<(string filename, string content)> sources,
                                                                                               IEnumerable<DiagnosticResult> explicitDiagnostics)
        {
            if (!AllowMarkup)
            {
                return (explicitDiagnostics.ToOrderedArray(), sources.ToArray());
            }

            var analyzers = GetDiagnosticAnalyzers().ToArray();
            var sourceFiles = new List<(string filename, string content)>();
            var diagnostics = new List<DiagnosticResult>(explicitDiagnostics);
            foreach ((var filename, var content) in sources)
            {
                TestFileMarkupParser.GetSpans(content, out var output, out IDictionary<string, IList<TextSpan>> namedSpans);
                sourceFiles.Add((filename, output));
                if (namedSpans.Count == 0)
                {
                    // No markup notation in this input
                    continue;
                }

                var sourceText = SourceText.From(output);
                foreach ((var name, var spans) in namedSpans)
                {
                    foreach (var span in spans)
                    {
                        diagnostics.Add(CreateDiagnosticForSpan(analyzers, name, filename, sourceText, span));
                    }
                }
            }

            return (diagnostics.ToOrderedArray(), sourceFiles.ToArray());
        }

        /// <summary>
        /// General method that gets a collection of actual <see cref="Diagnostic"/>s found in the source after the
        /// analyzer is run, then verifies each of them.
        /// </summary>
        /// <param name="sources">An array of strings to create source documents from to run the analyzers on.</param>
        /// <param name="expected">A collection of <see cref="DiagnosticResult"/>s that should appear after the analyzer
        /// is run on the sources.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task VerifyDiagnosticsAsync((string filename, string content)[] sources, DiagnosticResult[] expected, CancellationToken cancellationToken)
        {
            var analyzers = GetDiagnosticAnalyzers().ToImmutableArray();
            VerifyDiagnosticResults(await GetSortedDiagnosticsAsync(sources, analyzers, cancellationToken).ConfigureAwait(false), analyzers, expected);

            // Automatically test for exclusions
            if (VerifyExclusions)
            {
                // Also check if the analyzer honors exclusions
                if (expected.Any(IsSubjectToExclusion))
                {
                    // Diagnostics reported by the compiler and analyzer diagnostics which don't have a location will
                    // still be reported. We also insert a new line at the beginning so we have to move all diagnostic
                    // locations which have a specific position down by one line.
                    var expectedResults = expected
                        .Where(x => !IsSubjectToExclusion(x))
                        .Select(x => x.WithLineOffset(1))
                        .ToArray();

                    VerifyDiagnosticResults(await GetSortedDiagnosticsAsync(sources.Select(x => (x.filename, " // <auto-generated>\r\n" + x.content)).ToArray(), analyzers, cancellationToken).ConfigureAwait(false), analyzers, expectedResults);
                }
            }
        }

        /// <summary>
        /// Checks each of the actual <see cref="Diagnostic"/>s found and compares them with the corresponding
        /// <see cref="DiagnosticResult"/> in the array of expected results. <see cref="Diagnostic"/>s are considered
        /// equal only if the <see cref="DiagnosticResult.Spans"/>, <see cref="DiagnosticResult.Id"/>,
        /// <see cref="DiagnosticResult.Severity"/>, and <see cref="DiagnosticResult.Message"/> of the
        /// <see cref="DiagnosticResult"/> match the actual <see cref="Diagnostic"/>.
        /// </summary>
        /// <param name="actualResults">The <see cref="Diagnostic"/>s found by the compiler after running the analyzer
        /// on the source code.</param>
        /// <param name="analyzers">The analyzers that have been run on the sources.</param>
        /// <param name="expectedResults">A collection of <see cref="DiagnosticResult"/>s describing the expected
        /// diagnostics for the sources.</param>
        private void VerifyDiagnosticResults(IEnumerable<Diagnostic> actualResults, ImmutableArray<DiagnosticAnalyzer> analyzers, DiagnosticResult[] expectedResults)
        {
            var expectedCount = expectedResults.Length;
            var actualCount = actualResults.Count();

            var diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzers, actualResults.ToArray()) : "    NONE.";
            var message = $"Mismatch between number of diagnostics returned, expected \"{expectedCount}\" actual \"{actualCount}\"\r\n\r\nDiagnostics:\r\n{diagnosticsOutput}\r\n";
            Verify.Equal(expectedCount, actualCount, message);

            for (var i = 0; i < expectedResults.Length; i++)
            {
                var actual = actualResults.ElementAt(i);
                var expected = expectedResults[i];

                if (!expected.HasLocation)
                {
                    Verify.Equal(Location.None, actual.Location, $"Expected:\nA project diagnostic with No location\nActual:\n{FormatDiagnostics(analyzers, actual)}");
                }
                else
                {
                    VerifyDiagnosticLocation(analyzers, actual, actual.Location, expected.Spans[0]);
                    var additionalLocations = actual.AdditionalLocations.ToArray();

                    Verify.Equal(
                        expected.Spans.Length - 1,
                        additionalLocations.Length,
                        $"Expected {expected.Spans.Length - 1} additional locations but got {additionalLocations.Length} for Diagnostic:\r\n    {FormatDiagnostics(analyzers, actual)}\r\n");

                    for (var j = 0; j < additionalLocations.Length; ++j)
                    {
                        VerifyDiagnosticLocation(analyzers, actual, additionalLocations[j], expected.Spans[j + 1]);
                    }
                }

                Verify.Equal(
                    expected.Id,
                    actual.Id,
                    $"Expected diagnostic id to be \"{expected.Id}\" was \"{actual.Id}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzers, actual)}\r\n");

                Verify.Equal(
                    expected.Severity,
                    actual.Severity,
                    $"Expected diagnostic severity to be \"{expected.Severity}\" was \"{actual.Severity}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzers, actual)}\r\n");

                if (expected.Message != null)
                {
                    Verify.Equal(expected.Message, actual.GetMessage(), $"Expected diagnostic message to be \"{expected.Message}\" was \"{ actual.GetMessage()}\"\r\n\r\nDiagnostic:\r\n    { FormatDiagnostics(analyzers, actual)}\r\n");
                }
            }
        }

        /// <summary>
        /// Helper method to <see cref="VerifyDiagnosticResults"/> that checks the location of a
        /// <see cref="Diagnostic"/> and compares it with the location described by a
        /// <see cref="FileLinePositionSpan"/>.
        /// </summary>
        /// <param name="analyzers">The analyzer that have been run on the sources.</param>
        /// <param name="diagnostic">The diagnostic that was found in the code.</param>
        /// <param name="actual">The location of the diagnostic found in the code.</param>
        /// <param name="expected">The <see cref="FileLinePositionSpan"/> describing the expected location of the
        /// diagnostic.</param>
        private void VerifyDiagnosticLocation(ImmutableArray<DiagnosticAnalyzer> analyzers, Diagnostic diagnostic, Location actual, FileLinePositionSpan expected)
        {
            var actualSpan = actual.GetLineSpan();

            var assert = actualSpan.Path == expected.Path || (actualSpan.Path?.Contains("Test0.") == true && expected.Path.Contains("Test."));
            Verify.True(assert, $"Expected diagnostic to be in file \"{expected.Path}\" was actually in file \"{actualSpan.Path}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzers, diagnostic)}\r\n");

            var actualStartLinePosition = actualSpan.StartLinePosition;
            var actualEndLinePosition = actualSpan.EndLinePosition;

            VerifyLinePosition(analyzers, diagnostic, actualSpan.StartLinePosition, expected.StartLinePosition, "start");
            if (expected.StartLinePosition < expected.EndLinePosition)
            {
                VerifyLinePosition(analyzers, diagnostic, actualSpan.EndLinePosition, expected.EndLinePosition, "end");
            }
        }

        private void VerifyLinePosition(ImmutableArray<DiagnosticAnalyzer> analyzers, Diagnostic diagnostic, LinePosition actualLinePosition, LinePosition expectedLinePosition, string positionText)
        {
            // Only check the line position if it matters
            if (expectedLinePosition.Line > 0)
            {
                Verify.Equal(
                    expectedLinePosition.Line,
                    actualLinePosition.Line + 1,
                    $"Expected diagnostic to {positionText} on line \"{expectedLinePosition.Line}\" was actually on line \"{actualLinePosition.Line + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzers, diagnostic)}\r\n");
            }

            // Only check the column position if it matters
            if (expectedLinePosition.Character > 0)
            {
                Verify.Equal(
                    expectedLinePosition.Character,
                    actualLinePosition.Character + 1,
                    $"Expected diagnostic to {positionText} at column \"{expectedLinePosition.Character}\" was actually at column \"{actualLinePosition.Character + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzers, diagnostic)}\r\n");
            }
        }

        /// <summary>
        /// Helper method to format a <see cref="Diagnostic"/> into an easily readable string.
        /// </summary>
        /// <param name="analyzers">The analyzers that this verifier tests.</param>
        /// <param name="diagnostics">A collection of <see cref="Diagnostic"/>s to be formatted.</param>
        /// <returns>The <paramref name="diagnostics"/> formatted as a string.</returns>
        private static string FormatDiagnostics(ImmutableArray<DiagnosticAnalyzer> analyzers, params Diagnostic[] diagnostics)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < diagnostics.Length; ++i)
            {
                var diagnosticsId = diagnostics[i].Id;

                builder.Append("// ").AppendLine(diagnostics[i].ToString());

                var applicableAnalyzer = analyzers.FirstOrDefault(a => a.SupportedDiagnostics.Any(dd => dd.Id == diagnosticsId));
                if (applicableAnalyzer != null)
                {
                    var analyzerType = applicableAnalyzer.GetType();

                    var location = diagnostics[i].Location;
                    if (location == Location.None)
                    {
                        builder.AppendFormat("GetGlobalResult({0}.{1})", analyzerType.Name, diagnosticsId);
                    }
                    else
                    {
                        if (!location.IsInSource)
                        {
                            throw new Exception(
                                $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata:\r\n{diagnostics[i]}");
                        }

                        var resultMethodName = diagnostics[i].Location.SourceTree.FilePath.EndsWith(".cs") ? "GetCSharpResultAt" : "GetBasicResultAt";
                        var linePosition = diagnostics[i].Location.GetLineSpan().StartLinePosition;

                        builder.AppendFormat(
                            "{0}({1}, {2}, {3}.{4})",
                            resultMethodName,
                            linePosition.Line + 1,
                            linePosition.Character + 1,
                            analyzerType.Name,
                            diagnosticsId);
                    }

                    if (i != diagnostics.Length - 1)
                    {
                        builder.Append(',');
                    }

                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private static bool IsSubjectToExclusion(DiagnosticResult result)
        {
            if (result.Id.StartsWith("CS", StringComparison.Ordinal)
                || result.Id.StartsWith("VB", StringComparison.Ordinal))
            {
                // This is a compiler diagnostic
                return false;
            }

            if (result.Id.StartsWith("AD", StringComparison.Ordinal))
            {
                // This diagnostic is reported by the analyzer infrastructure
                return false;
            }

            if (result.Spans.IsEmpty)
            {
                return false;
            }

            return true;
        }

        private DiagnosticResult CreateDiagnosticForSpan(DiagnosticAnalyzer[] analyzers, string diagnosticId, string filename, SourceText content, TextSpan span)
        {
            var linePositionSpan = content.Lines.GetLinePositionSpan(span);

            DiagnosticResult diagnosticResult;
            if (diagnosticId?.Length == 0)
            {
                diagnosticResult = new DiagnosticResult(analyzers[0].SupportedDiagnostics.Single());
            }
            else
            {
                var descriptor = analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics).SingleOrDefault(d => d.Id == diagnosticId);
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

            return diagnosticResult
                .WithMessage(null)
                .WithSpan(
                    filename,
                    linePositionSpan.Start.Line + 1,
                    linePositionSpan.Start.Character + 1,
                    linePositionSpan.End.Line + 1,
                    linePositionSpan.End.Character + 1);
        }

        /// <summary>
        /// Given classes in the form of strings, their language, and an <see cref="DiagnosticAnalyzer"/> to apply to
        /// it, return the <see cref="Diagnostic"/>s found in the string after converting it to a
        /// <see cref="Document"/>.
        /// </summary>
        /// <param name="sources">Classes in the form of strings.</param>
        /// <param name="analyzers">The analyzers to be run on the sources.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A collection of <see cref="Diagnostic"/>s that surfaced in the source code, sorted by
        /// <see cref="Diagnostic.Location"/>.</returns>
        private Task<ImmutableArray<Diagnostic>> GetSortedDiagnosticsAsync((string filename, string content)[] sources, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            return GetSortedDiagnosticsFromDocumentsAsync(analyzers, GetDocuments(sources), cancellationToken);
        }

        /// <summary>
        /// Given an analyzer and a collection of documents to apply it to, run the analyzer and gather an array of
        /// diagnostics found. The returned diagnostics are then ordered by location in the source documents.
        /// </summary>
        /// <param name="analyzers">The analyzer to run on the documents.</param>
        /// <param name="documents">The <see cref="Document"/>s that the analyzer will be run on.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A collection of <see cref="Diagnostic"/>s that surfaced in the source code, sorted by
        /// <see cref="Diagnostic.Location"/>.</returns>
        protected static async Task<ImmutableArray<Diagnostic>> GetSortedDiagnosticsFromDocumentsAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, Document[] documents, CancellationToken cancellationToken)
        {
            var projects = new HashSet<Project>();
            foreach (var document in documents)
            {
                projects.Add(document.Project);
            }

            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            foreach (var project in projects)
            {
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers, project.AnalyzerOptions, cancellationToken);
                var compilerDiagnostics = compilation.GetDiagnostics(cancellationToken);
                var compilerErrors = compilerDiagnostics.Where(i => i.Severity == DiagnosticSeverity.Error);
                var diags = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
                var allDiagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync().ConfigureAwait(false);
                var failureDiagnostics = allDiagnostics.Where(diagnostic => diagnostic.Id == "AD0001");
                foreach (var diag in diags.Concat(compilerErrors).Concat(failureDiagnostics))
                {
                    if (diag.Location == Location.None || diag.Location.IsInMetadata)
                    {
                        diagnostics.Add(diag);
                    }
                    else
                    {
                        for (var i = 0; i < documents.Length; i++)
                        {
                            var document = documents[i];
                            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                            if (tree == diag.Location.SourceTree)
                            {
                                diagnostics.Add(diag);
                            }
                        }
                    }
                }
            }

            var results = SortDistinctDiagnostics(diagnostics);
            return results.ToImmutableArray();
        }

        /// <summary>
        /// Given an array of strings as sources and a language, turn them into a <see cref="Project"/> and return the
        /// documents and spans of it.
        /// </summary>
        /// <param name="sources">Classes in the form of strings.</param>
        /// <returns>A collection of <see cref="Document"/>s representing the sources.</returns>
        private Document[] GetDocuments((string filename, string content)[] sources)
        {
            Verify.LanguageIsSupported(Language);

            var project = CreateProject(sources, Language);
            var documents = project.Documents.ToArray();

            Verify.Equal(sources.Length, documents.Length, "Amount of sources did not match amount of Documents created");

            return documents;
        }

        /// <summary>
        /// Create a project using the input strings as sources.
        /// </summary>
        /// <remarks>
        /// <para>This method first creates a <see cref="Project"/> by calling <see cref="CreateProjectImpl"/>, and then
        /// applies compilation options to the project by calling <see cref="ApplyCompilationOptions"/>.</para>
        /// </remarks>
        /// <param name="sources">Classes in the form of strings.</param>
        /// <param name="language">The language the source classes are in. Values may be taken from the
        /// <see cref="LanguageNames"/> class.</param>
        /// <returns>A <see cref="Project"/> created out of the <see cref="Document"/>s created from the source
        /// strings.</returns>
        protected Project CreateProject((string filename, string content)[] sources, string language = null)
        {
            language = language ?? language;
            var project = CreateProjectImpl(sources, language);
            return ApplyCompilationOptions(project);
        }

        /// <summary>
        /// Create a project using the input strings as sources.
        /// </summary>
        /// <param name="sources">Classes in the form of strings.</param>
        /// <param name="language">The language the source classes are in. Values may be taken from the
        /// <see cref="LanguageNames"/> class.</param>
        /// <returns>A <see cref="Project"/> created out of the <see cref="Document"/>s created from the source
        /// strings.</returns>
        protected virtual Project CreateProjectImpl((string filename, string content)[] sources, string language)
        {
            var fileNamePrefix = DefaultFilePathPrefix;
            var fileExt = DefaultFileExt;

            var projectId = ProjectId.CreateNewId(debugName: DefaultTestProjectName);
            var solution = CreateSolution(projectId, language);

            var count = 0;
            for (var i = 0; i < sources.Length; i++)
            {
                (var newFileName, var source) = sources[i];
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
                count++;
            }

            return solution.GetProject(projectId);
        }

        /// <summary>
        /// Creates a solution that will be used as parent for the sources that need to be checked.
        /// </summary>
        /// <param name="projectId">The project identifier to use.</param>
        /// <param name="language">The language for which the solution is being created.</param>
        /// <returns>The created solution.</returns>
        protected virtual Solution CreateSolution(ProjectId projectId, string language)
        {
            var compilationOptions = CreateCompilationOptions();

            var xmlReferenceResolver = new TestXmlReferenceResolver();
            foreach (var xmlReference in XmlReferences)
            {
                xmlReferenceResolver.XmlReferences.Add(xmlReference.Key, xmlReference.Value);
            }

            compilationOptions = compilationOptions.WithXmlReferenceResolver(xmlReferenceResolver);

            var solution = CreateWorkspace()
                .CurrentSolution
                .AddProject(projectId, DefaultTestProjectName, DefaultTestProjectName, language)
                .WithProjectCompilationOptions(projectId, compilationOptions)
                .AddMetadataReference(projectId, MetadataReferences.CorlibReference)
                .AddMetadataReference(projectId, MetadataReferences.SystemReference)
                .AddMetadataReference(projectId, MetadataReferences.SystemCoreReference)
                .AddMetadataReference(projectId, MetadataReferences.CodeAnalysisReference);

            if (MetadataReferences.SystemRuntimeReference != null)
            {
                solution = solution.AddMetadataReference(projectId, MetadataReferences.SystemRuntimeReference);
            }

            if (MetadataReferences.SystemValueTupleReference != null)
            {
                solution = solution.AddMetadataReference(projectId, MetadataReferences.SystemValueTupleReference);
            }

            foreach (var transform in OptionsTransforms)
            {
                solution.Workspace.Options = transform(solution.Workspace.Options);
            }

            var parseOptions = solution.GetProject(projectId).ParseOptions;
            solution = solution.WithProjectParseOptions(projectId, parseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

            foreach (var transform in SolutionTransforms)
            {
                solution = transform(solution, projectId);
            }

            return solution;
        }

        /// <summary>
        /// Applies compilation options to a project.
        /// </summary>
        /// <remarks>
        /// <para>The default implementation configures the project by enabling all supported diagnostics of analyzers
        /// included in <see cref="GetDiagnosticAnalyzers"/> as well as <c>AD0001</c>. After configuring these
        /// diagnostics, any diagnostic IDs indicated in <see cref="DisabledDiagnostics"/> are explicitly suppressed
        /// using <see cref="ReportDiagnostic.Suppress"/>.</para>
        /// </remarks>
        /// <param name="project">The project.</param>
        /// <returns>The modified project.</returns>
        protected virtual Project ApplyCompilationOptions(Project project)
        {
            var analyzers = GetDiagnosticAnalyzers();

            var supportedDiagnosticsSpecificOptions = new Dictionary<string, ReportDiagnostic>();
            foreach (var analyzer in analyzers)
            {
                foreach (var diagnostic in analyzer.SupportedDiagnostics)
                {
                    // make sure the analyzers we are testing are enabled
                    supportedDiagnosticsSpecificOptions[diagnostic.Id] = ReportDiagnostic.Default;
                }
            }

            // Report exceptions during the analysis process as errors
            supportedDiagnosticsSpecificOptions.Add("AD0001", ReportDiagnostic.Error);

            foreach (var id in DisabledDiagnostics)
            {
                supportedDiagnosticsSpecificOptions[id] = ReportDiagnostic.Suppress;
            }

            // update the project compilation options
            var modifiedSpecificDiagnosticOptions = supportedDiagnosticsSpecificOptions.ToImmutableDictionary().SetItems(project.CompilationOptions.SpecificDiagnosticOptions);
            var modifiedCompilationOptions = project.CompilationOptions.WithSpecificDiagnosticOptions(modifiedSpecificDiagnosticOptions);

            var solution = project.Solution.WithProjectCompilationOptions(project.Id, modifiedCompilationOptions);
            return solution.GetProject(project.Id);
        }

        public virtual AdhocWorkspace CreateWorkspace()
        {
            return new AdhocWorkspace();
        }

        protected abstract CompilationOptions CreateCompilationOptions();

        /// <summary>
        /// Sort <see cref="Diagnostic"/>s by location in source document.
        /// </summary>
        /// <param name="diagnostics">A collection of <see cref="Diagnostic"/>s to be sorted.</param>
        /// <returns>A collection containing the input <paramref name="diagnostics"/>, sorted by
        /// <see cref="Diagnostic.Location"/> and <see cref="Diagnostic.Id"/>.</returns>
        private static Diagnostic[] SortDistinctDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ThenBy(d => d.Location.SourceSpan.End).ThenBy(d => d.Id).ToArray();
        }

        /// <summary>
        /// Gets the analyzers being tested.
        /// </summary>
        /// <returns>
        /// New instances of all the analyzers being tested.
        /// </returns>
        protected abstract IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers();
    }
}
