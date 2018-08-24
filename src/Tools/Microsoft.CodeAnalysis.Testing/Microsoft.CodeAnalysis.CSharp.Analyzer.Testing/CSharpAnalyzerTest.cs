using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing
{
    public class CSharpAnalyzerTest<TAnalyzer, TVerifier> : AnalyzerTest<TVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TVerifier : IVerifier, new()
    {
        protected override string DefaultFileExt => "cs";
        public override string Language => LanguageNames.CSharp;

        protected override CompilationOptions CreateCompilationOptions()
            => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            => new[] { new TAnalyzer() };
    }
}
