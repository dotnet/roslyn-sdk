﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Structure that stores information about a <see cref="Diagnostic"/> appearing in a source.
    /// </summary>
    public readonly struct DiagnosticResult
    {
        public static readonly DiagnosticResult[] EmptyDiagnosticResults = { };

        private static readonly object[] EmptyArguments = new object[0];

        private readonly ImmutableArray<DiagnosticLocation> _spans;
        private readonly bool _suppressMessage;
        private readonly string? _message;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticResult"/> structure with the specified
        /// <paramref name="id"/> and <paramref name="severity"/>.
        /// </summary>
        /// <param name="id">The diagnostic ID.</param>
        /// <param name="severity">The diagnostic severity.</param>
        public DiagnosticResult(string id, DiagnosticSeverity severity)
            : this()
        {
            Id = id;
            Severity = severity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticResult"/> structure with the <see cref="Id"/>,
        /// <see cref="Severity"/>, and <see cref="MessageFormat"/> taken from the specified
        /// <paramref name="descriptor"/>.
        /// </summary>
        /// <param name="descriptor">The diagnostic descriptor.</param>
        public DiagnosticResult(DiagnosticDescriptor descriptor)
            : this()
        {
            Id = descriptor.Id;
            Severity = descriptor.DefaultSeverity;
            MessageFormat = descriptor.MessageFormat;
        }

        private DiagnosticResult(
            ImmutableArray<DiagnosticLocation> spans,
            bool suppressMessage,
            string? message,
            DiagnosticSeverity severity,
            DiagnosticOptions options,
            string id,
            LocalizableString? messageFormat,
            object?[]? messageArguments,
            bool? isSuppressed)
        {
            _spans = spans;
            _suppressMessage = suppressMessage;
            _message = message;
            Severity = severity;
            Options = options;
            Id = id;
            MessageFormat = messageFormat;
            MessageArguments = messageArguments;
            IsSuppressed = isSuppressed;
        }

        /// <summary>
        /// Gets the locations where the expected diagnostic is reported.
        /// <list type="bullet">
        /// <item><description>An empty array is returned for no-location diagnostics.</description></item>
        /// <item><description>The first location corresponds to <see cref="Diagnostic.Location"/>.</description></item>
        /// <item><description>Remaining locations correspond to <see cref="Diagnostic.AdditionalLocations"/>. These
        /// locations are not validated if the diagnostic has the
        /// <see cref="DiagnosticOptions.IgnoreAdditionalLocations"/> flag set.</description></item>
        /// </list>
        /// </summary>
        public ImmutableArray<DiagnosticLocation> Spans => _spans.IsDefault ? ImmutableArray<DiagnosticLocation>.Empty : _spans;

        /// <summary>
        /// Gets the expected severity of the diagnostic.
        /// </summary>
        public DiagnosticSeverity Severity { get; }

        /// <summary>
        /// Gets the options to consider during validation of the expected diagnostic. The default value is
        /// <see cref="DiagnosticOptions.None"/>.
        /// </summary>
        public DiagnosticOptions Options { get; }

        /// <summary>
        /// Gets the expected ID of the diagnostic.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the expected message of the diagnostic, if any.
        /// </summary>
        /// <value>
        /// The expected message for the diagnostic; otherwise, <see langword="null"/> if the message should not be
        /// validated.
        /// </value>
        public string? Message
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
                    var messageFormatString = MessageFormat.ToString();

                    // Ensure placeholders in the MessageFormat match the provided arguments
                    int placeholderCount = Regex.Matches(messageFormatString, @"\{[0-9]+(:[^}]*)?\}").Count;

                    // Initialize MessageArguments if null
                    var arguments = MessageArguments ?? EmptyArguments;

                    if (arguments.Length != placeholderCount)
                    {
                        throw new ArgumentException($"Incorrect number of arguments provided. The message expects {placeholderCount} argument(s), but received {arguments.Length}.");
                    }

                    try
                    {
                        return string.Format(messageFormatString, arguments);
                    }
                    catch (FormatException)
                    {
                        return messageFormatString;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the expected message format for the diagnostic.
        /// </summary>
        public LocalizableString? MessageFormat { get; }

        /// <summary>
        /// Gets the expected message arguments for the diagnostic. These arguments are used for formatting
        /// <see cref="MessageFormat"/> when <see cref="Message"/> has not be set directly.
        /// </summary>
        public object?[]? MessageArguments { get; }

        /// <summary>
        /// Gets a value indicating whether the diagnostic is expected to have a location.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the diagnostic is expected to have a location; otherwise, <see langword="false"/>
        /// if a no-location diagnostic is expected.
        /// </value>
        public bool HasLocation => !Spans.IsEmpty;

        /// <summary>
        /// Gets a value indicating whether the diagnostic is expected to be suppressed.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the diagnostic is expected to be suppressed;
        /// <see langword="false"/> if the diagnostic is expected to be not suppressed;
        /// <see langword="null"/> if the suppression state should not be tested;
        /// </value>
        public bool? IsSuppressed { get; }

        /// <summary>
        /// Creates a <see cref="DiagnosticResult"/> for a compiler error with the specified ID.
        /// </summary>
        /// <param name="identifier">The compiler error ID.</param>
        /// <returns>A <see cref="DiagnosticResult"/> for a compiler error with the specified ID.</returns>
        public static DiagnosticResult CompilerError(string identifier)
            => new(identifier, DiagnosticSeverity.Error);

        /// <summary>
        /// Creates a <see cref="DiagnosticResult"/> for a compiler warning with the specified ID.
        /// </summary>
        /// <param name="identifier">The compiler warning ID.</param>
        /// <returns>A <see cref="DiagnosticResult"/> for a compiler warning with the specified ID.</returns>
        public static DiagnosticResult CompilerWarning(string identifier)
            => new(identifier, DiagnosticSeverity.Warning);

        /// <summary>
        /// Transforms the current <see cref="DiagnosticResult"/> to have the specified <see cref="Severity"/>.
        /// </summary>
        /// <param name="severity">The expected diagnostic severity.</param>
        /// <returns>A new <see cref="DiagnosticResult"/> copied from the current instance with the specified
        /// <paramref name="severity"/> applied.</returns>
        public DiagnosticResult WithSeverity(DiagnosticSeverity severity)
        {
            return new DiagnosticResult(
                spans: _spans,
                suppressMessage: _suppressMessage,
                message: _message,
                severity: severity,
                options: Options,
                id: Id,
                messageFormat: MessageFormat,
                messageArguments: MessageArguments,
                isSuppressed: IsSuppressed);
        }

        /// <summary>
        /// Transforms the current <see cref="DiagnosticResult"/> to have the specified <see cref="Options"/>.
        /// </summary>
        /// <param name="options">The options to consider during validation of the expected diagnostic.</param>
        /// <returns>A new <see cref="DiagnosticResult"/> copied from the current instance with the specified
        /// <paramref name="options"/> applied.</returns>
        public DiagnosticResult WithOptions(DiagnosticOptions options)
        {
            return new DiagnosticResult(
                spans: _spans,
                suppressMessage: _suppressMessage,
                message: _message,
                severity: Severity,
                options: options,
                id: Id,
                messageFormat: MessageFormat,
                messageArguments: MessageArguments,
                isSuppressed: IsSuppressed);
        }

        public DiagnosticResult WithArguments(params object[] arguments)
        {
            return new DiagnosticResult(
                spans: _spans,
                suppressMessage: _suppressMessage,
                message: _message,
                severity: Severity,
                options: Options,
                id: Id,
                messageFormat: MessageFormat,
                messageArguments: arguments,
                isSuppressed: IsSuppressed);
        }

        public DiagnosticResult WithMessage(string? message)
        {
            return new DiagnosticResult(
                spans: _spans,
                suppressMessage: message is null,
                message: message,
                severity: Severity,
                options: Options,
                id: Id,
                messageFormat: MessageFormat,
                messageArguments: MessageArguments,
                isSuppressed: IsSuppressed);
        }

        public DiagnosticResult WithMessageFormat(LocalizableString messageFormat)
        {
            return new DiagnosticResult(
                spans: _spans,
                suppressMessage: _suppressMessage,
                message: _message,
                severity: Severity,
                options: Options,
                id: Id,
                messageFormat: messageFormat,
                messageArguments: MessageArguments,
                isSuppressed: IsSuppressed);
        }

        public DiagnosticResult WithIsSuppressed(bool? isSuppressed)
        {
            return new DiagnosticResult(
                spans: _spans,
                suppressMessage: _suppressMessage,
                message: _message,
                severity: Severity,
                options: Options,
                id: Id,
                messageFormat: MessageFormat,
                messageArguments: MessageArguments,
                isSuppressed: isSuppressed);
        }

        public DiagnosticResult WithNoLocation()
        {
            return new DiagnosticResult(
                spans: ImmutableArray<DiagnosticLocation>.Empty,
                suppressMessage: _suppressMessage,
                message: _message,
                severity: Severity,
                options: Options,
                id: Id,
                messageFormat: MessageFormat,
                messageArguments: MessageArguments,
                isSuppressed: IsSuppressed);
        }

        public DiagnosticResult WithLocation(int line, int column)
            => WithLocation(path: string.Empty, new LinePosition(line - 1, column - 1));

        public DiagnosticResult WithLocation(LinePosition location)
            => WithLocation(path: string.Empty, location);

        public DiagnosticResult WithLocation(string path, int line, int column)
            => WithLocation(path, new LinePosition(line - 1, column - 1));

        public DiagnosticResult WithLocation(string path, LinePosition location)
            => AppendSpan(new FileLinePositionSpan(path, location, location), DiagnosticLocationOptions.IgnoreLength);

        public DiagnosticResult WithLocation(string path, LinePosition location, DiagnosticLocationOptions options)
            => AppendSpan(new FileLinePositionSpan(path, location, location), options | DiagnosticLocationOptions.IgnoreLength);

        public DiagnosticResult WithSpan(int startLine, int startColumn, int endLine, int endColumn)
            => WithSpan(path: string.Empty, startLine, startColumn, endLine, endColumn);

        public DiagnosticResult WithSpan(string path, int startLine, int startColumn, int endLine, int endColumn)
            => AppendSpan(new FileLinePositionSpan(path, new LinePosition(startLine - 1, startColumn - 1), new LinePosition(endLine - 1, endColumn - 1)), DiagnosticLocationOptions.None);

        public DiagnosticResult WithSpan(FileLinePositionSpan span)
            => AppendSpan(span, DiagnosticLocationOptions.None);

        public DiagnosticResult WithSpan(FileLinePositionSpan span, DiagnosticLocationOptions options)
            => AppendSpan(span, options);

        public DiagnosticResult WithLocation(int markupKey)
            => AppendSpan(new FileLinePositionSpan(string.Empty, new LinePosition(0, markupKey), new LinePosition(0, markupKey)), DiagnosticLocationOptions.InterpretAsMarkupKey);

        public DiagnosticResult WithLocation(int markupKey, DiagnosticLocationOptions options)
            => AppendSpan(new FileLinePositionSpan(string.Empty, new LinePosition(0, markupKey), new LinePosition(0, markupKey)), options | DiagnosticLocationOptions.InterpretAsMarkupKey);

        public DiagnosticResult WithDefaultPath(string path)
        {
            if (Spans.IsEmpty)
            {
                return this;
            }

            var spans = Spans.ToBuilder();
            for (var i = 0; i < spans.Count; i++)
            {
                if (spans[i].Options.HasFlag(DiagnosticLocationOptions.InterpretAsMarkupKey))
                {
                    // Markup keys have a predefined syntax that requires empty paths.
                    continue;
                }

                if (spans[i].Span.Path == string.Empty)
                {
                    spans[i] = new DiagnosticLocation(new FileLinePositionSpan(path, spans[i].Span.Span), spans[i].Options);
                }
            }

            return new DiagnosticResult(
                spans: spans.MoveToImmutable(),
                suppressMessage: _suppressMessage,
                message: _message,
                severity: Severity,
                options: Options,
                id: Id,
                messageFormat: MessageFormat,
                messageArguments: MessageArguments,
                isSuppressed: IsSuppressed);
        }

        internal DiagnosticResult WithAppliedMarkupLocations(ImmutableDictionary<string, FileLinePositionSpan> markupLocations)
        {
            if (Spans.IsEmpty)
            {
                return this;
            }

            var verifier = new DefaultVerifier();
            var spans = Spans.ToBuilder();
            for (var i = 0; i < spans.Count; i++)
            {
                if (!spans[i].Options.HasFlag(DiagnosticLocationOptions.InterpretAsMarkupKey))
                {
                    continue;
                }

                var index = spans[i].Span.StartLinePosition.Character;
                var expected = new FileLinePositionSpan(path: string.Empty, new LinePosition(0, index), new LinePosition(0, index));
                if (!spans[i].Span.Equals(expected))
                {
                    verifier.Equal(expected, spans[i].Span);
                }

                if (!markupLocations.TryGetValue("#" + index, out var location))
                {
                    throw new InvalidOperationException($"The markup location '#{index}' was not found in the input.");
                }

                spans[i] = new DiagnosticLocation(location, spans[i].Options & ~DiagnosticLocationOptions.InterpretAsMarkupKey);
            }

            return new DiagnosticResult(
                spans: spans.MoveToImmutable(),
                suppressMessage: _suppressMessage,
                message: _message,
                severity: Severity,
                options: Options,
                id: Id,
                messageFormat: MessageFormat,
                messageArguments: MessageArguments,
                isSuppressed: IsSuppressed);
        }

        public DiagnosticResult WithLineOffset(int offset)
        {
            if (Spans.IsEmpty)
            {
                return this;
            }

            var result = this;
            var spansBuilder = result.Spans.ToBuilder();
            for (var i = 0; i < result.Spans.Length; i++)
            {
                var newStartLinePosition = new LinePosition(result.Spans[i].Span.StartLinePosition.Line + offset, result.Spans[i].Span.StartLinePosition.Character);
                var newEndLinePosition = new LinePosition(result.Spans[i].Span.EndLinePosition.Line + offset, result.Spans[i].Span.EndLinePosition.Character);

                spansBuilder[i] = new DiagnosticLocation(new FileLinePositionSpan(result.Spans[i].Span.Path, newStartLinePosition, newEndLinePosition), result.Spans[i].Options);
            }

            return new DiagnosticResult(
                spans: spansBuilder.MoveToImmutable(),
                suppressMessage: _suppressMessage,
                message: _message,
                severity: Severity,
                options: Options,
                id: Id,
                messageFormat: MessageFormat,
                messageArguments: MessageArguments,
                isSuppressed: IsSuppressed);
        }

        private DiagnosticResult AppendSpan(FileLinePositionSpan span, DiagnosticLocationOptions options)
        {
            return new DiagnosticResult(
                spans: Spans.Add(new DiagnosticLocation(span, options)),
                suppressMessage: _suppressMessage,
                message: _message,
                severity: Severity,
                options: Options,
                id: Id,
                messageFormat: MessageFormat,
                messageArguments: MessageArguments,
                isSuppressed: IsSuppressed);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (HasLocation)
            {
                var location = Spans[0];
                builder.Append(location.Span.Path == string.Empty ? "?" : location.Span.Path);
                builder.Append("(");
                builder.Append(location.Span.StartLinePosition.Line + 1);
                builder.Append(",");
                builder.Append(location.Span.StartLinePosition.Character + 1);
                if (!location.Options.HasFlag(DiagnosticLocationOptions.IgnoreLength))
                {
                    builder.Append(",");
                    builder.Append(location.Span.EndLinePosition.Line + 1);
                    builder.Append(",");
                    builder.Append(location.Span.EndLinePosition.Character + 1);
                }

                builder.Append("): ");
            }

            builder.Append(Severity.ToString().ToLowerInvariant());
            builder.Append(" ");
            builder.Append(Id);

            var message = Message;
            if (message != null)
            {
                builder.Append(": ").Append(message);
            }

            return builder.ToString();
        }
    }
}
