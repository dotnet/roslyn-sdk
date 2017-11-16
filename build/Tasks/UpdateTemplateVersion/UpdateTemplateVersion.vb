' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Text
Imports Microsoft.Build.Framework
Imports Microsoft.Build.Utilities
Imports <xmlns="http://schemas.microsoft.com/developer/vstemplate/2005">

Public Class UpdateTemplateVersion
    Inherits Task

    <Required>
    Public Property VSTemplatesToRewrite As ITaskItem()

    <Required>
    Public Property AssemblyVersion As String

    <Required>
    Public Property IntermediatePath As String

    Private ReadOnly _newVSTemplates As New List(Of ITaskItem)

    <Output>
    Public ReadOnly Property NewVSTemplates As ITaskItem()
        Get
            Return _newVSTemplates.ToArray()
        End Get
    End Property

    Public Overrides Function Execute() As Boolean
        Try
            If Directory.Exists(IntermediatePath) Then
                For Each file In New DirectoryInfo(IntermediatePath).GetFiles("*.*", SearchOption.AllDirectories)
                    file.IsReadOnly = False
                Next

                Directory.Delete(IntermediatePath, recursive:=True)
            End If

            For Each template In VSTemplatesToRewrite
                Dim templateXml = UpdateAssemblyVersion(template.ItemSpec)
                Dim newDirectory = SaveTemplateXml(templateXml, template.ItemSpec, IntermediatePath, template.GetMetadata("OutputSubPath"), _newVSTemplates)
                ' For all other files, just copy them along
                Dim originalDirectory = Path.GetDirectoryName(Path.GetFullPath(template.ItemSpec))
                For Each extraFile In New DirectoryInfo(originalDirectory).GetFiles("*.*", SearchOption.AllDirectories)
                    If extraFile.Extension = ".vstemplate" Then
                        ' If this is one of our multi-project template files we need to do a rename
                        If IsReferencedTemplate(template, extraFile) Then
                            Dim referencedTemplateXml = UpdateAssemblyVersion(extraFile.FullName)
                            Dim relativePath = GetRelativePath(Path.GetFullPath(newDirectory), extraFile.FullName).Replace("..\", String.Empty)
                            Dim fullPath = Path.GetFullPath(Path.Combine(newDirectory, relativePath))
                            referencedTemplateXml.Save(fullPath)
                        Else
                            Continue For
                        End If
                    End If
                    CopyExtraFile(newDirectory, originalDirectory, extraFile.FullName)
                Next
            Next

            Return True
        Catch ex As Exception
            ' Show the stack trace in the build log to make diagnosis easier.
            Log.LogMessage($"Exception was thrown, stack trace was: {Environment.NewLine}{ex.StackTrace}")
            Throw
        End Try
    End Function

    Private Function UpdateAssemblyVersion(fullPath As String) As XDocument
        Dim xml = XDocument.Load(fullPath)

        For Each assembly In xml...<Assembly>
            assembly.Value = assembly.Value.Replace("$(AssemblyVersion)", AssemblyVersion)
        Next

        Return xml
    End Function

    Private Function SaveTemplateXml(xml As XDocument, templatePath As String, intermediatePath As String, metaDataValue As String, ByRef newItems As List(Of ITaskItem)) As String
        Dim newDirectory = Path.Combine(intermediatePath, Path.GetDirectoryName(templatePath))
        Directory.CreateDirectory(newDirectory)

        ' Rewrite the .vstemplate file and save it
        Dim newItemFilePath = Path.Combine(newDirectory, Path.GetFileName(templatePath))

        ' Work around the inability of the SDK to take an absolute path here
        Dim newItem = New TaskItem(newItemFilePath)
        newItem.SetMetadata("OutputSubPath", metaDataValue)
        newItems.Add(newItem)

        xml.Save(newItemFilePath)
        Return newDirectory
    End Function

    Private Function GetRelativePath(fromPath As String, toPath As String) As String
        Dim fromAttr = GetPathAttribute(fromPath)
        Dim toAttr = GetPathAttribute(toPath)
        Dim path = New StringBuilder(260)

        If Not PathRelativePathTo(path, fromPath, fromAttr, toPath, toAttr) Then
            Throw New ArgumentException($"Paths {fromPath} and {toPath} do not have a common prefix")
        End If

        Return path.ToString()
    End Function

    Private Declare Auto Function PathRelativePathTo Lib "shlwapi.dll" (pszPath As StringBuilder, pszFrom As String, dwAttrFrom As Integer, pszTo As String, dwAttrTo As Integer) As Boolean

    Public Sub CopyExtraFile(newDirectory As String, originalDirectory As String, extraFile As String)
        Dim relativePath = extraFile.Substring(originalDirectory.Length)

        ' This intentionally isn't using path.combine since relative path may lead with a \
        Dim fullPath = Path.GetFullPath(newDirectory + relativePath)

        Try
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath))
            File.Copy(extraFile, fullPath)

            ' Ensure the copy isn't read only
            Dim copyFileInfo As New FileInfo(fullPath)
            copyFileInfo.IsReadOnly = False
        Catch ex As Exception
        End Try
    End Sub

    Private Function GetPathAttribute(path As String) As Integer
        Dim directoryInfo = New DirectoryInfo(path)
        If directoryInfo.Exists Then
            ' This magic hexadecimal number represents a folder.
            Return &H10
        End If

        Dim fileInfo = New FileInfo(path)
        If fileInfo.Exists Then
            ' This magic hexadecimal number represents a file.
            Return &H80
        End If

        ' If the path doesn't exist assume it to be a folder.
        Return &H10
    End Function

    Private Shared Function IsReferencedTemplate(template As ITaskItem, extraFile As FileInfo) As Boolean
        Return IsReferencedCSharpTemplate(template, extraFile) OrElse
               IsReferencedVisualBasicTemplate(template, extraFile)
    End Function

    Private Shared Function IsReferencedCSharpTemplate(template As ITaskItem, extraFile As FileInfo) As Boolean
        Return (template.ItemSpec.Contains("CSharpDiagnostic.vstemplate") Or
                template.ItemSpec.Contains("CSRef.vstemplate")) AndAlso
               extraFile.FullName.Contains("CSharp") AndAlso
               (extraFile.FullName.Contains("DiagnosticAnalyzer") Or
                extraFile.FullName.Contains("Test") Or
                extraFile.FullName.Contains("Vsix") Or
                extraFile.FullName.Contains("CodeRefactoring"))
    End Function

    Private Shared Function IsReferencedVisualBasicTemplate(template As ITaskItem, extraFile As FileInfo) As Boolean
        Return (template.ItemSpec.Contains("VBDiagnostic.vstemplate") Or
                template.ItemSpec.Contains("VBRef.vstemplate")) AndAlso
               extraFile.FullName.Contains("VisualBasic") AndAlso
               (extraFile.FullName.Contains("DiagnosticAnalyzer") Or
                extraFile.FullName.Contains("Test") Or
                extraFile.FullName.Contains("Vsix") Or
                extraFile.FullName.Contains("CodeRefactoring"))
    End Function
End Class
