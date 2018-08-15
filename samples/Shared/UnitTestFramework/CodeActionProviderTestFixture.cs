﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Roslyn.UnitTestFramework
{
    public abstract class CodeActionProviderTestFixture
    {
        protected Document CreateDocument(string code)
        {
            string fileExtension = LanguageName == LanguageNames.CSharp ? ".cs" : ".vb";

            ProjectId projectId = ProjectId.CreateNewId(debugName: "TestProject");
            DocumentId documentId = DocumentId.CreateNewId(projectId, debugName: "Test" + fileExtension);

            // find these assemblies in the running process
            string[] simpleNames = { "mscorlib", "System.Core", "System" };

#if !NETSTANDARD1_5
            IEnumerable<PortableExecutableReference> references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => simpleNames.Contains(a.GetName().Name, StringComparer.OrdinalIgnoreCase))
                .Select(a => MetadataReference.CreateFromFile(a.Location));
#else
            IEnumerable<PortableExecutableReference> references = Enumerable.Empty<PortableExecutableReference>();
#endif

            return new AdhocWorkspace().CurrentSolution
                .AddProject(projectId, "TestProject", "TestProject", LanguageName)
                .AddMetadataReferences(projectId, references)
                .AddDocument(documentId, "Test" + fileExtension, SourceText.From(code))
                .GetDocument(documentId);
        }

        protected void VerifyDocument(string expected, bool compareTokens, Document document)
        {
            if (compareTokens)
            {
                VerifyTokens(expected, Format(document).ToString());
            }
            else
            {
                VerifyText(expected, document);
            }
        }

        private SyntaxNode Format(Document document)
        {
            Document updatedDocument = document.WithSyntaxRoot(document.GetSyntaxRootAsync().Result);
            return Formatter.FormatAsync(Simplifier.ReduceAsync(updatedDocument, Simplifier.Annotation).Result, Formatter.Annotation).Result.GetSyntaxRootAsync().Result;
        }

        private IList<SyntaxToken> ParseTokens(string text)
        {
            return LanguageName == LanguageNames.CSharp
                ? Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseTokens(text).Select(t => (SyntaxToken)t).ToList()
                : Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.ParseTokens(text).Select(t => (SyntaxToken)t).ToList();
        }

        private bool VerifyTokens(string expected, string actual)
        {
            IList<SyntaxToken> expectedNewTokens = ParseTokens(expected);
            IList<SyntaxToken> actualNewTokens = ParseTokens(actual);

            for (int i = 0; i < Math.Min(expectedNewTokens.Count, actualNewTokens.Count); i++)
            {
                Assert.Equal(expectedNewTokens[i].ToString(), actualNewTokens[i].ToString());
            }

            if (expectedNewTokens.Count != actualNewTokens.Count)
            {
                string expectedDisplay = string.Join(" ", expectedNewTokens.Select(t => t.ToString()));
                string actualDisplay = string.Join(" ", actualNewTokens.Select(t => t.ToString()));
                Assert.True(false,
                    string.Format("Wrong token count. Expected '{0}', Actual '{1}', Expected Text: '{2}', Actual Text: '{3}'",
                        expectedNewTokens.Count, actualNewTokens.Count, expectedDisplay, actualDisplay));
            }

            return true;
        }

        private bool VerifyText(string expected, Document document)
        {
            string actual = Format(document).ToString();
            Assert.Equal(expected, actual);
            return true;
        }

        protected abstract string LanguageName { get; }
    }
}
