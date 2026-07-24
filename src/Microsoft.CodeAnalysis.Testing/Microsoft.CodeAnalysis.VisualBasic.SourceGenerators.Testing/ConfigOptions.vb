' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics

Friend Class ConfigOptions
    Inherits AnalyzerConfigOptions

    Private ReadOnly _workspaceOptions As AnalyzerConfigOptions
    Private ReadOnly _globalOptions As Dictionary(Of String, String)

    Public Sub New(workspaceOptions As AnalyzerConfigOptions, globalOptions As List(Of (String, String)))
        _workspaceOptions = workspaceOptions
        _globalOptions = globalOptions.ToDictionary(Function(pair) pair.Item1, Function(pair) pair.Item2)
    End Sub

    Public Overrides Function TryGetValue(key As String, ByRef value As String) As Boolean
        Return _workspaceOptions.TryGetValue(key, value) Or _globalOptions.TryGetValue(key, value)
    End Function

End Class
