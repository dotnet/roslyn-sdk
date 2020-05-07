using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static System.Console;
using System.Text;
using System.Diagnostics;
using NotVisualBasic.FileIO;

#nullable enable

// CsvTextFileParser from https://github.com/22222/CsvTextFieldParser adding suppression rules for default VS config

namespace CsvGenerator
{
    [Generator]
    public class CSVGenerator : ISourceGenerator
    {
        // Guesses type of property for the object from the value of a csv field
        public static string GetCsvFieldType(string exemplar) => exemplar switch
        {
            _ when Boolean.TryParse(exemplar, out _)    => "bool",
            _ when int.TryParse(exemplar, out _)        => "int",
            _ when double.TryParse(exemplar, out _)     => "double",
            _                                           => "string"
        };

        // Examines the header row and the first row in the csv file to gather all header types and names
        // Also it returns the first row of data, because it must be read to figure out the types,
        // As the CsvTextFieldParser cannot 'Peek' ahead of one line. If there is no first line,
        // it consider all properties as strings. The generator returns an empty list of properly
        // typed objects in such cas. If the file is completely empty, an error is generated.
        public static (string[], string[], string[]?) ExtractProperties(CsvTextFieldParser parser)
        {
            string[]? headerFields = parser.ReadFields();
            if(headerFields == null) throw new Exception("Empty csv file!");

            string[]? firstLineFields = parser.ReadFields();
            if(firstLineFields == null)
            {
                return (Enumerable.Repeat("string", headerFields.Length).ToArray(), headerFields, firstLineFields);
            } else
            {
                return (firstLineFields.Select(GetCsvFieldType).ToArray(), headerFields.Select(UppercaseFirst).ToArray(), firstLineFields);
            }
        }

        // Adds a class to the `CSV` namespace for each `csv` file passed in. The class has a static property
        // named `All` that returns the list of strongly typed objects generated on demand at first access.
        // There is the slight chance of a race condition in a multi-thread program, but the result is relatively benign
        // , loading the collection multiple times instead of once. Measures could be taken to avoid that.
        public static string GenerateClassFile(string className, string csvText)
        {
            StringBuilder sb = new StringBuilder();
            using CsvTextFieldParser parser = new CsvTextFieldParser(new StringReader(csvText));

            //// Usings
            sb.Append( @"
#nullable enable
namespace CSV {
    using System.Collections.Generic;

");
            //// Class Definition
            sb.Append($"    public class {className} {{\n");


            (string[] types, string[] names, string[]? fields)  = ExtractProperties(parser);
            int minLen = Math.Min(types.Length, names.Length);

            for (int i = 0; i < minLen; i++)
            {
                sb.AppendLine($"        public {types[i]} {UppercaseFirst(names[i])} {{ get; set;}} = default!;");
            }
            sb.Append("\n");

            //// Loading data
            sb.AppendLine($"        static IEnumerable<{className}>? _all = null;");
            sb.Append($@"
        public static IEnumerable<{className}> All {{
            get {{
            if(_all != null)
                return _all;

            List<{className}> l = new List<{className}>();
            {className} c;
");

            // This awkwardness comes from having to pre-read one row to figure out the types of props.
            do
            {
                if(fields == null) continue;
                if(fields.Length < minLen) throw new Exception("Not enough fields in CSV file.");

                sb.AppendLine($"            c = new {className}();");
                string value = "";
                for (int i = 0; i < minLen; i++)
                {
                    // Wrap strings in quotes.
                    value = GetCsvFieldType(fields[i]) == "string" ? $"\"{fields[i]}\"" : fields[i];
                    sb.AppendLine($"            c.{names[i]} = {value};");
                }
                sb.AppendLine("            l.Add(c);");

                fields = parser.ReadFields();
            } while (! (fields == null));

            sb.AppendLine("           _all = l;");
            sb.AppendLine("           return l;");
            
            // Close things (property, class, namespace)
            sb.Append("            }\n        }\n    }\n}\n");
            return sb.ToString();

        }
        

        // from: https://www.dotnetperls.com/uppercase-first-letter
        static string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public void Execute(SourceGeneratorContext context)
        {

            IEnumerable<AdditionalText> csvFiles = context.AdditionalFiles.Where(at => at.Path.EndsWith(".csv"));
            foreach (AdditionalText csvFile in csvFiles)
            {
                ProcessCsvFile(csvFile, context);
            }
        }

        // Generates a new class for each `csv` file. Names the class as the file made uppercase.
        // We could use a pluralizer to create a better name.
        private void ProcessCsvFile(AdditionalText csvFile, SourceGeneratorContext context)
        {
            SourceText? sourceText = csvFile.GetText(context.CancellationToken);
            if(sourceText == null) throw new Exception("SourceText cannot be null!");
            string csvText = sourceText.ToString();

            string className = Path.GetFileNameWithoutExtension(csvFile.Path);
            string csvClassText = GenerateClassFile(className, csvText);
            context.AddSource($"Csv_{className}", SourceText.From(csvClassText, Encoding.UTF8));
        }
     
        public void Initialize(InitializationContext context)
        {
        }
    }
}
