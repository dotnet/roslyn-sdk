// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Sample.Analyzers
{
    /// <summary>
    /// Analyzer to demonstrate reading an additional file with a structured format.
    /// It looks for an additional file named "Terms.xml" and dumps it to a stream
    /// so that it can be loaded into an <see cref="XDocument"/>. It then extracts
    /// terms from the XML, detects type names that use those terms and reports
    /// diagnostics on them.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    class XmlAdditionalFileAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Type name contains invalid term";
        private const string MessageFormat = "The term '{0}' is not allowed in a type name.";

        private static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                DiagnosticIds.XmlAdditionalFileAnalyzerRuleId,
                Title,
                MessageFormat,
                DiagnosticCategories.AdditionalFile,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(compilationStartContext =>
            {
                // Find the additional file with the terms.
                var additionalFiles = compilationStartContext.Options.AdditionalFiles;
                var termsFile = additionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).Equals("Terms.xml"));

                if (termsFile != null)
                {
                    var terms = new HashSet<string>();
                    var fileText = termsFile.GetText(compilationStartContext.CancellationToken);

                    // Write the additional file back to a stream.
                    var stream = new MemoryStream();
                    using (var writer = new StreamWriter(stream))
                    {
                        fileText.Write(writer);
                    }

                    // Read all the <Term> elements to get the terms.
                    var document = XDocument.Load(stream);
                    foreach (var termElement in document.Descendants("Term"))
                    {
                        terms.Add(termElement.Value);
                    }

                    // Check every named type for the invalid terms.
                    compilationStartContext.RegisterSymbolAction(symbolAnalysisContext =>
                    {
                        var namedTypeSymbol = (INamedTypeSymbol)symbolAnalysisContext.Symbol;
                        var symbolName = namedTypeSymbol.Name;

                        foreach (var term in terms)
                        {
                            if (symbolName.Contains(term))
                            {
                                symbolAnalysisContext.ReportDiagnostic(
                                    Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], term));
                            }
                        }
                    },
                    SymbolKind.NamedType);
                }
            });
        }
    }
}
