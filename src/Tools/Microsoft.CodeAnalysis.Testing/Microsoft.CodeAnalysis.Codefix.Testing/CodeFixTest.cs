﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private const int DefaultNumberOfIncrementalIterations = -1000;

        /// <summary>
        /// Returns the code fixes being tested - to be implemented in non-abstract class.
        /// </summary>
        /// <returns>The <see cref="CodeFixProvider"/> to be used.</returns>
        protected abstract IEnumerable<CodeFixProvider> GetCodeFixProviders();

        public override async Task RunAsync(CancellationToken cancellationToken = default)
        {
            Verify.NotEmpty(nameof(TestSources), TestSources);

            (var expected, var testSources) = ProcessMarkupSources(TestSources, ExpectedDiagnostics);
            await VerifyDiagnosticsAsync(testSources, expected, cancellationToken).ConfigureAwait(false);
            if (HasFixableDiagnostics())
            {
                (var remainingDiagnostics, var fixedSources) = FixedSources.SequenceEqual(TestSources)
                    ? (expected, testSources)
                    : ProcessMarkupSources(FixedSources, RemainingDiagnostics);

                await VerifyDiagnosticsAsync(fixedSources, remainingDiagnostics, cancellationToken).ConfigureAwait(false);
                if (BatchFixedSources.Any())
                {
                    (var batchRemainingDiagnostics, var batchFixedSources) = BatchFixedSources.SequenceEqual(TestSources)
                        ? (expected, testSources)
                        : ProcessMarkupSources(BatchFixedSources, BatchRemainingDiagnostics);
                    await VerifyDiagnosticsAsync(batchFixedSources, batchRemainingDiagnostics, cancellationToken).ConfigureAwait(false);
                }

                await VerifyFixAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        private bool HasFixableDiagnostics()
        {
            var fixers = GetCodeFixProviders().ToArray();
            if (HasFixableDiagnosticsCore())
            {
                if (FixedSources.Count > 0)
                {
                    return true;
                }

                Verify.Empty(nameof(RemainingDiagnostics), RemainingDiagnostics);
                Verify.Empty(nameof(BatchRemainingDiagnostics), BatchRemainingDiagnostics);
                return false;
            }

            Verify.True(FixedSources.Count == 0 || (FixedSources.Count == 1 && string.IsNullOrEmpty(FixedSources[0].content)), $"'{nameof(FixedSources)}' not equal to '{nameof(TestSources)}'.");
            Verify.SequenceEqual(FixedSources, TestSources, $"'{nameof(FixedSources)}' not equal to '{nameof(TestSources)}'.");

            Verify.True(BatchFixedSources.Count == 0 || (BatchFixedSources.Count == 1 && string.IsNullOrEmpty(BatchFixedSources[0].content)), $"'{nameof(BatchFixedSources)}' not equal to '{nameof(TestSources)}'.");
            Verify.SequenceEqual(FixedSources, TestSources, $"'{nameof(BatchFixedSources)}' not equal to '{nameof(TestSources)}'.");

            Verify.Empty(nameof(RemainingDiagnostics), RemainingDiagnostics);
            Verify.Empty(nameof(BatchRemainingDiagnostics), BatchRemainingDiagnostics);

            return false;

            // Local function
            bool HasFixableDiagnosticsCore()
            {
                return ExpectedDiagnostics.Any(diagnostic => fixers.Any(fixer => fixer.FixableDiagnosticIds.Contains(diagnostic.Id)));
            }
        }

        /// <summary>
        /// Called to test a C# code fix when applied on the input source as a string.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the task will observe.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task VerifyFixAsync(CancellationToken cancellationToken)
        {
            var oldSources = TestSources.ToArray();
            var newSources = FixedSources.ToArray();
            var batchNewSources = BatchFixedSources.Any() ? BatchFixedSources.ToArray() : newSources;

            int numberOfIncrementalIterations;
            int numberOfFixAllIterations;
            if (NumberOfIncrementalIterations != null)
            {
                numberOfIncrementalIterations = NumberOfIncrementalIterations.Value;
            }
            else
            {
                if (!HasAnyChange(oldSources, newSources))
                {
                    numberOfIncrementalIterations = 0;
                }
                else
                {
                    numberOfIncrementalIterations = DefaultNumberOfIncrementalIterations;
                }
            }

            if (NumberOfFixAllIterations != null)
            {
                numberOfFixAllIterations = NumberOfFixAllIterations.Value;
            }
            else
            {
                if (!HasAnyChange(oldSources, batchNewSources))
                {
                    numberOfFixAllIterations = 0;
                }
                else
                {
                    numberOfFixAllIterations = 1;
                }
            }

            var t1 = VerifyFixAsync(Language, GetDiagnosticAnalyzers().ToImmutableArray(), GetCodeFixProviders().ToImmutableArray(), oldSources, newSources, numberOfIncrementalIterations, FixEachAnalyzerDiagnosticAsync, cancellationToken).ConfigureAwait(false);

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

                var t2 = VerifyFixAsync(LanguageNames.CSharp, GetDiagnosticAnalyzers().ToImmutableArray(), GetCodeFixProviders().ToImmutableArray(), oldSources, batchNewSources ?? newSources, numberOfFixAllIterations, FixAllAnalyzerDiagnosticsInDocumentAsync, cancellationToken).ConfigureAwait(false);
                if (Debugger.IsAttached)
                {
                    await t2;
                }

                var t3 = VerifyFixAsync(LanguageNames.CSharp, GetDiagnosticAnalyzers().ToImmutableArray(), GetCodeFixProviders().ToImmutableArray(), oldSources, batchNewSources ?? newSources, numberOfFixAllIterations, FixAllAnalyzerDiagnosticsInProjectAsync, cancellationToken).ConfigureAwait(false);
                if (Debugger.IsAttached)
                {
                    await t3;
                }

                var t4 = VerifyFixAsync(LanguageNames.CSharp, GetDiagnosticAnalyzers().ToImmutableArray(), GetCodeFixProviders().ToImmutableArray(), oldSources, batchNewSources ?? newSources, numberOfFixAllIterations, FixAllAnalyzerDiagnosticsInSolutionAsync, cancellationToken).ConfigureAwait(false);
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
            (string filename, string content)[] oldSources,
            (string filename, string content)[] newSources,
            int numberOfIterations,
            Func<ImmutableArray<DiagnosticAnalyzer>, ImmutableArray<CodeFixProvider>, int?, Project, int, CancellationToken, Task<Project>> getFixedProject,
            CancellationToken cancellationToken)
        {
            var project = CreateProject(oldSources, language);
            var compilerDiagnostics = await GetCompilerDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);

            project = await getFixedProject(analyzers, codeFixProviders, CodeFixIndex, project, numberOfIterations, cancellationToken).ConfigureAwait(false);

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

                throw new Exception(message.ToString());
            }

            // After applying all of the code fixes, compare the resulting string to the inputted one
            var updatedDocuments = project.Documents.ToArray();

            if (newSources.Length != updatedDocuments.Length)
            {
                throw new Exception($"expected '{nameof(newSources)}' and '{nameof(updatedDocuments)}' to be equal but '{nameof(newSources)}' contains '{newSources.Length}' documents and '{nameof(updatedDocuments)}' contains '{updatedDocuments.Length}' documents");
            }

            for (var i = 0; i < updatedDocuments.Length; i++)
            {
                var actual = await GetStringFromDocumentAsync(updatedDocuments[i], cancellationToken).ConfigureAwait(false);
                if (newSources[i].content != actual)
                {
                    throw new Exception($"content of '{newSources[i].filename}' was expected to be '{newSources[i].content}' but was {actual}");
                }

                if (newSources[i].filename != updatedDocuments[i].Name)
                {
                    throw new Exception($"file name was expected to be '{newSources[i].filename}' but was {updatedDocuments[i].Name}");
                }
            }
        }

        private static bool HasAnyChange((string filename, string content)[] oldSources, (string filename, string content)[] newSources)
        {
            return !oldSources.SequenceEqual(newSources);
        }

        private async Task<Project> FixEachAnalyzerDiagnosticAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, Project project, int numberOfIterations, CancellationToken cancellationToken)
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

                if (--numberOfIterations < -1)
                {
                    throw new Exception("The upper limit for the number of code fix iterations was exceeded");
                }

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

                    if (actions.Count > 0)
                    {
                        anyActions = true;

                        var fixedProject = await ApplyFixAsync(project, actions[codeFixIndex.GetValueOrDefault(0)], cancellationToken).ConfigureAwait(false);
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
                if (expectedNumberOfIterations != (expectedNumberOfIterations - numberOfIterations))
                {
                    throw new Exception($"Expected '{expectedNumberOfIterations}' iterations but found '{expectedNumberOfIterations - numberOfIterations}' iterations.");
                }
            }

            return project;
        }

        /// <summary>
        /// Implements a workaround for issue #936, force re-parsing to get the same sort of syntax tree as the original document.
        /// </summary>
        /// <param name="project">The project to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated <see cref="Project"/>.</returns>
        private static async Task<Project> RecreateProjectDocumentsAsync(Project project, CancellationToken cancellationToken)
        {
            foreach (var documentId in project.DocumentIds)
            {
                var document = project.GetDocument(documentId);
                document = await RecreateDocumentAsync(document, cancellationToken).ConfigureAwait(false);
                project = document.Project;
            }

            return project;
        }

        private static async Task<Document> RecreateDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var newText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            newText = newText.WithChanges(new TextChange(new TextSpan(0, 0), " "));
            newText = newText.WithChanges(new TextChange(new TextSpan(0, 1), string.Empty));
            return document.WithText(newText);
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

        private Task<Project> FixAllAnalyzerDiagnosticsInDocumentAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, Project project, int numberOfIterations, CancellationToken cancellationToken)
        {
            return FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope.Document, analyzers, codeFixProviders, codeFixIndex, project, numberOfIterations, cancellationToken);
        }

        private Task<Project> FixAllAnalyzerDiagnosticsInProjectAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, Project project, int numberOfIterations, CancellationToken cancellationToken)
        {
            return FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope.Project, analyzers, codeFixProviders, codeFixIndex, project, numberOfIterations, cancellationToken);
        }

        private Task<Project> FixAllAnalyzerDiagnosticsInSolutionAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, Project project, int numberOfIterations, CancellationToken cancellationToken)
        {
            return FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope.Solution, analyzers, codeFixProviders, codeFixIndex, project, numberOfIterations, cancellationToken);
        }

        private async Task<Project> FixAllAnalyerDiagnosticsInScopeAsync(FixAllScope scope, ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<CodeFixProvider> codeFixProviders, int? codeFixIndex, Project project, int numberOfIterations, CancellationToken cancellationToken)
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
                    if (actions.Count > (codeFixIndex ?? 0))
                    {
                        firstDiagnostic = diagnostic;
                        equivalenceKey = actions[codeFixIndex ?? 0].EquivalenceKey;
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
                if (expectedNumberOfIterations != (expectedNumberOfIterations - numberOfIterations))
                {
                    throw new Exception($"Expected '{expectedNumberOfIterations}' iterations but found '{expectedNumberOfIterations - numberOfIterations}' iterations.");
                }
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
        /// <returns>A string containing the syntax of the <see cref="Document"/> after formatting.</returns>
        private static async Task<string> GetStringFromDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var simplifiedDoc = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            var formatted = await Formatter.FormatAsync(simplifiedDoc, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            var sourceText = await formatted.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return sourceText.ToString();
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
    }
}
