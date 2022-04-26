using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CsvGenerator;

[Generator]
public class CSVGenerator : IIncrementalGenerator
{
    public enum CsvLoadType
    {
        Startup,
        OnDemand
    }

    // Guesses type of property for the object from the value of a csv field
    public static string GetCsvFieldType(string exemplar) => exemplar switch
    {
        _ when bool.TryParse(exemplar, out _) => "bool",
        _ when int.TryParse(exemplar, out _) => "int",
        _ when double.TryParse(exemplar, out _) => "double",
        _ => "string"
    };

    // Examines the header row and the first row in the csv file to gather all header types and names
    // Also it returns the first row of data, because it must be read to figure out the types,
    // As the CsvTextFieldParser cannot 'Peek' ahead of one line. If there is no first line,
    // it consider all properties as strings. The generator returns an empty list of properly
    // typed objects in such cas. If the file is completely empty, an error is generated.
    public static (string[], string[], string[]?) ExtractProperties(CsvTextFieldParser parser)
    {
        string[]? headerFields = parser.ReadFields();
        if (headerFields == null) throw new Exception("Empty csv file!");

        string[]? firstLineFields = parser.ReadFields();
        if (firstLineFields == null)
        {
            return (Enumerable.Repeat("string", headerFields.Length).ToArray(), headerFields, firstLineFields);
        }
        else
        {
            return (firstLineFields.Select(GetCsvFieldType).ToArray(), headerFields.Select(StringToValidPropertyName).ToArray(), firstLineFields);
        }
    }

    // Adds a class to the `CSV` namespace for each `csv` file passed in. The class has a static property
    // named `All` that returns the list of strongly typed objects generated on demand at first access.
    // There is the slight chance of a race condition in a multi-thread program, but the result is relatively benign
    // , loading the collection multiple times instead of once. Measures could be taken to avoid that.
    public static string GenerateClassFile(string className, string csvText, CsvLoadType loadTime, bool cacheObjects)
    {
        StringBuilder sb = new ();
        using CsvTextFieldParser parser = new (new StringReader(csvText));

        //// Usings
        sb.Append(@"
#nullable enable
namespace CSV {
    using System.Collections.Generic;

");
        //// Class Definition
        sb.Append($"    public class {className} {{\n");


        if (loadTime == CsvLoadType.Startup)
        {
            sb.Append(@$"
        static {className}() {{ var x = All; }}
");
        }
        (string[] types, string[] names, string[]? fields) = ExtractProperties(parser);
        int minLen = Math.Min(types.Length, names.Length);

        for (int i = 0; i < minLen; i++)
        {
            sb.AppendLine($"        public {types[i]} {StringToValidPropertyName(names[i])} {{ get; set;}} = default!;");
        }
        sb.Append("\n");

        //// Loading data
        sb.AppendLine($"        static IEnumerable<{className}>? _all = null;");
        sb.Append($@"
        public static IEnumerable<{className}> All {{
            get {{");

        if (cacheObjects) sb.Append(@"
                if(_all != null)
                    return _all;
");
        sb.Append(@$"

                List<{className}> l = new List<{className}>();
                {className} c;
");

        // This awkwardness comes from having to pre-read one row to figure out the types of props.
        do
        {
            if (fields == null) continue;
            if (fields.Length < minLen) throw new Exception("Not enough fields in CSV file.");

            sb.AppendLine($"                c = new {className}();");
            string value = "";
            for (int i = 0; i < minLen; i++)
            {
                // Wrap strings in quotes.
                value = GetCsvFieldType(fields[i]) == "string" ? $"\"{fields[i].Trim().Trim(new char[] { '"' })}\"" : fields[i];
                sb.AppendLine($"                c.{names[i]} = {value};");
            }
            sb.AppendLine("                l.Add(c);");

            fields = parser.ReadFields();
        } while (!(fields == null));

        sb.AppendLine("                _all = l;");
        sb.AppendLine("                return l;");

        // Close things (property, class, namespace)
        sb.Append("            }\n        }\n    }\n}\n");
        return sb.ToString();

    }


    static string StringToValidPropertyName(string s)
    {
        s = s.Trim();
        s = char.IsLetter(s[0]) ? char.ToUpper(s[0]) + s.Substring(1) : s;
        s = char.IsDigit(s.Trim()[0]) ? "_" + s : s;
        s = new string(s.Select(ch => char.IsDigit(ch) || char.IsLetter(ch) ? ch : '_').ToArray());
        return s;
    }

    static IEnumerable<(string, string)> SourceFilesFromAdditionalFile(CsvLoadType loadTime, bool cacheObjects, AdditionalText file)
    {
        string className = Path.GetFileNameWithoutExtension(file.Path);
        string csvText = file.GetText()!.ToString();
        return new (string, string)[] { (className, GenerateClassFile(className, csvText, loadTime, cacheObjects)) };
    }

    public (CsvLoadType, bool, AdditionalText) GetLoadOptionProvider(
        (AdditionalText file, AnalyzerConfigOptionsProvider config) source,
        CancellationToken ct)
    {
        var options = source.config.GetOptions(source.file);
        
        options.TryGetValue("build_metadata.additionalfiles.CsvLoadType", out string? loadTimeString);
        Enum.TryParse(loadTimeString, ignoreCase: true, out CsvLoadType loadType);

        options.TryGetValue("build_metadata.additionalfiles.CacheObjects", out string? cacheObjectsString);
        bool.TryParse(cacheObjectsString, out bool cacheObjects);

        return (loadType, cacheObjects, source.file);
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<AdditionalText> files
            = context.AdditionalTextsProvider.Where( static file => file.Path.EndsWith(".csv"));

        IncrementalValuesProvider<(AdditionalText, AnalyzerConfigOptionsProvider)>
            combined = files.Combine(context.AnalyzerConfigOptionsProvider);

        IncrementalValuesProvider<(CsvLoadType, bool, AdditionalText)> transformed
            = combined.Select(GetLoadOptionProvider); 

        var selector = static ((CsvLoadType ltype, bool cache, AdditionalText text)source, CancellationToken ctoken)
            => SourceFilesFromAdditionalFile(source.ltype, source.cache, source.text);
        IncrementalValuesProvider<(string name, string code)> nameAndContent = transformed.SelectMany(selector);

        context.RegisterSourceOutput(nameAndContent, (spc, nameAndContent) =>
            spc.AddSource($"{nameAndContent.name}.g.cs", nameAndContent.code)); 
    }
}
