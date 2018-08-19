using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Analyzer.Testing
{
    public interface IVerifier
    {
        void Empty<T>(string collectionName, IEnumerable<T> collection);
        void Equal<T>(T expected, T actual, string message = null);
        void False(bool assert, string message = null);
        void LanguageIsSupported(string language);
        void NotEmpty<T>(string collectionName, IEnumerable<T> collection);
        void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message = null);
        void True(bool assert, string message = null);
    }
}