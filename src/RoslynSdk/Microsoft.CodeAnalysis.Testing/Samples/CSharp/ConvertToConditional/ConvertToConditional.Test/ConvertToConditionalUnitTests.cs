// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Roslyn.UnitTestFramework;
using Xunit;

namespace ConvertToConditional.Test
{
    public class ConvertToConditionalTests : CodeRefactoringProviderTestFixture
    {
        [Fact]
        public void ReturnSimpleCase()
        {
            var initialCode =
@"class C
{
    int M(bool p)
    {
        [||]if (p)
            return 0;
        else
            return 1;
    }
}";

            var expectedCode =
@"class C
{
    int M(bool p)
    {
        return p ? 0 : 1;
    }
}";

            Test(initialCode, expectedCode);
        }

        [Fact]
        public void ReturnCastToReturnType()
        {
            var initialCode =
@"class C
{
    byte M(bool p)
    {
        [||]if (p)
            return 0;
        else
            return 1;
    }
}";

            var expectedCode =
@"class C
{
    byte M(bool p)
    {
        return p ? 0 : 1;
    }
}";

            Test(initialCode, expectedCode);
        }

        [Fact]
        public void ReturnReferenceTypes()
        {
            var initialCode =
@"class A
{
}

class B : A
{
}

class C : B
{
}

class D
{
    A M(bool p)
    {
        [||]if (p)
            return new C();
        else
            return new B();
    }
}";

            var expectedCode =
@"class A
{
}

class B : A
{
}

class C : B
{
}

class D
{
    A M(bool p)
    {
        return p ? new C() : new B();
    }
}";

            Test(initialCode, expectedCode);
        }

        [Fact]
        public void ReturnReferenceTypesWithCast()
        {
            var initialCode =
@"class A
{
}

class B : A
{
}

class C : A
{
}

class D
{
    A M(bool p)
    {
        [||]if (p)
            return new C();
        else
            return new B();
    }
}";

            var expectedCode =
@"class A
{
}

class B : A
{
}

class C : A
{
}

class D
{
    A M(bool p)
    {
        return p ? (A)new C() : new B();
    }
}";

            Test(initialCode, expectedCode);
        }

        [Fact]
        public void ParenthesizeConditionThatIsBooleanAssignment_Bug8236()
        {
            var initialCode =
@"using System;

public class C
{
    int Goo(bool x, bool y)
    {
        [||]if (x = y)
        {
            return 1;
        }
        else
        {
            return 2;
        }
    }
}
";

            var expectedCode =
@"using System;

public class C
{
    int Goo(bool x, bool y)
    {
        return (x = y) ? 1 : 2;
    }
}
";

            Test(initialCode, expectedCode);
        }

        [Fact]
        public void ParenthesizeLambdaIfNeeded_Bug8238()
        {
            var initialCode =
@"using System;

public class C
{
    Func<int> Goo(bool x)
    {
        [||]if (x)
        {
            return () => 1;
        }
        else
        {
            return () => 2;
        }
    }
}
";

            var expectedCode =
@"using System;

public class C
{
    Func<int> Goo(bool x)
    {
        return x ? () => 1 : () => 2;
    }
}
";

            Test(initialCode, expectedCode);
        }

        protected override CodeRefactoringProvider CreateCodeRefactoringProvider => new ConvertToConditionalCodeRefactoringProvider();

        protected override string LanguageName => LanguageNames.CSharp;
    }
}
