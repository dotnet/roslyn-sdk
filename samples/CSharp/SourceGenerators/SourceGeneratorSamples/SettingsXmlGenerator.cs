﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SourceGeneratorSamples;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Analyzer1
{
    [Generator]
    public class SettingsXmlGenerator : ISourceGenerator
    {
        private const string SettingsFileString = @"
namespace XmlSettings
{
    public partial class XmlSettings
    {
        
    }
}
";
        public void Execute(SourceGeneratorContext context)
        {
            string generatedSourceOutputPath = context.TryCreateGeneratedSourceOutputPath();

            // Using the context, get any additional files that end in .xmlsettings
            IEnumerable<AdditionalText> settingsFiles = context.AdditionalFiles.Where(at => at.Path.EndsWith(".xmlsettings"));
            foreach (AdditionalText settingsFile in settingsFiles)
            {
                ProcessSettingsFile(settingsFile, context, generatedSourceOutputPath);
            }
        }
        
        private void ProcessSettingsFile(AdditionalText xmlFile, SourceGeneratorContext context, string generatedSourceOutputPath)
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

            StringBuilder sb = new StringBuilder($@"
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

            for(int i = 0; i < xmlDoc.DocumentElement.ChildNodes.Count; i++)
            {
                XmlElement setting = (XmlElement)xmlDoc.DocumentElement.ChildNodes[i];
                string settingName = setting.GetAttribute("name");
                string settingType = setting.GetAttribute("type");

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

            sb.Append("} } }");

            context.AddSource(generatedSourceOutputPath, $"Settings_{name}", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
     
        public void Initialize(InitializationContext context)
        {
        }
    }
}
