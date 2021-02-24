Friend Module UseResxSourceGenerator
    Public Sub Run()
        Console.WriteLine($"Reading resources from '{GetType(Global.GeneratedDemo.VisualBasic.Resources)}'")
        Console.WriteLine(Global.GeneratedDemo.VisualBasic.Resources.Example)

        Console.WriteLine($"Reading resources from '{GetType(Global.GeneratedDemo.VisualBasic.SubFolder.Resources2)}'")
        Console.WriteLine(Global.GeneratedDemo.VisualBasic.SubFolder.Resources2.SubExample)
    End Sub
End Module
