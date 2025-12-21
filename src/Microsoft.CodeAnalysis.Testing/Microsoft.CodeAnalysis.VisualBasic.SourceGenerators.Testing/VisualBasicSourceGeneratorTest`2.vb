' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing

Public Class VisualBasicSourceGeneratorTest(Of TSourceGenerator As New, TVerifier As {IVerifier, New})
    Inherits SourceGeneratorTest(Of TVerifier)

    Private _GlobalOptions As List(Of (String, String))
    Private _LanguageVersion As LanguageVersion =
        If([Enum].TryParse("Default", _LanguageVersion), _LanguageVersion, LanguageVersion.VisualBasic14)

    Public Overrides ReadOnly Property Language As String
        Get
            Return LanguageNames.VisualBasic
        End Get
    End Property

    Protected Overrides ReadOnly Property DefaultFileExt As String
        Get
            Return "vb"
        End Get
    End Property

    ''' <summary>
    ''' Gets the global options to be used in <see cref="GetAnalyzerOptions"/>.
    ''' This can be appended to by the user to provide additional options.
    ''' </summary>
    Public ReadOnly Property GlobalOptions As List(Of (String, String))
        Get
            If _GlobalOptions Is Nothing Then
                _GlobalOptions = New List(Of (String, String))()
            End If
            Return _GlobalOptions
        End Get
    End Property

    ''' <summary>
    ''' Gets Or sets the Visual Basic language version used for the test. The default Is <see cref="LanguageVersion.Default"/>.
    ''' </summary>
    Public Property LanguageVersion As LanguageVersion
        Get
            Return CType(_LanguageVersion, LanguageVersion)
        End Get
        Set(value As LanguageVersion)
            _LanguageVersion = value
        End Set
    End Property

    Protected Overrides Function CreateCompilationOptions() As CompilationOptions
        Return New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    End Function

    Protected Overrides Function CreateParseOptions() As ParseOptions
        Return New VisualBasicParseOptions(LanguageVersion, DocumentationMode.Diagnose)
    End Function

    Protected Overrides Function GetSourceGenerators() As IEnumerable(Of Type)
        Return New Type() {GetType(TSourceGenerator)}
    End Function

    Protected Overrides Function GetAnalyzerOptions(project As Project) As AnalyzerOptions
        Return New AnalyzerOptions(
            project.AnalyzerOptions.AdditionalFiles,
            New OptionsProvider(project.AnalyzerOptions.AnalyzerConfigOptionsProvider, GlobalOptions))
    End Function

End Class
