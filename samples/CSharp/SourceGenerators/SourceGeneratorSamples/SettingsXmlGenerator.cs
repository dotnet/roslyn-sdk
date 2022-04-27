namespace Analyzer1;

[Generator]
public class SettingsXmlGenerator : IIncrementalGenerator
{
    private void ProcessSettingsFile(SourceProductionContext context,  AdditionalText xmlFile)
    {
        // try and load the settings file
        XmlDocument xmlDoc = new ();
        var text = xmlFile.GetText(context.CancellationToken)?.ToString();
        if(text is null) throw new Exception("Error reading the settings file");

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

        StringBuilder sb = new($@"
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

        context.AddSource($"Settings_{name}", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
 
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var xmlFiles = context.AdditionalTextsProvider.Where(at => at.Path.EndsWith(".xmlsettings"));
        context.RegisterSourceOutput(xmlFiles, ProcessSettingsFile);
    }
}
