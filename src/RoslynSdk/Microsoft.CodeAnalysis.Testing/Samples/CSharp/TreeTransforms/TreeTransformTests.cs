using Xunit;

namespace TreeTransforms
{
    public static class TreeTransformTests
    {
        [Fact]
        public static void LambdaToAnonMethodTest()
        {
            var input = @"
public class Test
{
    public static void Main(string[] args)
    {
        Func<int, int, int> f1 = (int x, int y) => { return x + y; };
    }
}";

            var expected_transform = @"
public class Test
{
    public static void Main(string[] args)
    {
        Func<int, int, int> f1 = delegate(int x, int y) { return x + y; };
    }
}";

            var actual_transform = Transforms.Transform(input, TransformKind.LambdaToAnonMethod);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void AnonMethodToLambdaTest()
        {
            var input = @"
public class Test
{
    public static void Main(string[] args)
    {
        Func<int, int, int> f1 = delegate(int x, int y) { return x + y; };
    }
}";

            var expected_transform = @"
public class Test
{
    public static void Main(string[] args)
    {
        Func<int, int, int> f1 = (int x, int y) =>{ return x + y; };
    }
}";
            var actual_transform = Transforms.Transform(input, TransformKind.AnonMethodToLambda);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void DoToWhileTest()
        {
            var input = @"
class Program
{
    static void Main()
    {
        int i = 0;
        int sum = 0;
        do
        {
            sum += i;
            i++;
        } while (i < 10);
        System.Console.WriteLine(sum);
    }
}
";

            var expected_transform = @"
class Program
{
    static void Main()
    {
        int i = 0;
        int sum = 0;
        while (i < 10)
        {
            sum += i;
            i++;
        } 
        System.Console.WriteLine(sum);
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.DoToWhile);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void WhileToDoTest()
        {
            var input = @"
class Program
{
    static void Main()
    {
        int i = 0;
        int sum = 0;
        while (i < 10)
        {
            sum += i;
            i++;
        }
        System.Console.WriteLine(sum);
    }
}
";

            var expected_transform = @"
class Program
{
    static void Main()
    {
        int i = 0;
        int sum = 0;
        do
        {
            sum += i;
            i++;
        }while (i < 10);
        System.Console.WriteLine(sum);
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.WhileToDo);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void CheckedStmtToUncheckedStmtTest()
        {
            var input = @"
class Program
{
    static void Main()
    {
        checked
        {
            int x = int.MaxValue;
            x = x + 1;
        }
    }
}
";

            var expected_transform = @"
class Program
{
    static void Main()
    {
        unchecked
        {
            int x = int.MaxValue;
            x = x + 1;
        }
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.CheckedStmtToUncheckedStmt);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void UncheckedStmtToCheckedStmt()
        {
            var input = @"
class Program
{
    static void Main()
    {
        unchecked
        {
            int x = int.MaxValue;
            x = x + 1;
        }
    }
}
";

            var expected_transform = @"
class Program
{
    static void Main()
    {
        checked
        {
            int x = int.MaxValue;
            x = x + 1;
        }
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.UncheckedStmtToCheckedStmt);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void CheckedExprToUncheckedExprTest()
        {
            var input = @"
class Program
{
    static void Main()
    {
        int x = int.MaxValue;
        x = checked(x + 1);
    }
}
";

            var expected_transform = @"
class Program
{
    static void Main()
    {
        int x = int.MaxValue;
        x = unchecked(x + 1);
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.CheckedExprToUncheckedExpr);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void UncheckedExprToCheckedExprTest()
        {
            var input = @"
class Program
{
    static void Main()
    {
        int x = int.MaxValue;
        x = unchecked(x + 1);
    }
}
";

            var expected_transform = @"
class Program
{
    static void Main()
    {
        int x = int.MaxValue;
        x = checked(x + 1);
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.UncheckedExprToCheckedExpr);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void PostfixToPrefixTest()
        {
            var input = @"
class Program
{
    static void Main()
    {
        int x = 10;
        /*START*/ x++ /*END*/;
        x--;
    }
}
";

            var expected_transform = @"
class Program
{
    static void Main()
    {
        int x = 10;
        /*START*/ ++x /*END*/;
        --x;
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.PostfixToPrefix);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void PrefixToPostfixTest()
        {
            var input = @"
class Program
{
    static void Main()
    {
        int x = 10;
        /*START*/ ++x /*END*/;
        --x;
    }
}
";

            var expected_transform = @"
class Program
{
    static void Main()
    {
        int x = 10;
        /*START*/ x++ /*END*/;
        x--;
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.PrefixToPostfix);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void TrueToFalseTest()
        {
            var input = @"
class Program
{
    static void Main()
    {
        bool b1 = true;
        if (true)
        {
        }
    }
}
";

            var expected_transform = @"
class Program
{
    static void Main()
    {
        bool b1 = false;
        if (false)
        {
        }
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.TrueToFalse);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void FalseToTrueTest()
        {
            var input = @"
class Program
{
    static void Main()
    {
        bool b1 = false;
        if (false)
        {
        }
    }
}
";

            var expected_transform = @"
class Program
{
    static void Main()
    {
        bool b1 = true;
        if (true)
        {
        }
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.FalseToTrue);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void AddAssignToAssignTest()
        {
            var input = @"
class Program
{
    static void Main()
    {
        int x = 10;
        int y = 45;
        x += y;
    }
}
";

            var expected_transform = @"
class Program
{
    static void Main()
    {
        int x = 10;
        int y = 45;
        x = x + y;
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.AddAssignToAssign);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void RefParamToOutParamTest()
        {
            var input = @"
class Program
{
    static void Method1(ref int i1, out int i2, int i3)
    {
        i2 = 45;
    }
    static void Main()
    {
        int x = 4, y = 5, z = 6;
        Method1(ref x, out y, z);
    }
}
";

            var expected_transform = @"
class Program
{
    static void Method1(out int i1, out int i2, int i3)
    {
        i2 = 45;
    }
    static void Main()
    {
        int x = 4, y = 5, z = 6;
        Method1(ref x, out y, z);
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.RefParamToOutParam);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void OutParamToRefParamTest()
        {
            var input = @"
class Program
{
    static void Method1(ref int i1, out int i2, int i3)
    {
        i2 = 45;
    }
    static void Main()
    {
        int x = 4, y = 5, z = 6;
        Method1(ref x, out y, z);
    }
}
";

            var expected_transform = @"
class Program
{
    static void Method1(ref int i1, ref int i2, int i3)
    {
        i2 = 45;
    }
    static void Main()
    {
        int x = 4, y = 5, z = 6;
        Method1(ref x, out y, z);
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.OutParamToRefParam);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void RefArgToOutArgTest()
        {
            var input = @"
class Program
{
    static void Method1(ref int i1, out int i2, int i3)
    {
        i2 = 45;
    }
    static void Main()
    {
        int x = 4, y = 5, z = 6;
        Method1(ref x, out y, z);
    }
}
";

            var expected_transform = @"
class Program
{
    static void Method1(ref int i1, out int i2, int i3)
    {
        i2 = 45;
    }
    static void Main()
    {
        int x = 4, y = 5, z = 6;
        Method1(out x, out y, z);
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.RefArgToOutArg);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void OutArgToRefArgTest()
        {
            var input = @"
class Program
{
    static void Method1(ref int i1, out int i2, int i3)
    {
        i2 = 45;
    }
    static void Main()
    {
        int x = 4, y = 5, z = 6;
        Method1(ref x, out y, z);
    }
}
";

            var expected_transform = @"
class Program
{
    static void Method1(ref int i1, out int i2, int i3)
    {
        i2 = 45;
    }
    static void Main()
    {
        int x = 4, y = 5, z = 6;
        Method1(ref x, ref y, z);
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.OutArgToRefArg);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void OrderByAscToOrderByDescTest()
        {
            var input = @"
using System;
using System.Linq;
class Program
{
    static void Main()
    {
        int[] numbers = { 3, 1, 4, 6, 10 };
        var sortedNumbers = from number in numbers orderby number ascending select number;
        foreach (var num in sortedNumbers)
            Console.WriteLine(num);
    }
}
";

            var expected_transform = @"
using System;
using System.Linq;
class Program
{
    static void Main()
    {
        int[] numbers = { 3, 1, 4, 6, 10 };
        var sortedNumbers = from number in numbers orderby number descending select number;
        foreach (var num in sortedNumbers)
            Console.WriteLine(num);
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.OrderByAscToOrderByDesc);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void OrderByDescToOrderByAscTest()
        {
            var input = @"
using System;
using System.Linq;
class Program
{
    static void Main()
    {
        int[] numbers = { 3, 1, 4, 6, 10 };
        var sortedNumbers = from number in numbers orderby number descending select number;
        foreach (var num in sortedNumbers)
            Console.WriteLine(num);
    }
}
";

            var expected_transform = @"
using System;
using System.Linq;
class Program
{
    static void Main()
    {
        int[] numbers = { 3, 1, 4, 6, 10 };
        var sortedNumbers = from number in numbers orderby number ascending select number;
        foreach (var num in sortedNumbers)
            Console.WriteLine(num);
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.OrderByDescToOrderByAsc);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void DefaultInitAllVarsTest()
        {
            var input = @"
class Program
{
    static void Main()
    {
        int i, j;
        Program f1;
    }
}
";

            var expected_transform = @"
class Program
{
    static void Main()
    {
        int i = default(int ), j = default(int );
        Program f1 = default(Program );
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.DefaultInitAllVars);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void ClassDeclToStructDeclTest()
        {
            var input = @"
class Program
{
    static void Main()
    {
    }
}
";

            var expected_transform = @"
struct Program
{
    static void Main()
    {
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.ClassDeclToStructDecl);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void StructDeclToClassDeclTest()
        {
            var input = @"
struct Program
{
    static void Main()
    {
    }
}
";

            var expected_transform = @"
class Program
{
    static void Main()
    {
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.StructDeclToClassDecl);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void IntTypeToLongTypeTest()
        {
            var input = @"
using System.Collections.Generic;
class Program
{    
    static void Main()
    {
        int i;
        List<int> l1 = new List<int>();
    }
}
";

            var expected_transform = @"
using System.Collections.Generic;
class Program
{    
    static void Main()
    {
        long i;
        List<long> l1 = new List<long>();
    }
}
";
            var actual_transform = Transforms.Transform(input, TransformKind.IntTypeToLongType);

            Assert.Equal(expected_transform, actual_transform);
        }
    }
}
