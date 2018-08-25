// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Structure that stores information about a <see cref="Diagnostic"/> appearing in a source.
    /// </summary>
    public struct DiagnosticResult
    {
        private static readonly object[] EmptyArguments = new object[0];

        private ImmutableArray<FileLinePositionSpan> _spans;
        private bool _suppressMessage;
        private string _message;

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
            => WithLocation(path: string.Empty, new LinePosition(line, column));

        public DiagnosticResult WithLocation(LinePosition location)
            => WithLocation(path: string.Empty, location);

        public DiagnosticResult WithLocation(string path, int line, int column)
            => WithLocation(path, new LinePosition(line, column));

        public DiagnosticResult WithLocation(string path, LinePosition location)
            => AppendSpan(new FileLinePositionSpan(path, location, location));

        public DiagnosticResult WithSpan(int startLine, int startColumn, int endLine, int endColumn)
            => WithSpan(path: string.Empty, startLine, startColumn, endLine, endColumn);

        public DiagnosticResult WithSpan(string path, int startLine, int startColumn, int endLine, int endColumn)
            => AppendSpan(new FileLinePositionSpan(path, new LinePosition(startLine, startColumn), new LinePosition(endLine, endColumn)));

        public DiagnosticResult WithSpan(FileLinePositionSpan span)
            => AppendSpan(span);

        public DiagnosticResult WithDefaultPath(string path)
        {
            var result = this;
            var spans = _spans.ToBuilder();
            for (var i = 0; i < spans.Count; i++)
            {
                if (spans[i].Path == string.Empty)
                {
                    spans[i] = new FileLinePositionSpan(path, spans[i].Span);
                }
            }

            result._spans = spans.MoveToImmutable();
            return result;
        }

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
