using System;

namespace GeneratedDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            // Run the various scenarios
            Console.WriteLine("Running HelloWorld:\n");
            UseHelloWorldGenerator.Run();

            Console.WriteLine("\n\nRunning AutoNotify:\n");
            UseAutoNotifyGenerator.Run();

            Console.WriteLine("\n\nRunning XmlSettings:\n");
            UseXmlSettingsGenerator.Run();
        }
    }
}
