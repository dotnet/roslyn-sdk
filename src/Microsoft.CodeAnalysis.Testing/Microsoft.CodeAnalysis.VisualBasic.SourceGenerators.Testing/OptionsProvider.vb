' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics

Friend Class OptionsProvider
    Inherits AnalyzerConfigOptionsProvider

    Private ReadOnly _analyzerConfigOptionsProvider As AnalyzerConfigOptionsProvider
    Private ReadOnly _globalOptions As AnalyzerConfigOptions

    Public Sub New(analyzerConfigOptionsProvider As AnalyzerConfigOptionsProvider, globalOptions As List(Of (String, String)))
        _analyzerConfigOptionsProvider = analyzerConfigOptionsProvider
        _globalOptions = New ConfigOptions(_analyzerConfigOptionsProvider.GlobalOptions, globalOptions)
    End Sub

    Public Overrides ReadOnly Property GlobalOptions As AnalyzerConfigOptions
        Get
            Return _globalOptions
        End Get
    End Property

    Public Overrides Function GetOptions(tree As SyntaxTree) As AnalyzerConfigOptions
        Return _analyzerConfigOptionsProvider.GetOptions(tree)
    End Function

    Public Overrides Function GetOptions(additionalFile As AdditionalText) As AnalyzerConfigOptions
        Return _analyzerConfigOptionsProvider.GetOptions(additionalFile)
    End Function

End Class
