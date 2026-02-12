// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Model;

namespace Microsoft.CodeAnalysis.Testing
{
    public abstract class SourceGeneratorTest<TVerifier> : AnalyzerTest<TVerifier>
        where TVerifier : IVerifier, new()
    {
        protected SourceGeneratorTest()
        {
            IncrementalChangeTestState = new SolutionState(DefaultTestProjectName, Language, DefaultFilePathPrefix, DefaultFileExt);
        }

        public SolutionState IncrementalChangeTestState { get; }

        /// <summary>
        /// Gets the expected state for incremental generator states per generator type.
        /// </summary>
        public Dictionary<Type, IncrementalGeneratorExpectedState> IncrementalGeneratorStepStates { get; } = new Dictionary<Type, IncrementalGeneratorExpectedState>();

        protected override async Task RunImplAsync(CancellationToken cancellationToken)
        {
            var analyzers = GetDiagnosticAnalyzers().ToArray();
            var defaultDiagnostic = GetDefaultDiagnostic(analyzers);
            var supportedDiagnostics = analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics).ToImmutableArray();
            var fixableDiagnostics = ImmutableArray<string>.Empty;
            var rawTestState = TestState.WithInheritedValuesApplied(null, fixableDiagnostics);
            var rawIncrementalState = IncrementalChangeTestState.WithInheritedValuesApplied(null, fixableDiagnostics);

            var testState = rawTestState.WithProcessedMarkup(MarkupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, DefaultFilePath);
            var incrementalTestState = rawIncrementalState.WithProcessedMarkup(MarkupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, DefaultFilePath);

            var diagnostics = await VerifySourceGeneratorAsync(testState, incrementalTestState, Verify, cancellationToken).ConfigureAwait(false);
            await VerifyDiagnosticsAsync(new EvaluatedProjectState(testState, ReferenceAssemblies), testState.AdditionalProjects.Values.Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies)).ToImmutableArray(), testState.ExpectedDiagnostics.ToArray(), Verify, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<Diagnostic>> VerifySourceGeneratorAsync(SolutionState testState, SolutionState incrementalTestState, TVerifier verifier, CancellationToken cancellationToken)
        {
            var sourceGenerators = GetSourceGenerators().ToImmutableArray();
            if (sourceGenerators.IsEmpty)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var driver = await VerifySourceGeneratorAsync(Language, sourceGenerators, testState, verifier.PushContext("Source generator application"), cancellationToken);

            // If any solution transforms for incremental generator testing have been defined,
            // then test incremental generators.
            // We'll also only go down here if incremental generators exist.
            if (incrementalTestState is not null && typeof(CompilationOptions).Assembly.GetType("Microsoft.CodeAnalysis.IIncrementalGenerator") is not null)
            {
                var secondRunProject = await CreateProjectAsync(new EvaluatedProjectState(incrementalTestState, ReferenceAssemblies), incrementalTestState.AdditionalProjects.Values.Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies)).ToImmutableArray(), cancellationToken);

                driver = driver
                        .ReplaceAdditionalTexts(secondRunProject.AnalyzerOptions.AdditionalFiles)
                        .WithUpdatedParseOptions(secondRunProject.ParseOptions!)
                        .WithUpdatedAnalyzerConfigOptions(secondRunProject.AnalyzerOptions.AnalyzerConfigOptionsProvider());

                var (_, secondRunDriver) = await ApplySourceGeneratorsAsync(secondRunProject, driver, verifier, cancellationToken).ConfigureAwait(false);

                var generatorRunResults = secondRunDriver.GetRunResult();

