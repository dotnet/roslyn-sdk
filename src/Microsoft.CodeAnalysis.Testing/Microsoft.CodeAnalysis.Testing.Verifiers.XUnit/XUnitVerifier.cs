// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.CodeAnalysis.Testing.Verifiers
{
    public class XUnitVerifier : IVerifier
    {
        private readonly DefaultVerifier _defaultVerifer;

        public XUnitVerifier()
            : this(ImmutableStack<string>.Empty, new DefaultVerifier())
        {
        }

        protected XUnitVerifier(ImmutableStack<string> context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));

            // Construct an equivalent DefaultVerifier from the provided context
            var defaultVerifier = new DefaultVerifier();
            foreach (var frame in context.Reverse())
            {
                defaultVerifier = (DefaultVerifier)defaultVerifier.PushContext(frame);
            }

            _defaultVerifer = defaultVerifier;
        }

        private XUnitVerifier(ImmutableStack<string> context, DefaultVerifier defaultVerifier)
        {
            Context = context;
            _defaultVerifer = defaultVerifier;
        }

        protected ImmutableStack<string> Context { get; }

        public virtual void Empty<T>(string collectionName, IEnumerable<T> collection)
            => _defaultVerifer.Empty(collectionName, collection);

        public virtual void Equal<T>(T expected, T actual, string? message = null)
            => _defaultVerifer.Equal(expected, actual, message);

        public virtual void True([DoesNotReturnIf(false)] bool assert, string? message = null)
            => _defaultVerifer.True(assert, message);

        public virtual void False([DoesNotReturnIf(true)] bool assert, string? message = null)
            => _defaultVerifer.False(assert, message);

        [DoesNotReturn]
        public virtual void Fail(string? message = null)
            => _defaultVerifer.Fail(message);

        public virtual void LanguageIsSupported(string language)
            => _defaultVerifer.LanguageIsSupported(language);

        public virtual void NotEmpty<T>(string collectionName, IEnumerable<T> collection)
            => _defaultVerifer.NotEmpty(collectionName, collection);

        public virtual void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T>? equalityComparer = null, string? message = null)
            => _defaultVerifer.SequenceEqual(expected, actual, equalityComparer, message);

        public virtual IVerifier PushContext(string context)
        {
            if (GetType() != typeof(XUnitVerifier))
            {
                throw new InvalidOperationException($"'{nameof(PushContext)}' must be overridden to support types derived from '{typeof(XUnitVerifier)}'");
            }

            return new XUnitVerifier(Context.Push(context), (DefaultVerifier)_defaultVerifer.PushContext(context));
        }

        protected virtual string CreateMessage(string? message)
        {
            foreach (var frame in Context)
            {
                message = "Context: " + frame + Environment.NewLine + message;
            }

            return message ?? string.Empty;
        }
    }
}
