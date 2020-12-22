using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Analyzer1
{
    [Generator(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class SettingsXmlGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // Using the context, get any additional files that end in .xmlsettings
            IEnumerable<AdditionalText> settingsFiles = context.AdditionalFiles.Where(at => at.Path.EndsWith(".xmlsettings"));
            foreach (AdditionalText settingsFile in settingsFiles)
            {
                ProcessSettingsFile(settingsFile, context);
            }
        }
        
        private void ProcessSettingsFile(AdditionalText xmlFile, GeneratorExecutionContext context)
        {
            // try and load the settings file
            XmlDocument xmlDoc = new XmlDocument();
            string text = xmlFile.GetText(context.CancellationToken).ToString();
            try
            {
                xmlDoc.LoadXml(text);
            }
            catch
            {
                //TODO: issue a diagnostic that says we couldn't parse it
                return;
            }

            
            // create a class in the XmlSetting class that represnts this entry, and a static field that contains a singleton instance.
            string fileName = Path.GetFileName(xmlFile.Path);
            string name = xmlDoc.DocumentElement.GetAttribute("name");

            StringBuilder sb;
            if (context.Compilation.Language == LanguageNames.CSharp)
            {
                sb = new StringBuilder($@"
namespace AutoSettings
{{
    using System;
    using System.Xml;

    public partial class XmlSettings
    {{

        public static {name}Settings {name} {{ get; }} = new {name}Settings(""{fileName}"");

        public class {name}Settings
        {{

            XmlDocument xmlDoc = new XmlDocument();

            private string fileName;

            public string GetLocation() => fileName;

            internal {name}Settings(string fileName)
            {{
                this.fileName = fileName;
                xmlDoc.Load(fileName);
            }}
");
            }
            else
            {
                sb = new StringBuilder($@"
Imports System
Imports System.Xml

Namespace Global.AutoSettings
    Partial Public Class XmlSettings

        Public Shared ReadOnly Property {name} As {name}Settings = New {name}Settings(""{fileName}"")

        Public Class {name}Settings
            Private xmlDoc As New XmlDocument()

            Private fileName As String

            Public Function GetLocation() As String
                Return fileName
            End Function

            Friend Sub New(fileName As String)
                Me.fileName = fileName
                xmlDoc.Load(fileName)
            End Sub
");
            }

            for(int i = 0; i < xmlDoc.DocumentElement.ChildNodes.Count; i++)
            {
                XmlElement setting = (XmlElement)xmlDoc.DocumentElement.ChildNodes[i];
                string settingName = setting.GetAttribute("name");
                string settingType = setting.GetAttribute("type");

                if (context.Compilation.Language == LanguageNames.CSharp)
                {
                    sb.Append($@"

public {settingType} {settingName}
{{
    get
    {{
        return ({settingType}) Convert.ChangeType(((XmlElement)xmlDoc.DocumentElement.ChildNodes[{i}]).InnerText, typeof({settingType}));
    }}
}}
");
                }
                else
                {
                    sb.Append($@"

Public ReadOnly Property {settingName} As {settingType}
    Get
        Return DirectCast(Convert.ChangeType(DirectCast(xmlDoc.DocumentElement.ChildNodes({i}), XmlElement).InnerText, GetType({settingType})), {settingType})
    End Get
End Property
");
                }
            }

            if (context.Compilation.Language == LanguageNames.CSharp)
            {
                sb.Append(@"
        }
    }
}
");
            }
            else
            {
                sb.Append(@"
        End Class
    End Class
End Namespace
");
            }

            context.AddSource($"Settings_{name}", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
     
        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
