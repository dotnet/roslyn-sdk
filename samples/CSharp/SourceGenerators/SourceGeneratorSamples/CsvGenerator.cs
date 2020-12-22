using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using NotVisualBasic.FileIO;

#nullable enable

// CsvTextFileParser from https://github.com/22222/CsvTextFieldParser adding suppression rules for default VS config

namespace CsvGenerator
{
    [Generator(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class CSVGenerator : ISourceGenerator
    {
        public enum CsvLoadType
        {
            Startup,
            OnDemand
        }

        // Guesses type of property for the object from the value of a csv field
        public static string GetCsvFieldType(string exemplar, string languageName) => (exemplar, languageName) switch
        {
            (_, LanguageNames.CSharp) when bool.TryParse(exemplar, out _) => "bool",
            (_, LanguageNames.CSharp) when int.TryParse(exemplar, out _) => "int",
            (_, LanguageNames.CSharp) when double.TryParse(exemplar, out _) => "double",
            (_, LanguageNames.CSharp) => "string",
            (_, LanguageNames.VisualBasic) when bool.TryParse(exemplar, out _) => "Boolean",
            (_, LanguageNames.VisualBasic) when int.TryParse(exemplar, out _) => "Integer",
            (_, LanguageNames.VisualBasic) when double.TryParse(exemplar, out _) => "Double",
            (_, LanguageNames.VisualBasic) => "String",
            _ => throw new NotSupportedException(),
        };

        // Examines the header row and the first row in the csv file to gather all header types and names
        // Also it returns the first row of data, because it must be read to figure out the types,
        // As the CsvTextFieldParser cannot 'Peek' ahead of one line. If there is no first line,
        // it consider all properties as strings. The generator returns an empty list of properly
        // typed objects in such cas. If the file is completely empty, an error is generated.
        public static (string[], string[], string[]?) ExtractProperties(CsvTextFieldParser parser, string languageName)
        {
            string[]? headerFields = parser.ReadFields();
            if (headerFields == null) throw new Exception("Empty csv file!");

            string[]? firstLineFields = parser.ReadFields();
            if (firstLineFields == null)
            {
                string fieldType = languageName == LanguageNames.CSharp ? "string" : "String";
                return (Enumerable.Repeat(fieldType, headerFields.Length).ToArray(), headerFields, firstLineFields);
            }
            else
            {
                return (firstLineFields.Select(field => GetCsvFieldType(field, languageName)).ToArray(), headerFields.Select(StringToValidPropertyName).ToArray(), firstLineFields);
            }
        }

        // Adds a class to the `CSV` namespace for each `csv` file passed in. The class has a static property
        // named `All` that returns the list of strongly typed objects generated on demand at first access.
        // There is the slight chance of a race condition in a multi-thread program, but the result is relatively benign
        // , loading the collection multiple times instead of once. Measures could be taken to avoid that.
        public static string GenerateClassFile(string languageName, string className, string csvText, CsvLoadType loadTime, bool cacheObjects)
        {
            StringBuilder sb = new StringBuilder();
            using CsvTextFieldParser parser = new CsvTextFieldParser(new StringReader(csvText));

            //// Usings
            if (languageName == LanguageNames.CSharp)
            {
                sb.Append(@"
#nullable enable
namespace CSV {
    using System.Collections.Generic;

");
            }
            else
            {
                sb.Append(@"
Imports System.Collections.Generic
Namespace Global.CSV
");
            }

            //// Class Definition
            if (languageName == LanguageNames.CSharp)
            {
                sb.Append($"    public class {className} {{\n");
            }
            else
            {
                sb.Append($@"    Public Class {className}
");
            }


            if (loadTime == CsvLoadType.Startup)
            {
                if (languageName == LanguageNames.CSharp)
                {
                    sb.Append(@$"
        static {className}() {{ var x = All; }}
");
                }
                else
                {
                    sb.Append(@$"
        Shared Sub New()
            Dim x = All
        End Sub
");
                }
            }

            (string[] types, string[] names, string[]? fields) = ExtractProperties(parser, languageName);
            int minLen = Math.Min(types.Length, names.Length);

            for (int i = 0; i < minLen; i++)
            {
                if (languageName == LanguageNames.CSharp)
                {
                    sb.AppendLine($@"        public {types[i]} {StringToValidPropertyName(names[i])} {{ get; set;}} = default!;
");
                }
                else
                {
                    sb.AppendLine($@"        Public Property {StringToValidPropertyName(names[i])} As {types[i]}
");
                }
            }

            //// Loading data
            if (languageName == LanguageNames.CSharp)
            {
                sb.Append($@"        static IEnumerable<{className}>? _all = null;

        public static IEnumerable<{className}> All {{
            get {{");

                if (cacheObjects)
                {
                    sb.Append(@"
                if(_all != null)
                    return _all;
");
                }

                sb.Append(@$"
                List<{className}> l = new List<{className}>();
                {className} c;
");
            }
            else
            {
                sb.Append($@"        Private Shared _all As IEnumerable(Of {className})

        Public Shared ReadOnly Property All As IEnumerable(Of {className})
            Get");

                if (cacheObjects)
                {
                    sb.Append(@"
                If _all IsNot Nothing Then
                    Return _all
                End If
");
                }

                sb.Append(@$"
                Dim l As New List(Of {className})()
                Dim c As {className}
");
            }

            // This awkwardness comes from having to pre-read one row to figure out the types of props.
            do
            {
                if (fields == null) continue;
                if (fields.Length < minLen) throw new Exception("Not enough fields in CSV file.");

                if (languageName == LanguageNames.CSharp)
                {
                    sb.AppendLine($"                c = new {className}();");
                }
                else
                {
                    sb.AppendLine($"                c = New {className}()");
                }

                string value = "";
                for (int i = 0; i < minLen; i++)
                {
                    if (languageName == LanguageNames.CSharp)
                    {
                        // Wrap strings in quotes.
                        value = GetCsvFieldType(fields[i], languageName) == "string" ? $"\"{fields[i].Trim().Trim(new char[] { '"' })}\"" : fields[i];
                        sb.AppendLine($"                c.{names[i]} = {value};");
                    }
                    else
                    {
                        // Wrap strings in quotes.
                        value = GetCsvFieldType(fields[i], languageName) == "String" ? $"\"{fields[i].Trim().Trim(new char[] { '"' })}\"" : fields[i];
                        sb.AppendLine($"                c.{names[i]} = {value}");
                    }
                }

                if (languageName == LanguageNames.CSharp)
                {
                    sb.AppendLine("                l.Add(c);");
                }
                else
                {
                    sb.AppendLine("                l.Add(c)");
                }

                fields = parser.ReadFields();
            } while (!(fields == null));

            if (languageName == LanguageNames.CSharp)
            {
                sb.Append($@"                _all = l;
                return l;
");
            }
            else
            {
                sb.Append($@"                _all = l
                Return l
");
            }

            // Close things (property, class, namespace)
            if (languageName == LanguageNames.CSharp)
            {
                sb.Append(@"            }
        }
    }
}
");
            }
            else
            {
                sb.Append(@"            End Get
        End Property
    End Class
End Namespace
");
            }

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

        static IEnumerable<(string, string)> SourceFilesFromAdditionalFile(string languageName, CsvLoadType loadTime, bool cacheObjects, AdditionalText file)
        {
            string className = Path.GetFileNameWithoutExtension(file.Path);
            string csvText = file.GetText()!.ToString();
            return new (string, string)[] { (className, GenerateClassFile(languageName, className, csvText, loadTime, cacheObjects)) };
        }

        static IEnumerable<(string, string)> SourceFilesFromAdditionalFiles(string languageName, IEnumerable<(CsvLoadType loadTime, bool cacheObjects, AdditionalText file)> pathsData)
            => pathsData.SelectMany(d => SourceFilesFromAdditionalFile(languageName, d.loadTime, d.cacheObjects, d.file));

        static IEnumerable<(CsvLoadType, bool, AdditionalText)> GetLoadOptions(GeneratorExecutionContext context)
        {
            foreach (AdditionalText file in context.AdditionalFiles)
            {
                if (Path.GetExtension(file.Path).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    // are there any options for it?
                    context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.additionalfiles.CsvLoadType", out string? loadTimeString);
                    Enum.TryParse(loadTimeString, ignoreCase: true, out CsvLoadType loadType);

                    context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.additionalfiles.CacheObjects", out string? cacheObjectsString);
                    bool.TryParse(cacheObjectsString, out bool cacheObjects);

                    yield return (loadType, cacheObjects, file);
                }
            }
        }

        public void Execute(GeneratorExecutionContext context)
        {
            IEnumerable<(CsvLoadType, bool, AdditionalText)> options = GetLoadOptions(context);
            IEnumerable<(string, string)> nameCodeSequence = SourceFilesFromAdditionalFiles(context.Compilation.Language, options);
            foreach ((string name, string code) in nameCodeSequence)
                context.AddSource($"Csv_{name}", SourceText.From(code, Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
#pragma warning restore IDE0008 // Use explicit type
