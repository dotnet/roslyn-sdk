// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Structure that stores information about a <see cref="Diagnostic"/> appearing in a source.
    /// </summary>
    public struct DiagnosticResult
    {
        private const string DefaultPath = "Test0.cs";

        private static readonly object[] EmptyArguments = new object[0];

        private ImmutableArray<FileLinePositionSpan> _spans;
        private bool _suppressMessage;
        private string _message;

        public static DiagnosticResult[] EmptyDiagnosticResults { get; } = { };

        public static DiagnosticResult Create<TAnalyzer>(string diagnosticId = null)
            where TAnalyzer : DiagnosticAnalyzer, new()
        {
            var analyzer = new TAnalyzer();
            var supportedDiagnostics = analyzer.SupportedDiagnostics;
            if (diagnosticId is null)
            {
                return new DiagnosticResult(supportedDiagnostics.Single());
            }
            else
            {
                return new DiagnosticResult(supportedDiagnostics.Single(i => i.Id == diagnosticId));
            }
        }

        public static DiagnosticResult CompilerError(string errorIdentifier) => new DiagnosticResult(errorIdentifier, DiagnosticSeverity.Error);

        public DiagnosticResult(string id, DiagnosticSeverity severity)
            : this()
        {
            Id = id;
            Severity = severity;
        }

        public DiagnosticResult(DiagnosticDescriptor descriptor)
            : this()
        {
            Id = descriptor.Id;
            Severity = descriptor.DefaultSeverity;
            MessageFormat = descriptor.MessageFormat;
        }

        public ImmutableArray<FileLinePositionSpan> Spans => _spans.IsDefault ? ImmutableArray<FileLinePositionSpan>.Empty : _spans;

        public DiagnosticSeverity Severity { get; private set; }

        public string Id { get; }

        public string Message
        {
            get
            {
                if (_suppressMessage)
                {
                    return null;
                }

                if (_message != null)
                {
                    return _message;
                }

                if (MessageFormat != null)
                {
                    return string.Format(MessageFormat.ToString(), MessageArguments ?? EmptyArguments);
                }

                return null;
            }
        }

        public LocalizableString MessageFormat { get; private set; }

        public object[] MessageArguments { get; private set; }

        public bool HasLocation => (_spans != default) && (_spans.Length > 0);

        public DiagnosticResult WithSeverity(DiagnosticSeverity severity)
        {
            var result = this;
            result.Severity = severity;
            return result;
        }

        public DiagnosticResult WithArguments(params object[] arguments)
        {
            var result = this;
            result.MessageArguments = arguments;
            return result;
        }

        public DiagnosticResult WithMessage(string message)
        {
            var result = this;
            result._message = message;
            result._suppressMessage = message is null;
            return result;
        }

        public DiagnosticResult WithMessageFormat(LocalizableString messageFormat)
        {
            var result = this;
            result.MessageFormat = messageFormat;
            return result;
        }

        public DiagnosticResult WithLocation(int line, int column)
        {
            return WithLocation(DefaultPath, line, column);
        }

        public DiagnosticResult WithLocation(string path, int line, int column)
        {
            var linePosition = new LinePosition(line, column);
            return AppendSpan(new FileLinePositionSpan(path, linePosition, linePosition));
        }

        public DiagnosticResult WithSpan(int startLine, int startColumn, int endLine, int endColumn)
            => WithSpan(DefaultPath, startLine, startColumn, endLine, endColumn);

        public DiagnosticResult WithSpan(string path, int startLine, int startColumn, int endLine, int endColumn)
            => AppendSpan(new FileLinePositionSpan(path, new LinePosition(startLine, startColumn), new LinePosition(endLine, endColumn)));

        public DiagnosticResult WithLineOffset(int offset)
        {
            var result = this;
            var spansBuilder = result._spans.ToBuilder();
            for (var i = 0; i < result.Spans.Length; i++)
            {
                var newStartLinePosition = new LinePosition(result.Spans[i].StartLinePosition.Line + offset, result.Spans[i].StartLinePosition.Character);
                var newEndLinePosition = new LinePosition(result.Spans[i].EndLinePosition.Line + offset, result.Spans[i].EndLinePosition.Character);

                spansBuilder[i] = new FileLinePositionSpan(result.Spans[i].Path, newStartLinePosition, newEndLinePosition);
            }

            result._spans = spansBuilder.MoveToImmutable();
            return result;
        }

        private DiagnosticResult AppendSpan(FileLinePositionSpan span)
        {
            var result = this;
            result._spans = Spans.Add(span);
            return result;
        }
    }
}
