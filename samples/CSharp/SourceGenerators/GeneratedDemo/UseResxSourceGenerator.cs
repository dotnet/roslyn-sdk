using System;

namespace GeneratedDemo
{
    internal static class UseResxSourceGenerator
    {
        public static void Run()
        {
            Console.WriteLine($"Reading resources from '{typeof(global::GeneratedDemo.Resources)}'");
            Console.WriteLine(global::GeneratedDemo.Resources.Example);

            Console.WriteLine($"Reading resources from '{typeof(global::GeneratedDemo.SubFolder.Resources)}'");
            Console.WriteLine(global::GeneratedDemo.SubFolder.Resources.SubExample);
        }
    }
}
