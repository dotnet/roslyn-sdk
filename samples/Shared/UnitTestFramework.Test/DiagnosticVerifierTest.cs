﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using Xunit.Sdk;
using static Roslyn.UnitTestFramework.DiagnosticVerifier<Roslyn.UnitTestFramework.Test.CSharpSyntaxTreeDiagnosticAnalyzer>;

namespace Roslyn.UnitTestFramework.Test
{
    /// <summary>
    /// This class verifies that <see cref="DiagnosticVerifier{TAnalyzer}"/> will correctly report failing tests.
    /// </summary>
    public class DiagnosticVerifierTest
    {
        [Fact]
        public async Task TestExpectedDiagnosticMissingAsync()
        {
            string testCode = @"
class ClassName
{
    void MethodName()
    {
        ;
    }
}
";

            DiagnosticResult expected = Diagnostic();
            XunitException ex = await Assert.ThrowsAnyAsync<XunitException>(
                async () =>
                {
                    await VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
                }).ConfigureAwait(false);
            Assert.StartsWith("Mismatch between number of diagnostics returned, expected \"1\" actual \"0\"", ex.Message);
        }

        [Fact]
        public async Task TestValidBehaviorAsync()
        {
            string testCode = @"
class ClassName
{
    int property;
    int PropertyName
    {
        get{return this.property;}
    }
}
";

            DiagnosticResult expected = Diagnostic().WithArguments("followed").WithLocation(7, 33);

            await VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestValidBehaviorUncheckedLineAsync()
        {
            string testCode = @"
class ClassName
{
    int property;
    int PropertyName
    {
        get{return this.property;}
    }
}
";

            DiagnosticResult expected = Diagnostic().WithArguments("followed").WithLocation(0, 33);

            await VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestValidBehaviorUncheckedColumnAsync()
        {
            string testCode = @"
class ClassName
{
    int property;
    int PropertyName
    {
        get{return this.property;}
    }
}
";

            DiagnosticResult expected = Diagnostic().WithArguments("followed").WithLocation(7, 0);

            await VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestValidBehaviorWithFullSpanAsync()
        {
            string testCode = @"
class ClassName
{
    int property;
    int PropertyName
    {
        get{return this.property;}
    }
}
";

            DiagnosticResult expected = Diagnostic().WithArguments("followed").WithSpan(7, 33, 7, 34);

            await VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestUnexpectedLocationForProjectDiagnosticAsync()
        {
            string testCode = @"
class ClassName
{
    int property;
    int PropertyName
    {
        get{return this.property;}
    }
}
";

            // By failing to include a location, the verified thinks we're only trying to verify a project diagnostic.
            DiagnosticResult expected = Diagnostic().WithArguments("followed");

            XunitException ex = await Assert.ThrowsAnyAsync<XunitException>(
                async () =>
                {
                    await VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
                }).ConfigureAwait(false);
            Assert.StartsWith("Expected:\nA project diagnostic with No location\nActual:\n", ex.Message);
        }

        [Fact]
        public async Task TestUnexpectedMessageAsync()
        {
            string testCode = @"
class ClassName
{
    int property;
    int PropertyName
    {
        get{return this.property;}
    }
}
";

            XunitException ex = await Assert.ThrowsAnyAsync<XunitException>(
                async () =>
                {
                    await VerifyCSharpDiagnosticAsync(testCode, EmptyDiagnosticResults, CancellationToken.None).ConfigureAwait(false);
                }).ConfigureAwait(false);
            Assert.StartsWith("Mismatch between number of diagnostics returned, expected \"0\" actual \"1\"", ex.Message);
            Assert.Contains("warning TEST01", ex.Message);
        }

        [Fact]
        public async Task TestUnexpectedAnalyzerErrorAsync()
        {
            string testCode = @"
class ClassName
{
    void MethodName()
    {
        ;
    }
}
";

            XunitException ex = await Assert.ThrowsAnyAsync<XunitException>(
                async () =>
                {
                    await DiagnosticVerifier<ErrorThrowingAnalyzer>.VerifyCSharpDiagnosticAsync(testCode, EmptyDiagnosticResults, CancellationToken.None).ConfigureAwait(false);
                }).ConfigureAwait(false);
            Assert.StartsWith("Mismatch between number of diagnostics returned, expected \"0\" actual \"2\"", ex.Message);
            Assert.Contains("error AD0001", ex.Message);
        }

        [Fact]
        public async Task TestUnexpectedCompilerErrorAsync()
        {
            string testCode = @"
class ClassName
{
    int property;
    Int32 PropertyName
    {
        get{return this.property;}
    }
}
";

            DiagnosticResult expected = Diagnostic().WithArguments("followed").WithLocation(7, 33);

            XunitException ex = await Assert.ThrowsAnyAsync<XunitException>(
                async () =>
                {
                    await VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
                }).ConfigureAwait(false);
            Assert.StartsWith("Mismatch between number of diagnostics returned, expected \"1\" actual \"2\"", ex.Message);
            Assert.Contains("error CS0246", ex.Message);
        }

        [Fact]
        public async Task TestUnexpectedCompilerWarningAsync()
        {
            string testCode = @"
class ClassName
{
    int property;
    Int32 PropertyName
    {
        ///
        get{return this.property;}
    }
}
";

            DiagnosticResult expected = Diagnostic().WithArguments("followed").WithLocation(8, 33);

            XunitException ex = await Assert.ThrowsAnyAsync<XunitException>(
                async () =>
                {
                    await VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
                }).ConfigureAwait(false);
            Assert.StartsWith("Mismatch between number of diagnostics returned, expected \"1\" actual \"2\"", ex.Message);
            Assert.Contains("error CS0246", ex.Message);
        }

        [Fact]
        public async Task TestInvalidDiagnosticIdAsync()
        {
            string testCode = @"
class ClassName
{
    int property;
    int PropertyName
    {
        get{return this.property;}
    }
}
";

            DiagnosticDescriptor descriptor = new DiagnosticDescriptor("SA9999", "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);
            DiagnosticResult expected = Diagnostic(descriptor).WithLocation(7, 33);

            XunitException ex = await Assert.ThrowsAnyAsync<XunitException>(
                async () =>
                {
                    await VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
                }).ConfigureAwait(false);
            Assert.StartsWith($"Expected diagnostic id to be \"SA9999\" was \"{CSharpSyntaxTreeDiagnosticAnalyzer.DiagnosticId}\"", ex.Message);
        }

        [Fact]
        public async Task TestInvalidSeverityAsync()
        {
            string testCode = @"
class ClassName
{
    int property;
    int PropertyName
    {
        get{return this.property;}
    }
}
";

            DiagnosticResult expected = Diagnostic().WithLocation(7, 33).WithSeverity(DiagnosticSeverity.Error);

            XunitException ex = await Assert.ThrowsAnyAsync<XunitException>(
                async () =>
                {
                    await VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
                }).ConfigureAwait(false);
            Assert.StartsWith("Expected diagnostic severity to be \"Error\" was \"Warning\"", ex.Message);
        }

        [Fact]
        public async Task TestIncorrectLocationLine1Async()
        {
            string testCode = @"
class ClassName
{
    int property;
    int PropertyName
    {
        get{return this.property;}
    }
}
";

            DiagnosticResult expected = Diagnostic().WithArguments("followed").WithLocation(8, 33);

            XunitException ex = await Assert.ThrowsAnyAsync<XunitException>(
                async () =>
                {
                    await VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
                }).ConfigureAwait(false);
            Assert.StartsWith("Expected diagnostic to start on line \"8\" was actually on line \"7\"", ex.Message);
        }

        [Fact]
        public async Task TestIncorrectLocationLine2Async()
        {
            string testCode = @"
class ClassName
{
    int property;
    int PropertyName
    {
        get{return this.property;}
        set{this.property = value;}
    }
}
";

            DiagnosticResult[] expected =
            {
                Diagnostic().WithArguments("followed").WithLocation(7, 33),
                Diagnostic().WithArguments("followed").WithLocation(7, 34),
            };

            XunitException ex = await Assert.ThrowsAnyAsync<XunitException>(
                async () =>
                {
                    await VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
                }).ConfigureAwait(false);
            Assert.StartsWith("Expected diagnostic to start on line \"7\" was actually on line \"8\"", ex.Message);
        }

        [Fact]
        public async Task TestIncorrectLocationColumnAsync()
        {
            string testCode = @"
class ClassName
{
    int property;
    int PropertyName
    {
        get{return this.property;}
    }
}
";

            DiagnosticResult expected = Diagnostic().WithArguments("followed").WithLocation(7, 34);

            XunitException ex = await Assert.ThrowsAnyAsync<XunitException>(
                async () =>
                {
                    await VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
                }).ConfigureAwait(false);
            Assert.StartsWith("Expected diagnostic to start at column \"34\" was actually at column \"33\"", ex.Message);
        }

        [Fact]
        public async Task TestIncorrectLocationEndColumnAsync()
        {
            string testCode = @"
class ClassName
{
    int property;
    int PropertyName
    {
        get{return this.property;}
    }
}
";

            DiagnosticResult expected = Diagnostic().WithArguments("followed").WithSpan(7, 33, 7, 35);

            XunitException ex = await Assert.ThrowsAnyAsync<XunitException>(
                async () =>
                {
                    await VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
                }).ConfigureAwait(false);
            Assert.StartsWith("Expected diagnostic to end at column \"35\" was actually at column \"34\"", ex.Message);
        }

        [Fact]
        public async Task TestIncorrectMessageAsync()
        {
            string testCode = @"
class ClassName
{
    int property;
    int PropertyName
    {
        get{return this.property;}
    }
}
";

            DiagnosticResult expected = Diagnostic().WithArguments("bogus argument").WithLocation(7, 33);

            XunitException ex = await Assert.ThrowsAnyAsync<XunitException>(
                async () =>
                {
                    await VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
                }).ConfigureAwait(false);
            Assert.StartsWith("Expected diagnostic message to be ", ex.Message);
        }

        [Fact]
        public async Task TestIncorrectAdditionalLocationAsync()
        {
            string testCode = @"
class ClassName
{
    int property;
    int PropertyName
    {
        get{return this.property;}
    }
}
";

            DiagnosticResult expected = Diagnostic().WithArguments("bogus argument").WithLocation(7, 33).WithLocation(8, 34);

            XunitException ex = await Assert.ThrowsAnyAsync<XunitException>(
                async () =>
                {
                    await VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
                }).ConfigureAwait(false);
            Assert.StartsWith("Expected 1 additional locations but got 0 for Diagnostic", ex.Message);
        }

        private class ErrorThrowingAnalyzer : CSharpSyntaxTreeDiagnosticAnalyzer
        {
            private static readonly Action<SyntaxNodeAnalysisContext> BlockAction = HandleBlock;

            /// <inheritdoc/>
            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(BlockAction, SyntaxKind.Block);
            }

            private static void HandleBlock(SyntaxNodeAnalysisContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
