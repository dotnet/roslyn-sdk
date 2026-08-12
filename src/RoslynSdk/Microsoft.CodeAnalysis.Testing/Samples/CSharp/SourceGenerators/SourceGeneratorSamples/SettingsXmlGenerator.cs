using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable RS1035 // Do not use banned APIs for analyzers

namespace Analyzer1
{
    [Generator]
    public class SettingsXmlGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // Using the context, get any additional files that end in .xmlsettings
            var settingsFiles = context.AdditionalFiles.Where(at => at.Path.EndsWith(".xmlsettings"));
            foreach (var settingsFile in settingsFiles)
            {
                ProcessSettingsFile(settingsFile, context);
            }
        }

        private void ProcessSettingsFile(AdditionalText xmlFile, GeneratorExecutionContext context)
        {
            // try and load the settings file
            var xmlDoc = new XmlDocument();
            var text = xmlFile.GetText(context.CancellationToken).ToString();
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
            var fileName = Path.GetFileName(xmlFile.Path);
            var name = xmlDoc.DocumentElement.GetAttribute("name");

            var sb = new StringBuilder($@"
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

            for (var i = 0; i < xmlDoc.DocumentElement.ChildNodes.Count; i++)
            {
                var setting = (XmlElement)xmlDoc.DocumentElement.ChildNodes[i];
                var settingName = setting.GetAttribute("name");
                var settingType = setting.GetAttribute("type");

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

            context.AddSource($"Settings_{name}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
