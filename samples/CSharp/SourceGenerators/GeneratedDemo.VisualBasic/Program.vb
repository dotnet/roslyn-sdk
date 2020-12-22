Friend Class Program
    Public Shared Sub Main(args As String())
        ' Run the various scenarios
        Console.WriteLine("Running HelloWorld:\n")
        UseHelloWorldGenerator.Run()

        Console.WriteLine("\n\nRunning AutoNotify:\n")
        UseAutoNotifyGenerator.Run()

        Console.WriteLine("\n\nRunning XmlSettings:\n")
        UseXmlSettingsGenerator.Run()

        Console.WriteLine("\n\nRunning CsvGenerator:\n")
        UseCsvGenerator.Run()

        Console.WriteLine("\n\nRunning MustacheGenerator:\n")
        UseMustacheGenerator.Run()
    End Sub
End Class
