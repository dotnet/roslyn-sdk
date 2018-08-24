// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Testing.Verifiers
{
    public class MSTestVerifier : IVerifier
    {
        public void Empty<T>(string collectionName, IEnumerable<T> collection)
        {
            Assert.IsFalse(collection?.Any() == true, $"expected '{collectionName}' to be empty, contains '{collection?.Count()}' elements");
        }

        public void Equal<T>(T expected, T actual, string message = null)
        {
            if (message is null)
            {
                Assert.AreEqual(expected, actual);
            }
            else
            {
                Assert.AreEqual(expected, actual, message);
            }
        }

        public void False(bool assert, string message = null)
        {
            if (message is null)
            {
                Assert.IsFalse(assert);
            }
            else
            {
                Assert.IsFalse(assert, message);
            }
        }

        public void LanguageIsSupported(string language)
        {
            Assert.IsFalse(language != LanguageNames.CSharp && language != LanguageNames.VisualBasic, $"Unsupported Language: '{language}'");
        }

        public void NotEmpty<T>(string collectionName, IEnumerable<T> collection)
        {
            Assert.IsTrue(collection?.Any() == false, $"expected '{collectionName}' to be non-empty, contains");
        }

        public void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message = null)
        {
            var comparer = new SequenceEqualEnumerableEqualityComparer<T>();
            var areEqual = comparer.Equals(expected, actual);
            if (!areEqual)
            {
                Assert.Fail(message);
            }
        }

        public void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> equalityComparer, string message = null)
        {
            var comparer = new SequenceEqualEnumerableEqualityComparer<T>(equalityComparer);
            var areEqual = comparer.Equals(expected, actual);
            if (!areEqual)
            {
                Assert.Fail(message);
            }
        }

        public void True(bool assert, string message = null)
        {
            if (message is null)
            {
                Assert.IsTrue(assert);
            }
            else
            {
                Assert.IsTrue(assert, message);
            }
        }

        private sealed class SequenceEqualEnumerableEqualityComparer<T> : IEqualityComparer<IEnumerable<T>>
        {
            private readonly IEqualityComparer<T> _itemEqualityComparer;

            public SequenceEqualEnumerableEqualityComparer()
                : this(default)
            {
            }

            public SequenceEqualEnumerableEqualityComparer(IEqualityComparer<T> itemEqualityComparer)
            {
                _itemEqualityComparer = itemEqualityComparer ?? EqualityComparer<T>.Default;
            }

            public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
            {
                if (ReferenceEquals(x, y)) { return true; }
                if (x is null || y is null) { return false; }

                return x.SequenceEqual(y, _itemEqualityComparer);
            }

            public int GetHashCode(IEnumerable<T> obj)
            {
                // From System.Tuple
                return obj
                    .Select(item => _itemEqualityComparer.GetHashCode(item))
                    .Aggregate(
                        0,
                        (aggHash, nextHash) => ((aggHash << 5) + aggHash) ^ nextHash);
            }
        }
    }
}
