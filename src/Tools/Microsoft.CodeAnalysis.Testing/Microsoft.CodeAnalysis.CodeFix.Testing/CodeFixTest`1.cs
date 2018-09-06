// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    public abstract class CodeFixTest<TVerifier> : AnalyzerTest<TVerifier>
        where TVerifier : IVerifier, new()
    {
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
        /// Gets or sets the index of the code fix to apply.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="CodeFixIndex"/> and <see cref="CodeFixEquivalenceKey"/> are both specified, the code fix
        /// test with further verify that the two properties refer to the same code fix.</para>
        /// </remarks>
        /// <seealso cref="CodeFixEquivalenceKey"/>
        public int? CodeFixIndex { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="CodeAction.EquivalenceKey"/> of the code fix to apply.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="CodeFixIndex"/> and <see cref="CodeFixEquivalenceKey"/> are both specified, the code fix
        /// test with further verify that the two properties refer to the same code fix.</para>
        /// </remarks>
        /// <seealso cref="CodeFixIndex"/>
        public string CodeFixEquivalenceKey { get; set; }

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
        /// <see cref="SourceFileList.Add(string)"/> methods.
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
        /// using one of the <see cref="SourceFileList.Add(string)"/> methods.
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
        /// Gets or sets the number of code fix iterations expected during code fix testing for Fix All in Document
        /// scenarios.
        /// </summary>
        /// <remarks>
        /// <para>See the <see cref="NumberOfIncrementalIterations"/> property for an overview of the behavior of this
        /// property. If the number of Fix All in Document iterations is not specified, the value from
        /// <see cref="NumberOfFixAllIterations"/> is used.</para>
        /// </remarks>
        /// <seealso cref="NumberOfIncrementalIterations"/>
        /// <seealso cref="NumberOfFixAllIterations"/>
        /// <seealso href="https://github.com/dotnet/roslyn-sdk/issues/147">#147: Figure out Fix All iteration counts by context</seealso>
        public int? NumberOfFixAllInDocumentIterations { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether new compiler diagnostics are allowed to appear in code fix outputs.
        /// The default value is <see langword="false"/>.
        /// </summary>
        public bool AllowNewCompilerDiagnostics { get; set; } = false;

        /// <summary>
        /// Gets or sets the validation mode for code fixes. The default is
        /// <see cref="CodeFixValidationMode.SemanticStructure"/>.
        /// </summary>
        public CodeFixValidationMode CodeFixValidationMode { get; set; } = CodeFixValidationMode.SemanticStructure;

        protected CodeFixTest()
        {
            FixedSources = new SourceFileList(DefaultFilePathPrefix, DefaultFileExt);
            BatchFixedSources = new SourceFileList(DefaultFilePathPrefix, DefaultFileExt);
        }

        /// <summary>
        /// Returns the code fixes being tested - to be implemented in non-abstract class.
        /// </summary>
        /// <returns>The <see cref="CodeFixProvider"/> to be used.</returns>
        protected abstract IEnumerable<CodeFixProvider> GetCodeFixProviders();

        public override async Task RunAsync(CancellationToken cancellationToken = default)
        {
            Verify.NotEmpty(nameof(TestSources), TestSources);

            (var expected, var testSources) = ProcessMarkupSources(TestSources, ExpectedDiagnostics);
            var (additionalExpected, additionalFiles) = ProcessMarkupSources(AdditionalFiles.Concat(AdditionalFilesFactories.SelectMany(factory => factory())), expected);
            await VerifyDiagnosticsAsync(testSources, additionalFiles, additionalExpected, cancellationToken).ConfigureAwait(false);
            if (HasFixableDiagnostics(additionalExpected))
            {
                (var remainingDiagnostics, var fixedSources) = FixedSources.SequenceEqual(TestSources, SourceFileEqualityComparer.Instance)
                    ? (expected, testSources)
                    : ProcessMarkupSources(FixedSources, RemainingDiagnostics);
                var (additionalRemainingDiagnostics, fixedAdditionalFiles) = FixedAdditionalFiles.Count == 0
                    ? ProcessMarkupSources(AdditionalFiles.Concat(AdditionalFilesFactories.SelectMany(factory => factory())), remainingDiagnostics)
                    : ProcessMarkupSources(FixedAdditionalFiles.Concat(AdditionalFilesFactories.SelectMany(factory => factory())), remainingDiagnostics);

                await VerifyDiagnosticsAsync(fixedSources, fixedAdditionalFiles, additionalRemainingDiagnostics, cancellationToken).ConfigureAwait(false);
                if (BatchFixedSources.Any())
                {
                    (var batchRemainingDiagnostics, var batchFixedSources) = BatchFixedSources.SequenceEqual(TestSources, SourceFileEqualityComparer.Instance)
                        ? (expected, testSources)
                        : ProcessMarkupSources(BatchFixedSources, BatchRemainingDiagnostics);
                    var (additionalBatchRemainingDiagnostics, batchFixedAdditionalFiles) = FixedAdditionalFiles.Count == 0
                        ? ProcessMarkupSources(AdditionalFiles.Concat(AdditionalFilesFactories.SelectMany(factory => factory())), batchRemainingDiagnostics)
                        : ProcessMarkupSources(FixedAdditionalFiles.Concat(AdditionalFilesFactories.SelectMany(factory => factory())), batchRemainingDiagnostics);
                    await VerifyDiagnosticsAsync(batchFixedSources, batchFixedAdditionalFiles, additionalBatchRemainingDiagnostics, cancellationToken).ConfigureAwait(false);
                }

                await VerifyFixAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        private bool HasFixableDiagnostics(DiagnosticResult[] diagnostics)
        {
            var (_, testSources) = ProcessMarkupSources(TestSources, Enumerable.Empty<DiagnosticResult>());
            var (_, fixedSources) = ProcessMarkupSources(FixedSources, Enumerable.Empty<DiagnosticResult>());
            var (_, batchFixedSources) = ProcessMarkupSources(BatchFixedSources, Enumerable.Empty<DiagnosticResult>());

            var fixers = GetCodeFixProviders().ToArray();
            if (HasFixableDiagnosticsCore())
            {
                if (fixedSources.Length > 0)
                {
                    return true;
                }

                Verify.Empty(nameof(RemainingDiagnostics), RemainingDiagnostics);
                Verify.Empty(nameof(BatchRemainingDiagnostics), BatchRemainingDiagnostics);
                return false;
            }

            VerifySourcesConsistentForNoFixableDiagnostics(testSources, fixedSources, nameof(FixedSources));
            VerifySourcesConsistentForNoFixableDiagnostics(testSources, batchFixedSources, nameof(BatchFixedSources));

            Verify.Empty(nameof(RemainingDiagnostics), RemainingDiagnostics);
            Verify.Empty(nameof(BatchRemainingDiagnostics), BatchRemainingDiagnostics);

            return false;

            // Local functions
            bool HasFixableDiagnosticsCore()
            {
                return diagnostics.Any(diagnostic => fixers.Any(fixer => fixer.FixableDiagnosticIds.Contains(diagnostic.Id)));
            }

            void VerifySourcesConsistentForNoFixableDiagnostics(IEnumerable<(string filename, SourceText content)> testSourceList, IEnumerable<(string filename, SourceText content)> fixedSourceList, string fixedSourcesPropertyName)
            {
                if (!fixedSourceList.Any())
                {
                    return;
                }

                Verify.SequenceEqual(testSourceList, fixedSourceList, SourceFileEqualityComparer.Instance, $"'{fixedSourcesPropertyName}' not equal to '{nameof(TestSources)}'.");
            }
        }

        /// <summary>
        /// Called to test a C# code fix when applied on the input source as a string.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task VerifyFixAsync(CancellationToken cancellationToken)
        {
            var (markupDiagnostics, oldSources) = ProcessMarkupSources(TestSources, Enumerable.Empty<DiagnosticResult>());
            var (_, additionalFiles) = ProcessMarkupSources(AdditionalFiles, Enumerable.Empty<DiagnosticResult>());
            var (_, newSources) = ProcessMarkupSources(FixedSources, Enumerable.Empty<DiagnosticResult>());
            var (_, fixedAdditionalFiles) = FixedAdditionalFiles.Any() ? ProcessMarkupSources(FixedAdditionalFiles, Enumerable.Empty<DiagnosticResult>()) : (null, additionalFiles);
            var (_, batchNewSources) = BatchFixedSources.Any() ? ProcessMarkupSources(BatchFixedSources, Enumerable.Empty<DiagnosticResult>()) : (null, newSources);

            int numberOfIncrementalIterations;
            int numberOfFixAllIterations;
            int numberOfFixAllInDocumentIterations;
            if (NumberOfIncrementalIterations != null)
            {
                numberOfIncrementalIterations = NumberOfIncrementalIterations.Value;
            }
            else
            {
                if (!HasAnyChange(oldSources, newSources) && !HasAnyChange(additionalFiles, fixedAdditionalFiles))
                {
                    numberOfIncrementalIterations = 0;
                }
                else
                {
                    // Expect at most one iteration per fixable diagnostic
                    var fixers = GetCodeFixProviders().ToArray();
                    var fixableMarkupDiagnostics = markupDiagnostics.Count(diagnostic => fixers.Any(fixer => fixer.FixableDiagnosticIds.Contains(diagnostic.Id)));
                    var fixableExpectedDiagnostics = ExpectedDiagnostics.Count(diagnostic => fixers.Any(fixer => fixer.FixableDiagnosticIds.Contains(diagnostic.Id)));
                    numberOfIncrementalIterations = -(fixableMarkupDiagnostics + fixableExpectedDiagnostics);
                }
            }

            if (NumberOfFixAllIterations != null)
            {
                numberOfFixAllIterations = NumberOfFixAllIterations.Value;
            }
            else
            {
                if (!HasAnyChange(oldSources, batchNewSources) && !HasAnyChange(additionalFiles, fixedAdditionalFiles))
                {
                    numberOfFixAllIterations = 0;
                }
                else
                {
                    numberOfFixAllIterations = 1;
                }
            }

            if (NumberOfFixAllInDocumentIterations != null)
            {
                numberOfFixAllInDocumentIterations = NumberOfFixAllInDocumentIterations.Value;
            }
            else
            {
                numberOfFixAllInDocumentIterations = numberOfFixAllIterations;
            }

            var t1 = VerifyFixAsync(Language, GetDiagnosticAnalyzers().ToImmutableArray(), GetCodeFixProviders().ToImmutableArray(), oldSources, additionalFiles, newSources, fixedAdditionalFiles, numberOfIncrementalIterations, FixEachAnalyzerDiagnosticAsync, cancellationToken).ConfigureAwait(false);

            var fixAllProvider = GetCodeFixProviders().Select(codeFixProvider => codeFixProvider.GetFixAllProvider()).Where(codeFixProvider => codeFixProvider != null).ToImmutableArray();

            if (fixAllProvider.IsEmpty)
            {
                await t1;
            }
            else
            {
                if (Debugger.IsAttached)
                {
                    await t1;
                }

                var t2 = VerifyFixAsync(Language, GetDiagnosticAnalyzers().ToImmutableArray(), GetCodeFixProviders().ToImmutableArray(), oldSources, additionalFiles, batchNewSources ?? newSources, fixedAdditionalFiles, numberOfFixAllInDocumentIterations, FixAllAnalyzerDiagnosticsInDocumentAsync, cancellationToken).ConfigureAwait(false);
                if (Debugger.IsAttached)
                {
                    await t2;
                }

                var t3 = VerifyFixAsync(Language, GetDiagnosticAnalyzers().ToImmutableArray(), GetCodeFixProviders().ToImmutableArray(), oldSources, additionalFiles, batchNewSources ?? newSources, fixedAdditionalFiles, numberOfFixAllIterations, FixAllAnalyzerDiagnosticsInProjectAsync, cancellationToken).ConfigureAwait(false);
                if (Debugger.IsAttached)
                {
                    await t3;
                }

                var t4 = VerifyFixAsync(Language, GetDiagnosticAnalyzers().ToImmutableArray(), GetCodeFixProviders().ToImmutableArray(), oldSources, additionalFiles, batchNewSources ?? newSources, fixedAdditionalFiles, numberOfFixAllIterations, FixAllAnalyzerDiagnosticsInSolutionAsync, cancellationToken).ConfigureAwait(false);
                if (Debugger.IsAttached)
                {
                    await t4;
                }

                if (!Debugger.IsAttached)
                {
                    // Allow the operations to run in parallel
                    await t1;
                    await t2;
                    await t3;
                    await t4;
                }
            }
        }

        private async Task VerifyFixAsync(
            string language,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            ImmutableArray<CodeFixProvider> codeFixProviders,
            (string filename, SourceText content)[] oldSources,
            (string filename, SourceText content)[] additionalFiles,
            (string filename, SourceText content)[] newSources,
            (string filename, SourceText content)[] fixedAdditionalFiles,
            int numberOfIterations,
            Func<ImmutableArray<DiagnosticAnalyzer>, ImmutableArray<CodeFixProvider>, int?, string, Project, int, CancellationToken, Task<Project>> getFixedProject,
            CancellationToken cancellationToken)
        {
            additionalFiles = additionalFiles.Concat(AdditionalFilesFactories.SelectMany(factory => factory())).ToArray();
            fixedAdditionalFiles = fixedAdditionalFiles.Concat(AdditionalFilesFactories.SelectMany(factory => factory())).ToArray();

            var project = CreateProject(oldSources, additionalFiles, language);
            var compilerDiagnostics = await GetCompilerDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);

            project = await getFixedProject(analyzers, codeFixProviders, CodeFixIndex, CodeFixEquivalenceKey, project, numberOfIterations, cancellationToken).ConfigureAwait(false);

            var newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false));

            // Check if applying the code fix introduced any new compiler diagnostics
            if (!AllowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
            {
                // Format and get the compiler diagnostics again so that the locations make sense in the output
                project = await ReformatProjectDocumentsAsync(project, cancellationToken).ConfigureAwait(false);
                newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false));

                var message = new StringBuilder();
                message.Append("Fix introduced new compiler diagnostics:\r\n");
                newCompilerDiagnostics.Aggregate(message, (sb, d) => sb.Append(d.ToString()).Append("\r\n"));
                foreach (var document in project.Documents)
                {
                    message.Append("\r\n").Append(document.Name).Append(":\r\n");
                    message.Append((await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)).ToFullString());
                    message.Append("\r\n");
                }

                Verify.Fail(message.ToString());
            }

            // After applying all of the code fixes, compare the resulting string to the inputted one
            var updatedDocuments = project.Documents.ToArray();

            Verify.Equal(newSources.Length, updatedDocuments.Length, $"expected '{nameof(newSources)}' and '{nameof(updatedDocuments)}' to be equal but '{nameof(newSources)}' contains '{newSources.Length}' documents and '{nameof(updatedDocuments)}' contains '{updatedDocuments.Length}' documents");

            for (var i = 0; i < updatedDocuments.Length; i++)
            {
                var actual = await GetSourceTextFromDocumentAsync(updatedDocuments[i], cancellationToken).ConfigureAwait(false);
                Verify.Equal(newSources[i].content.ToString(), actual.ToString(), $"content of '{newSources[i].filename}' was expected to be '{newSources[i].content}' but was '{actual}'");
                Verify.Equal(newSources[i].content.Encoding, actual.Encoding, $"encoding of '{newSources[i].filename}' was expected to be '{newSources[i].content.Encoding}' but was '{actual.Encoding}'");
                Verify.Equal(newSources[i].content.ChecksumAlgorithm, actual.ChecksumAlgorithm, $"checksum algorithm of '{newSources[i].filename}' was expected to be '{newSources[i].content.ChecksumAlgorithm}' but was '{actual.ChecksumAlgorithm}'");
                Verify.Equal(newSources[i].filename, updatedDocuments[i].Name, $"file name was expected to be '{newSources[i].filename}' but was '{updatedDocuments[i].Name}'");
            }

            var updatedAdditionalDocuments = project.AdditionalDocuments.ToArray();

            Verify.Equal(fixedAdditionalFiles.Length, updatedAdditionalDocuments.Length, $"expected '{nameof(fixedAdditionalFiles)}' and '{nameof(updatedAdditionalDocuments)}' to be equal but '{nameof(fixedAdditionalFiles)}' contains '{fixedAdditionalFiles.Length}' documents and '{nameof(updatedAdditionalDocuments)}' contains '{updatedAdditionalDocuments.Length}' documents");

            for (var i = 0; i < updatedAdditionalDocuments.Length; i++)
            {
                var actual = await updatedAdditionalDocuments[i].GetTextAsync(cancellationToken).ConfigureAwait(false);
                Verify.Equal(fixedAdditionalFiles[i].content.ToString(), actual.ToString(), $"content of '{fixedAdditionalFiles[i].filename}' was expected to be '{fixedAdditionalFiles[i].content}' but was '{actual}'");
                Verify.Equal(fixedAdditionalFiles[i].content.Encoding, actual.Encoding, $"encoding of '{fixedAdditionalFiles[i].filename}' was expected to be '{fixedAdditionalFiles[i].content.Encoding}' but was '{actual.Encoding}'");
                Verify.Equal(fixedAdditionalFiles[i].content.ChecksumAlgorithm, actual.ChecksumAlgorithm, $"checksum algorithm of '{fixedAdditionalFiles[i].filename}' was expected to be '{fixedAdditionalFiles[i].content.ChecksumAlgorithm}' but was '{actual.ChecksumAlgorithm}'");
                Verify.Equal(fixedAdditionalFiles[i].filename, updatedAdditionalDocuments[i].Name, $"file name was expected to be '{fixedAdditionalFiles[i].filename}' but was '{updatedAdditionalDocuments[i].Name}'");
            }
        }

        private static bool HasAnyChange((string filename, SourceText content)[] oldSources, (string filename, SourceText content)[] newSources)
        {
            return !oldSources.SequenceEqual(newSources, SourceFileEqualityComparer.Instance);
        }

        private async Task<Project> FixEachAnalyzerDiagnosticAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, string codeFixEquivalenceKey, Project project, int numberOfIterations, CancellationToken cancellationToken)
        {
            var codeFixProvider = codeFixProviders.Single();

            var expectedNumberOfIterations = numberOfIterations;
            if (numberOfIterations < 0)
            {
                numberOfIterations = -numberOfIterations;
            }

            var previousDiagnostics = ImmutableArray.Create<Diagnostic>();

            bool done;
            do
            {
                var analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(analyzers, project.Documents.ToArray(), cancellationToken).ConfigureAwait(false);
                if (analyzerDiagnostics.Length == 0)
                {
                    break;
                }

                if (!AreDiagnosticsDifferent(analyzerDiagnostics, previousDiagnostics))
                {
                    break;
                }

                Verify.True(--numberOfIterations >= -1, "The upper limit for the number of code fix iterations was exceeded");

                previousDiagnostics = analyzerDiagnostics;

                done = true;
                var anyActions = false;
                foreach (var diagnostic in analyzerDiagnostics)
                {
                    if (!codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id))
                    {
                        // do not pass unsupported diagnostics to a code fix provider
                        continue;
                    }

                    var actions = new List<CodeAction>();
                    var context = new CodeFixContext(project.GetDocument(diagnostic.Location.SourceTree), diagnostic, (a, d) => actions.Add(a), cancellationToken);
                    await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

                    var actionToApply = TryGetCodeActionToApply(actions, codeFixIndex, codeFixEquivalenceKey);
                    if (actionToApply != null)
                    {
                        anyActions = true;

                        var fixedProject = await ApplyFixAsync(project, actionToApply, cancellationToken).ConfigureAwait(false);
                        if (fixedProject != project)
                        {
                            done = false;

                            project = await RecreateProjectDocumentsAsync(fixedProject, cancellationToken).ConfigureAwait(false);
                            break;
                        }
                    }
                }

                if (!anyActions)
                {
                    Verify.True(done, "Expected to be done executing actions.");

                    // Avoid counting iterations that do not provide any code actions
                    numberOfIterations++;
                }
            }
            while (!done);

            if (expectedNumberOfIterations >= 0)
            {
                Verify.Equal(expectedNumberOfIterations, expectedNumberOfIterations - numberOfIterations, $"Expected '{expectedNumberOfIterations}' iterations but found '{expectedNumberOfIterations - numberOfIterations}' iterations.");
            }
            else
            {
                Verify.True(numberOfIterations >= 0, "The upper limit for the number of code fix iterations was exceeded");
            }

            return project;
        }

        private static CodeAction TryGetCodeActionToApply(List<CodeAction> actions, int? codeFixIndex, string codeFixEquivalenceKey)
        {
            if (codeFixIndex.HasValue && codeFixEquivalenceKey != null)
            {
                if (actions.Count <= codeFixIndex)
                {
                    return null;
                }

                Verify.Equal(
                    codeFixEquivalenceKey,
                    actions[codeFixIndex.Value].EquivalenceKey,
                    "The code action equivalence key and index must be consistent when both are specified.");

                return actions[codeFixIndex.Value];
            }
            else if (codeFixEquivalenceKey != null)
            {
                return actions.Find(x => x.EquivalenceKey == codeFixEquivalenceKey);
            }
            else if (actions.Count > (codeFixIndex ?? 0))
            {
                return actions[codeFixIndex ?? 0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Implements a workaround for issue #936, force re-parsing to get the same sort of syntax tree as the original document.
        /// </summary>
        /// <param name="project">The project to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated <see cref="Project"/>.</returns>
        private async Task<Project> RecreateProjectDocumentsAsync(Project project, CancellationToken cancellationToken)
        {
            foreach (var documentId in project.DocumentIds)
            {
                var document = project.GetDocument(documentId);
                var initialTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                document = await RecreateDocumentAsync(document, cancellationToken).ConfigureAwait(false);
                var recreatedTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                if (CodeFixValidationMode != CodeFixValidationMode.None)
                {
                    // We expect the tree produced by the code fix (initialTree) to match the form of the tree produced
                    // by the compiler for the same text (recreatedTree).
                    TreeEqualityVisitor.AssertNodesEqual(
                        await recreatedTree.GetRootAsync(cancellationToken).ConfigureAwait(false),
                        await initialTree.GetRootAsync(cancellationToken).ConfigureAwait(false),
                        checkTrivia: CodeFixValidationMode == CodeFixValidationMode.Full);
                }

                project = document.Project;
            }

            return project;
        }

        private static async Task<Document> RecreateDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var newText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return document.WithText(SourceText.From(newText.ToString(), newText.Encoding, newText.ChecksumAlgorithm));
        }

        /// <summary>
        /// Apply the inputted <see cref="CodeAction"/> to the inputted document.
        /// Meant to be used to apply code fixes.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> to apply the fix on.</param>
        /// <param name="codeAction">A <see cref="CodeAction"/> that will be applied to the
        /// <paramref name="project"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Project"/> with the changes from the <see cref="CodeAction"/>.</returns>
        private static async Task<Project> ApplyFixAsync(Project project, CodeAction codeAction, CancellationToken cancellationToken)
        {
            var operations = await codeAction.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
            var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
            return solution.GetProject(project.Id);
        }

        private Task<Project> FixAllAnalyzerDiagnosticsInDocumentAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, string codeFixEquivalenceKey, Project project, int numberOfIterations, CancellationToken cancellationToken)
        {
            return FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope.Document, analyzers, codeFixProviders, codeFixIndex, codeFixEquivalenceKey, project, numberOfIterations, cancellationToken);
        }

        private Task<Project> FixAllAnalyzerDiagnosticsInProjectAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, string codeFixEquivalenceKey, Project project, int numberOfIterations, CancellationToken cancellationToken)
        {
            return FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope.Project, analyzers, codeFixProviders, codeFixIndex, codeFixEquivalenceKey, project, numberOfIterations, cancellationToken);
        }

        private Task<Project> FixAllAnalyzerDiagnosticsInSolutionAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, string codeFixEquivalenceKey, Project project, int numberOfIterations, CancellationToken cancellationToken)
        {
            return FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope.Solution, analyzers, codeFixProviders, codeFixIndex, codeFixEquivalenceKey, project, numberOfIterations, cancellationToken);
        }

        private async Task<Project> FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope scope, ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, string codeFixEquivalenceKey, Project project, int numberOfIterations, CancellationToken cancellationToken)
        {
            var codeFixProvider = codeFixProviders.Single();

            var expectedNumberOfIterations = numberOfIterations;
            if (numberOfIterations < 0)
            {
                numberOfIterations = -numberOfIterations;
            }

            var previousDiagnostics = ImmutableArray.Create<Diagnostic>();

            var fixAllProvider = codeFixProvider.GetFixAllProvider();

            if (fixAllProvider == null)
            {
                return null;
            }

            bool done;
            do
            {
                var analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(analyzers, project.Documents.ToArray(), cancellationToken).ConfigureAwait(false);
                if (analyzerDiagnostics.Length == 0)
                {
                    break;
                }

                if (!AreDiagnosticsDifferent(analyzerDiagnostics, previousDiagnostics))
                {
                    break;
                }

                Verify.False(--numberOfIterations < -1, "The upper limit for the number of fix all iterations was exceeded");

                Diagnostic firstDiagnostic = null;
                string equivalenceKey = null;
                foreach (var diagnostic in analyzerDiagnostics)
                {
                    if (!codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id))
                    {
                        // do not pass unsupported diagnostics to a code fix provider
                        continue;
                    }

                    var actions = new List<CodeAction>();
                    var context = new CodeFixContext(project.GetDocument(diagnostic.Location.SourceTree), diagnostic, (a, d) => actions.Add(a), cancellationToken);
                    await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
                    var actionToApply = TryGetCodeActionToApply(actions, codeFixIndex, codeFixEquivalenceKey);
                    if (actionToApply != null)
                    {
                        firstDiagnostic = diagnostic;
                        equivalenceKey = actionToApply.EquivalenceKey;
                        break;
                    }
                }

                if (firstDiagnostic == null)
                {
                    numberOfIterations++;
                    break;
                }

                previousDiagnostics = analyzerDiagnostics;

                done = true;

                FixAllContext.DiagnosticProvider fixAllDiagnosticProvider = TestDiagnosticProvider.Create(analyzerDiagnostics);

                var analyzerDiagnosticIds = analyzers.SelectMany(x => x.SupportedDiagnostics).Select(x => x.Id);
                var compilerDiagnosticIds = codeFixProvider.FixableDiagnosticIds.Where(x => x.StartsWith("CS", StringComparison.Ordinal));
                var disabledDiagnosticIds = project.CompilationOptions.SpecificDiagnosticOptions.Where(x => x.Value == ReportDiagnostic.Suppress).Select(x => x.Key);
                var relevantIds = analyzerDiagnosticIds.Concat(compilerDiagnosticIds).Except(disabledDiagnosticIds).Distinct();
                var fixAllContext = new FixAllContext(project.GetDocument(firstDiagnostic.Location.SourceTree), codeFixProvider, scope, equivalenceKey, relevantIds, fixAllDiagnosticProvider, cancellationToken);

                var action = await fixAllProvider.GetFixAsync(fixAllContext).ConfigureAwait(false);
                if (action == null)
                {
                    return project;
                }

                var fixedProject = await ApplyFixAsync(project, action, cancellationToken).ConfigureAwait(false);
                if (fixedProject != project)
                {
                    done = false;

                    project = await RecreateProjectDocumentsAsync(fixedProject, cancellationToken).ConfigureAwait(false);
                }
            }
            while (!done);

            if (expectedNumberOfIterations >= 0)
            {
                Verify.Equal(expectedNumberOfIterations, expectedNumberOfIterations - numberOfIterations, $"Expected '{expectedNumberOfIterations}' iterations but found '{expectedNumberOfIterations - numberOfIterations}' iterations.");
            }
            else
            {
                Verify.True(numberOfIterations >= 0, "The upper limit for the number of code fix iterations was exceeded");
            }

            return project;
        }

        /// <summary>
        /// Get the existing compiler diagnostics on the input document.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> to run the compiler diagnostic analyzers on.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>The compiler diagnostics that were found in the code.</returns>
        private static async Task<ImmutableArray<Diagnostic>> GetCompilerDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            var allDiagnostics = ImmutableArray.Create<Diagnostic>();

            foreach (var document in project.Documents)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                allDiagnostics = allDiagnostics.AddRange(semanticModel.GetDiagnostics(cancellationToken: cancellationToken));
            }

            return allDiagnostics;
        }

        /// <summary>
        /// Formats the whitespace in all documents of the specified <see cref="Project"/>.
        /// </summary>
        /// <param name="project">The project to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated <see cref="Project"/>.</returns>
        private static async Task<Project> ReformatProjectDocumentsAsync(Project project, CancellationToken cancellationToken)
        {
            foreach (var documentId in project.DocumentIds)
            {
                var document = project.GetDocument(documentId);
                document = await Formatter.FormatAsync(document, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
                project = document.Project;
            }

            return project;
        }

        /// <summary>
        /// Given a document, turn it into a string based on the syntax root.
        /// </summary>
        /// <param name="document">The <see cref="Document"/> to be converted to a string.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="SourceText"/> containing the syntax of the <see cref="Document"/> after formatting.</returns>
        private static async Task<SourceText> GetSourceTextFromDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var simplifiedDoc = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            var formatted = await Formatter.FormatAsync(simplifiedDoc, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            return await formatted.GetTextAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Compare two collections of <see cref="Diagnostic"/>s, and return a list of any new diagnostics that appear
        /// only in the second collection.
        /// <note type="note">
        /// <para>Considers <see cref="Diagnostic"/> to be the same if they have the same <see cref="Diagnostic.Id"/>s.
        /// In the case of multiple diagnostics with the same <see cref="Diagnostic.Id"/> in a row, this method may not
        /// necessarily return the new one.</para>
        /// </note>
        /// </summary>
        /// <param name="diagnostics">The <see cref="Diagnostic"/>s that existed in the code before the code fix was
        /// applied.</param>
        /// <param name="newDiagnostics">The <see cref="Diagnostic"/>s that exist in the code after the code fix was
        /// applied.</param>
        /// <returns>A list of <see cref="Diagnostic"/>s that only surfaced in the code after the code fix was
        /// applied.</returns>
        private static IEnumerable<Diagnostic> GetNewDiagnostics(IEnumerable<Diagnostic> diagnostics, IEnumerable<Diagnostic> newDiagnostics)
        {
            var oldArray = diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
            var newArray = newDiagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();

            var oldIndex = 0;
            var newIndex = 0;

            while (newIndex < newArray.Length)
            {
                if (oldIndex < oldArray.Length && oldArray[oldIndex].Id == newArray[newIndex].Id)
                {
                    ++oldIndex;
                    ++newIndex;
                }
                else
                {
                    yield return newArray[newIndex++];
                }
            }
        }

        private static bool AreDiagnosticsDifferent(ImmutableArray<Diagnostic> analyzerDiagnostics, ImmutableArray<Diagnostic> previousDiagnostics)
        {
            if (analyzerDiagnostics.Length != previousDiagnostics.Length)
            {
                return true;
            }

            for (var i = 0; i < analyzerDiagnostics.Length; i++)
            {
                if ((analyzerDiagnostics[i].Id != previousDiagnostics[i].Id)
                    || (analyzerDiagnostics[i].Location.SourceSpan != previousDiagnostics[i].Location.SourceSpan))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class SourceFileEqualityComparer : IEqualityComparer<(string filename, SourceText content)>
        {
            private SourceFileEqualityComparer()
            {
            }

            public static SourceFileEqualityComparer Instance { get; } = new SourceFileEqualityComparer();

            public bool Equals((string filename, SourceText content) x, (string filename, SourceText content) y)
            {
                if (x.filename != y.filename)
                {
                    return false;
                }

                if (x.content is null || y.content is null)
                {
                    return ReferenceEquals(x, y);
                }

                return x.content.Encoding == y.content.Encoding
                    && x.content.ChecksumAlgorithm == y.content.ChecksumAlgorithm
                    && x.content.ContentEquals(y.content);
            }

            public int GetHashCode((string filename, SourceText content) obj)
            {
                return obj.filename.GetHashCode()
                    ^ (obj.content?.ToString().GetHashCode() ?? 0);
            }
        }

        private class TreeEqualityVisitor
        {
            private readonly SyntaxNode _expected;
            private readonly bool _checkTrivia;

            private TreeEqualityVisitor(SyntaxNode expected, bool checkTrivia)
            {
                _expected = expected ?? throw new ArgumentNullException(nameof(expected));
                _checkTrivia = checkTrivia;
            }

            public void Visit(SyntaxNode node)
            {
                Verify.Equal(_expected.RawKind, node.RawKind);
                AssertChildSyntaxListEqual(_expected.ChildNodesAndTokens(), node.ChildNodesAndTokens(), _checkTrivia);
            }

            internal static void AssertNodesEqual(SyntaxNode expected, SyntaxNode actual, bool checkTrivia)
            {
                new TreeEqualityVisitor(expected, checkTrivia).Visit(actual);
            }

            private static void AssertChildSyntaxListEqual(ChildSyntaxList expected, ChildSyntaxList actual, bool checkTrivia)
            {
                Verify.Equal(expected.Count, actual.Count);
                foreach (var (expectedChild, actualChild) in expected.Zip(actual, (first, second) => (first, second)))
                {
                    if (expectedChild.IsToken)
                    {
                        Verify.True(actualChild.IsToken);
                        AssertTokensEqual(expectedChild.AsToken(), actualChild.AsToken(), checkTrivia);
                    }
                    else
                    {
                        Verify.True(actualChild.IsNode);
                        AssertNodesEqual(expectedChild.AsNode(), actualChild.AsNode(), checkTrivia);
                    }
                }
            }

            private static void AssertTokensEqual(SyntaxToken expected, SyntaxToken actual, bool checkTrivia)
            {
                AssertTriviaListEqual(expected.LeadingTrivia, actual.LeadingTrivia, checkTrivia);
                Verify.Equal(expected.RawKind, actual.RawKind);
                Verify.Equal(expected.Value, actual.Value);
                Verify.Equal(expected.Text, actual.Text);
                Verify.Equal(expected.ValueText, actual.ValueText);
                AssertTriviaListEqual(expected.TrailingTrivia, actual.TrailingTrivia, checkTrivia);
            }

            private static void AssertTriviaListEqual(SyntaxTriviaList expected, SyntaxTriviaList actual, bool checkTrivia)
            {
                if (!checkTrivia)
                {
                    return;
                }

                for (var i = 0; i < Math.Min(expected.Count, actual.Count); i++)
                {
                    AssertTriviaEqual(expected[i], actual[i], checkTrivia);
                }

                Verify.Equal(expected.Count, actual.Count);
            }

            private static void AssertTriviaEqual(SyntaxTrivia expected, SyntaxTrivia actual, bool checkTrivia)
            {
                if (!checkTrivia)
                {
                    return;
                }

                Verify.Equal(expected.RawKind, actual.RawKind);
                Verify.Equal(expected.HasStructure, actual.HasStructure);
                Verify.Equal(expected.IsDirective, actual.IsDirective);
                Verify.Equal(expected.GetAnnotations(), actual.GetAnnotations());
                if (expected.HasStructure)
                {
                    AssertNodesEqual(expected.GetStructure(), actual.GetStructure(), checkTrivia);
                }
            }
        }
    }
}
