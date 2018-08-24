using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Testing
{
    public class DefaultVerifier : IVerifier
    {
        public void Empty<T>(string collectionName, IEnumerable<T> collection)
        {
            if (collection?.Any() == true)
            {
                throw new InvalidOperationException($"'{collectionName}' is not empty");
            }
        }

        public void NotEmpty<T>(string collectionName, IEnumerable<T> collection)
        {
            if (collection?.Any() == false)
            {
                throw new InvalidOperationException($"'{collectionName}' is empty");
            }
        }

        public void LanguageIsSupported(string language)
        {
            if (language != LanguageNames.CSharp && language != LanguageNames.VisualBasic)
            {
                throw new InvalidOperationException($"Unsupported Language: '{language}'");
            }
        }

        public void Equal<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(message ?? $"items not equal.  expected:'{expected}' actual:'{actual}'");
            }
        }

        public void True(bool assert, string message = null)
        {
            if (!assert)
            {
                throw new InvalidOperationException(message ?? $"Expected value to be 'true' but was 'false'");
            }
        }

        public void False(bool assert, string message = null)
        {
            if (assert)
            {
                throw new InvalidOperationException(message ?? $"Expected value to be 'false' but was 'true'");
            }
        }

        public void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message = null)
        {
            var comparer = new SequenceEqualEnumerableEqualityComparer<T>();
            var areEqual = comparer.Equals(expected, actual);
            if (!areEqual)
            {
                throw new InvalidOperationException(message ?? $"Sequences are not equal");
            }
        }

        private sealed class SequenceEqualEnumerableEqualityComparer<T> : IEqualityComparer<IEnumerable<T>>
        {
            private readonly IEqualityComparer<T> _itemEqualityComparer;

            public SequenceEqualEnumerableEqualityComparer()
                : this(EqualityComparer<T>.Default)
            {
            }

            public SequenceEqualEnumerableEqualityComparer(IEqualityComparer<T> itemEqualityComparer)
            {
                _itemEqualityComparer = itemEqualityComparer;
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
