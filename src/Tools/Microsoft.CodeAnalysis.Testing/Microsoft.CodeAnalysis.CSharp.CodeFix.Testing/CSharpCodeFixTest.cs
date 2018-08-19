using System.Collections.Generic;
using Microsoft.CodeAnalysis.Analyzer.Testing;
using Microsoft.CodeAnalysis.Codefix.Testing;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Codefix.Testing
{
    public class CSharpCodeFixTest<TAnalyzer, TCodeFix, TVerifier> : BaseCodeFixTest<TVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix  : CodeFixProvider, new()
        where TVerifier : IVerifier, new()
    {
        protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
            => new[] { new TCodeFix() };

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            => new[] { new TAnalyzer() };

        protected override string DefaultFileExt => "cs";
        public override string Language => LanguageNames.CSharp;

        protected override CompilationOptions CreateCompilationOptions()
            => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);
    }
}
