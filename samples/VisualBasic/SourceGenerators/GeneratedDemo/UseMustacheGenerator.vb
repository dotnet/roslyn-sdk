Option Explicit On
Option Strict On
Option Infer On

Imports GeneratedDemo.UseMustacheGenerator

<Assembly: Mustache("Lottery", t1, h1)>
<Assembly: Mustache("HR", t2, h2)>
<Assembly: Mustache("HTML", t3, h3)>
<Assembly: Mustache("Section", t4, h4)>
<Assembly: Mustache("NestedSection", t5, h5)>

Friend Class UseMustacheGenerator

    Public Shared Sub Run()
        Console.WriteLine(Mustache.Constants.Lottery)
        Console.WriteLine(Mustache.Constants.HR)
        Console.WriteLine(Mustache.Constants.HTML)
        Console.WriteLine(Mustache.Constants.Section)
        Console.WriteLine(Mustache.Constants.NestedSection)
    End Sub

    ' Mustache templates and hashes from the manual at https://mustache.github.io/mustache.1.html...
    Public Const t1 As String = "
Hello {{name}}
You have just won {{value}} dollars!
{{#in_ca}}
Well, {{taxed_value}} dollars, after taxes.
{{/in_ca}}
"
    Public Const h1 As String = "
{
  ""name"": ""Chris"",
  ""value"": 10000,
  ""taxed_value"": 5000,
  ""in_ca"": true
}
"
    Public Const t2 As String = "
* {{name}}
* {{age}}
* {{company}}
* {{{company}}}
"
    Public Const h2 As String = "
{
  ""name"": ""Chris"",
  ""company"": ""<b>GitHub</b>""
}
"
    Public Const t3 As String = "
    Shown
    {{#person}}
      Never shown!
    {{/person}}
    "
    Public Const h3 As String = "
{
  ""person"": false
}
"
    Public Const t4 As String = "
{{#repo}}
  <b>{{name}}</b>
{{/repo}}
"
    Public Const h4 As String = "
{
  ""repo"": [
    { ""name"": ""resque"" },
    { ""name"": ""hub"" },
    { ""name"": ""rip"" }
  ]
}
"
    Public Const t5 As String = "
{{#repo}}
  <b>{{name}}</b>
    {{#nested}}
        NestedName: {{name}}
    {{/nested}}
{{/repo}}
"
    Public Const h5 As String = "
{
  ""repo"": [
    { ""name"": ""resque"", ""nested"":[{""name"":""nestedResque""}] },
    { ""name"": ""hub"" },
    { ""name"": ""rip"" }
  ]
}
"

End Class
