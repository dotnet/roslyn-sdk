Imports System.Console
Imports CSV

Friend Class UseCsvGenerator
    Public Shared Sub Run()
        WriteLine("## CARS")
        Cars.All.ToList().ForEach(Sub(c) WriteLine(c.Brand & vbTab & c.Model & vbTab & c.Year & vbTab & c.Cc))
        WriteLine(vbCr & "## PEOPLE")
        People.All.ToList().ForEach(Sub(p) WriteLine(p.Name & vbTab & p.Address & vbTab & p._11Age))
    End Sub
End Class