                foreach (var generatorResult in generatorRunResults.Results)
                {
                    if (generatorResult.GeneratorType is not { } generatorType
                        || generatorResult.TrackedSteps is not { } trackedSteps)
                    {
                        continue;
                    }

                    VerifyGeneratorSteps(verifier.PushContext("Verifying source generator incremental step state"), generatorType, trackedSteps);
                }
            }

            return driver.GetRunResult().Diagnostics;

            void VerifyGeneratorSteps(IVerifier verifier, Type generatorType, ImmutableDictionary<string, ImmutableArray<LightupIncrementalGeneratorRunStep>> trackedSteps)
            {
                if (IncrementalGeneratorStepStates.TryGetValue(generatorType, out var expectedState))
                {
                    var expectedStepNames = new HashSet<string>(expectedState.ExpectedStepStates.Keys);

                    expectedStepNames.ExceptWith(trackedSteps.Keys);

                    if (expectedStepNames.Count != 0)
                    {
                        verifier.Fail($"Expected to see steps with the following names, but they were not executed by the '{generatorType.FullName}' generator: {string.Join(", ", expectedStepNames)}");
                    }

                    foreach (var trackedStep in trackedSteps)
                    {
                        if (expectedState.ExpectedStepStates.TryGetValue(trackedStep.Key, out var expectedStepStates))
                        {
                            var trackedStepExecutions = trackedStep.Value;
                            verifier.Equal(expectedStepStates.Count, trackedStepExecutions.Length, $"Expected {expectedStepStates.Count} executions of step '{trackedStep.Key}' but there were only {trackedStepExecutions.Length} executions");
                            for (var i = 0; i < expectedStepStates.Count; i++)
                            {
                                var stepNumber = i + 1;
                                verifier.Equal(expectedStepStates[i].InputRunReasons.Count, trackedStepExecutions[i].Inputs.Length, $"Expected {expectedStepStates[i].InputRunReasons.Count} inputs for the '{trackedStep.Key}' step's {stepNumber.Ordinalize()} execution but there was {"input".ToQuantity(trackedStepExecutions[i].Inputs.Length)}");
                                for (var j = 0; j < expectedStepStates[i].InputRunReasons.Count; j++)
                                {
                                    var inputNumber = j + 1;
                                    var expectedInputState = expectedStepStates[i].InputRunReasons[j];
                                    var actualInputState = trackedStepExecutions[i].Inputs[j].Input.Outputs[trackedStepExecutions[i].Inputs[j].OutputIndex].Reason;
                                    verifier.Equal(expectedInputState, actualInputState, $"Expected the {inputNumber.Ordinalize()} input state for the {stepNumber.Ordinalize()} '{trackedStep.Key}' step to be '{expectedInputState}' but it was '{actualInputState}'");
                                }

                                verifier.Equal(expectedStepStates[i].OutputRunReasons.Count, trackedStepExecutions[i].Outputs.Length, $"Expected {expectedStepStates[i].OutputRunReasons.Count} outputs for the '{trackedStep.Key}' step's {stepNumber.Ordinalize()} execution but there was {"outputs".ToQuantity(trackedStepExecutions[i].Outputs.Length)}");
                                for (var j = 0; j < expectedStepStates[i].OutputRunReasons.Count; j++)
                                {
                                    var outputNumber = j + 1;
                                    var expectedOutputState = expectedStepStates[i].OutputRunReasons[j];
                                    var actualOutputState = trackedStepExecutions[i].Outputs[j].Reason;
                                    verifier.Equal(expectedOutputState, actualOutputState, $"Expected the {outputNumber.Ordinalize()} output state for the {stepNumber.Ordinalize()} '{trackedStep.Key}' step to be '{expectedOutputState}' but it was '{actualOutputState}'");
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            => Enumerable.Empty<DiagnosticAnalyzer>();

        /// <summary>
        /// Returns the source generators being tested - to be implemented in non-abstract class.
        /// </summary>
        /// <returns>The <see cref="ISourceGenerator"/> and/or <see cref="T:Microsoft.CodeAnalysis.IIncrementalGenerator"/> to be used.</returns>
        protected override abstract IEnumerable<Type> GetSourceGenerators();
    }
}
