﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.MSBuild;
using File = System.IO.File;
using Path = System.IO.Path;

namespace AnalyzerRunner
{
    /// <summary>
    /// AnalyzerRunner is a tool that will analyze a solution, find diagnostics in it and will print out the number of
    /// diagnostics it could find. This is useful to easily test performance without having the overhead of visual
    /// studio running.
    /// </summary>
    class Program
    {
        public static async Task Main(string[] args)
        {
            Options options;
            try
            {
                options = Options.Create(args);
            }
            catch (InvalidDataException)
            {
                PrintHelp();
                return;
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress +=
                (sender, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                };

            var analyzers = GetDiagnosticAnalyzers(options.AnalyzerPath);
            analyzers = FilterAnalyzers(analyzers, options).ToImmutableArray();

            if (analyzers.Length == 0)
            {
                WriteLine("No analyzers found", ConsoleColor.Red);
                PrintHelp();
                return;
            }
            var cancellationToken = cts.Token;

            Stopwatch stopwatch = Stopwatch.StartNew();
            using (MSBuildWorkspace workspace = MSBuildWorkspace.Create())
            {
                Solution solution = await workspace.OpenSolutionAsync(options.SolutionPath, cancellationToken).ConfigureAwait(false);
                var projectIds = solution.ProjectIds;

                foreach (var projectId in projectIds)
                {
                    solution = solution.WithProjectAnalyzerReferences(projectId, ImmutableArray<AnalyzerReference>.Empty);
                }

                Console.WriteLine($"Loaded solution in {stopwatch.ElapsedMilliseconds}ms");

                if (options.ShowStats)
                {
                    // TODO The tool support CSharp projects for now. It should support VB/FSharp as well
                    // https://github.com/dotnet/roslyn/issues/23108
                    List<Project> csharpProjects = solution.Projects.Where(project => project.Language == LanguageNames.CSharp).ToList();

                    Console.WriteLine("Number of projects:\t\t" + csharpProjects.Count);
                    Console.WriteLine("Number of documents:\t\t" + csharpProjects.Sum(x => x.DocumentIds.Count));

                    var statistics = GetSolutionStatistics(csharpProjects, cancellationToken);

                    Console.WriteLine("Number of syntax nodes:\t\t" + statistics.NumberofNodes);
                    Console.WriteLine("Number of syntax tokens:\t" + statistics.NumberOfTokens);
                    Console.WriteLine("Number of syntax trivia:\t" + statistics.NumberOfTrivia);
                }

                stopwatch.Restart();

                var analysisResult = await GetAnalysisResultAsync(solution, analyzers, options, cancellationToken).ConfigureAwait(true);
                var allDiagnostics = analysisResult.Where(pair => pair.Value != null).SelectMany(pair => pair.Value.GetAllDiagnostics()).ToImmutableArray();

                Console.WriteLine($"Found {allDiagnostics.Length} diagnostics in {stopwatch.ElapsedMilliseconds}ms");
                WriteTelemetry(analysisResult);

                foreach (var group in allDiagnostics.GroupBy(diagnostic => diagnostic.Id).OrderBy(diagnosticGroup => diagnosticGroup.Key, StringComparer.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"  {group.Key}: {group.Count()} instances");

                    // Print out analyzer diagnostics like AD0001 for analyzer exceptions
                    if (group.Key.StartsWith("AD", StringComparison.Ordinal))
                    {
                        foreach (var item in group)
                        {
                            Console.WriteLine(item);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(options.LogFileName))
                {
                    WriteDiagnosticResults(analysisResult.SelectMany(pair => pair.Value.GetAllDiagnostics().Select(j => Tuple.Create(pair.Key, j))).ToImmutableArray(), options.LogFileName);
                }
            }
        }

        private static void WriteDiagnosticResults(ImmutableArray<Tuple<ProjectId, Diagnostic>> diagnostics, string fileName)
        {
            var orderedDiagnostics =
                diagnostics
                .OrderBy(tuple => tuple.Item2.Id)
                .ThenBy(tuple => tuple.Item2.Location.SourceTree?.FilePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(tuple => tuple.Item2.Location.SourceSpan.Start)
                .ThenBy(tuple => tuple.Item2.Location.SourceSpan.End);

            var uniqueLines = new HashSet<string>();
            StringBuilder completeOutput = new StringBuilder();
            StringBuilder uniqueOutput = new StringBuilder();
            foreach (var diagnostic in orderedDiagnostics)
            {
                string message = diagnostic.Item2.ToString();
                string uniqueMessage = $"{diagnostic.Item1}: {diagnostic.Item2}";
                completeOutput.AppendLine(message);
                if (uniqueLines.Add(uniqueMessage))
                {
                    uniqueOutput.AppendLine(message);
                }
            }

            string directoryName = Path.GetDirectoryName(fileName);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            string uniqueFileName = Path.Combine(directoryName, $"{fileNameWithoutExtension}-Unique{extension}");

            File.WriteAllText(fileName, completeOutput.ToString(), Encoding.UTF8);
            File.WriteAllText(uniqueFileName, uniqueOutput.ToString(), Encoding.UTF8);
        }

        private static Statistic GetSolutionStatistics(IEnumerable<Project> projects, CancellationToken cancellationToken)
        {
            ConcurrentBag<Statistic> sums = new ConcurrentBag<Statistic>();

            Parallel.ForEach(projects.SelectMany(project => project.Documents), document =>
            {
                var documentStatistics = GetSolutionStatisticsAsync(document, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
                sums.Add(documentStatistics);
            });

            Statistic sum = sums.Aggregate(new Statistic(0, 0, 0), (currentResult, value) => currentResult + value);
            return sum;
        }

        // TODO consider removing this and using GetAnalysisResultAsync
        // https://github.com/dotnet/roslyn/issues/23108
        private static async Task<Statistic> GetSolutionStatisticsAsync(Document document, CancellationToken cancellationToken)
        {
            SyntaxTree tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            SyntaxNode root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            var tokensAndNodes = root.DescendantNodesAndTokensAndSelf(descendIntoTrivia: true);

            int numberOfNodes = tokensAndNodes.Count(x => x.IsNode);
            int numberOfTokens = tokensAndNodes.Count(x => x.IsToken);
            int numberOfTrivia = root.DescendantTrivia(descendIntoTrivia: true).Count();

            return new Statistic(numberOfNodes, numberOfTokens, numberOfTrivia);
        }

        private static IEnumerable<DiagnosticAnalyzer> FilterAnalyzers(IEnumerable<DiagnosticAnalyzer> analyzers, Options options)
        {
            foreach (var analyzer in analyzers)
            {
                if (options.UseAll)
                {
                    yield return analyzer;
                }
                else if (options.AnalyzerNames.Count == 0)
                {
                    if (analyzer.SupportedDiagnostics.Any(diagnosticDescriptor => diagnosticDescriptor.IsEnabledByDefault))
                    {
                        yield return analyzer;
                    }
                }
                else if (options.AnalyzerNames.Contains(analyzer.GetType().Name))
                {
                    yield return analyzer;
                }
            }
        }

        private static ImmutableArray<DiagnosticAnalyzer> GetDiagnosticAnalyzers(string path)
        {
            if (File.Exists(path))
            {
                return GetDiagnosticAnalyzersFromFile(path);
            }
            else if (Directory.Exists(path))
            {
                return Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories).SelectMany(file => GetDiagnosticAnalyzersFromFile(file)).ToImmutableArray();
            }

            throw new InvalidDataException($"Cannot find {path}.");
        }

        private static ImmutableArray<DiagnosticAnalyzer> GetDiagnosticAnalyzersFromFile(string path)
        {
            var analyzerReference = new AnalyzerFileReference(path, AssemblyLoader.Instance);
            var analyzers = analyzerReference.GetAnalyzersForAllLanguages();
            return analyzers;
        }

        private static async Task<ImmutableDictionary<ProjectId, AnalysisResult>> GetAnalysisResultAsync(
            Solution solution,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            Options options,
            CancellationToken cancellationToken)
        {
            List<KeyValuePair<ProjectId, Task<AnalysisResult>>> projectDiagnosticTasks = new List<KeyValuePair<ProjectId, Task<AnalysisResult>>>();

            // Make sure we analyze the projects in parallel
            foreach (var project in solution.Projects)
            {
                // TODO The tool support CSharp projects for now. It should support VB/FSharp as well
                // https://github.com/dotnet/roslyn/issues/23108
                if (project.Language != LanguageNames.CSharp)
                {
                    continue;
                }

                projectDiagnosticTasks.Add(new KeyValuePair<ProjectId, Task<AnalysisResult>>(
                    project.Id,
                    GetProjectAnalysisResultAsync(
                        analyzers,
                        project,
                        options,
                        cancellationToken)));
            }

            ImmutableDictionary<ProjectId, AnalysisResult>.Builder projectDiagnosticBuilder = ImmutableDictionary.CreateBuilder<ProjectId, AnalysisResult>();
            foreach (var task in projectDiagnosticTasks)
            {
                projectDiagnosticBuilder.Add(task.Key, await task.Value.ConfigureAwait(false));
            }

            return projectDiagnosticBuilder.ToImmutable();
        }

        /// <summary>
        /// Returns a list of all analysis results inside the specific project. This is an asynchronous operation.
        /// </summary>
        /// <param name="analyzers">The list of analyzers that should be used</param>
        /// <param name="project">The project that should be analyzed
        /// <see langword="false"/> to use the behavior configured for the specified <paramref name="project"/>.</param>
        /// <param name="cancellationToken">The cancellation token that the task will observe.</param>
        /// <returns>A list of analysis results inside the project</returns>
        private static async Task<AnalysisResult> GetProjectAnalysisResultAsync(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            Project project,
            Options analyzerOptionsInternal,
            CancellationToken cancellationToken)
        {
            WriteLine($"Running analyzers for {project.Name}", ConsoleColor.Gray);

            // update the project compilation options
            var modifiedSpecificDiagnosticOptions = project.CompilationOptions.SpecificDiagnosticOptions.Add("AD0001", ReportDiagnostic.Error);
            // Report exceptions during the analysis process as errors
            var modifiedCompilationOptions = project.CompilationOptions.WithSpecificDiagnosticOptions(modifiedSpecificDiagnosticOptions);
            var processedProject = project.WithCompilationOptions(modifiedCompilationOptions);

            try
            {
                Compilation compilation = await processedProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var newCompilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(compilation.SyntaxTrees);

                // TODO some analyzer require additionaltext, we need to think about how to support such analyzer.
                // https://github.com/dotnet/roslyn/issues/23108
                CompilationWithAnalyzers compilationWithAnalyzers = newCompilation.WithAnalyzers(analyzers, new CompilationWithAnalyzersOptions(new AnalyzerOptions(ImmutableArray.Create<AdditionalText>()), null, analyzerOptionsInternal.RunConcurrent, logAnalyzerExecutionTime: true, reportSuppressedDiagnostics: analyzerOptionsInternal.ReportSuppressedDiagnostics));
                var analystResult = await compilationWithAnalyzers.GetAnalysisResultAsync(cancellationToken).ConfigureAwait(false);
                return analystResult;
            }
            catch (Exception e)
            {
                WriteLine($"Failed to analyze {project.Name} with {e.ToString()}", ConsoleColor.Red);
                return null;
            }
        }

        private static void WriteTelemetry(ImmutableDictionary<ProjectId, AnalysisResult> dictionary)
        {
            var telemetryInfoDictionary = new Dictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo>();
            foreach (var analysisResult in dictionary.Values)
            {
                foreach (var pair in analysisResult.AnalyzerTelemetryInfo)
                {
                    if (!telemetryInfoDictionary.TryGetValue(pair.Key, out var telemetry))
                    {
                        telemetryInfoDictionary.Add(pair.Key, pair.Value);
                    }
                    else
                    {
                        telemetry.Add(pair.Value);
                    }
                }
            }

            foreach (var pair in telemetryInfoDictionary)
            {
                WriteTelemetry(pair.Key.GetType().Name, pair.Value);
            }
        }

        private static void WriteTelemetry(string analyzerName, AnalyzerTelemetryInfo telemetry)
        {
            WriteLine($"Statistics for {analyzerName}:", ConsoleColor.DarkCyan);
            WriteLine($"Execution time (ms):            {telemetry.ExecutionTime.TotalMilliseconds}", ConsoleColor.White);

            WriteLine($"Code Block Actions:             {telemetry.CodeBlockActionsCount}", ConsoleColor.White);
            WriteLine($"Code Block Start Actions:       {telemetry.CodeBlockStartActionsCount}", ConsoleColor.White);
            WriteLine($"Code Block End Actions:         {telemetry.CodeBlockEndActionsCount}", ConsoleColor.White);

            WriteLine($"Compilation Actions:            {telemetry.CompilationActionsCount}", ConsoleColor.White);
            WriteLine($"Compilation Start Actions:      {telemetry.CompilationStartActionsCount}", ConsoleColor.White);
            WriteLine($"Compilation End Actions:        {telemetry.CompilationEndActionsCount}", ConsoleColor.White);

            WriteLine($"Operation Actions:              {telemetry.OperationActionsCount}", ConsoleColor.White);
            WriteLine($"Operation Block Actions:        {telemetry.OperationBlockActionsCount}", ConsoleColor.White);
            WriteLine($"Operation Block Start Actions:  {telemetry.OperationBlockStartActionsCount}", ConsoleColor.White);
            WriteLine($"Operation Block End Actions:    {telemetry.OperationBlockEndActionsCount}", ConsoleColor.White);

            WriteLine($"Semantic Model Actions:         {telemetry.SemanticModelActionsCount}", ConsoleColor.White);
            WriteLine($"Symbol Actions:                 {telemetry.SymbolActionsCount}", ConsoleColor.White);
            WriteLine($"Syntax Node Actions:            {telemetry.SyntaxNodeActionsCount}", ConsoleColor.White);
            WriteLine($"Syntax Tree Actions:            {telemetry.SyntaxTreeActionsCount}", ConsoleColor.White);
        }

        internal static void WriteLine(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        internal static void PrintHelp()
        {
            Console.WriteLine("Usage: AnalyzerRunner <AnalyzerAssemblyOrFolder> <Solution> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("/all                 Run all analyzers, including ones that are disabled by default");
            Console.WriteLine("/stats               Display statistics of the solution");
            Console.WriteLine("/a <analyzer name>   Enable analyzer with <analyzer name> (when this is specified, only analyzers specificed are enabled. Use: /a <name1> /a <name2>, etc.");
            Console.WriteLine("/concurrent          Executes analyzers in concurrent mode");
            Console.WriteLine("/suppressed          Reports suppressed diagnostics");
            Console.WriteLine("/log <logFile>       Write logs into the log file specified");
        }
    }
}
