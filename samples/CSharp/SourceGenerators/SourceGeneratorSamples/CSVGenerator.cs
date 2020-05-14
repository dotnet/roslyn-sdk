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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

#nullable enable

// CsvTextFileParser from https://github.com/22222/CsvTextFieldParser adding suppression rules for default VS config

#pragma warning disable IDE0008 // Use explicit type
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
                return (firstLineFields.Select(GetCsvFieldType).ToArray(), headerFields.Select(StringToValidPropertyName).ToArray(), firstLineFields);
            }
        }

        // Adds a class to the `CSV` namespace for each `csv` file passed in. The class has a static property
        // named `All` that returns the list of strongly typed objects generated on demand at first access.
        // There is the slight chance of a race condition in a multi-thread program, but the result is relatively benign
        // , loading the collection multiple times instead of once. Measures could be taken to avoid that.
        public static string GenerateClassFile(string className, string csvText, string loadTime, bool cacheObjects)
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


            if(loadTime == "LoadTime.Startup")
            {
                sb.Append(@$"
        static {className}() {{ var x = All; }}
");
            }
            (string[] types, string[] names, string[]? fields)  = ExtractProperties(parser);
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

            if(cacheObjects) sb.Append(@"
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
                if(fields == null) continue;
                if(fields.Length < minLen) throw new Exception("Not enough fields in CSV file.");

                sb.AppendLine($"                c = new {className}();");
                string value = "";
                for (int i = 0; i < minLen; i++)
                {
                    // Wrap strings in quotes.
                    value = GetCsvFieldType(fields[i]) == "string" ? $"\"{fields[i].Trim().Trim(new char[] {'"'})}\"" : fields[i];
                    sb.AppendLine($"                c.{names[i]} = {value};");
                }
                sb.AppendLine("                l.Add(c);");

                fields = parser.ReadFields();
            } while (! (fields == null));

            sb.AppendLine("                _all = l;");
            sb.AppendLine("                return l;");
            
            // Close things (property, class, namespace)
            sb.Append("            }\n        }\n    }\n}\n");
            return sb.ToString();

        }
        

        static string StringToValidPropertyName(string s)
        {
            s = s.Trim();
            s = Char.IsLetter(s[0]) ? Char.ToUpper(s[0]) + s.Substring(1) : s;
            s = Char.IsDigit(s.Trim()[0]) ? "_" + s : s;
            s = new string(s.Select(ch => Char.IsDigit(ch) || Char.IsLetter(ch) ? ch : '_').ToArray());
            return s;
        }

        static IEnumerable<(string,string)> SourceFilesFromPath(string loadTime, bool cacheObjects, string path)
        {
            if(Directory.Exists(path))
            {
               return Directory.GetFiles(path, "*.csv").SelectMany(f => SourceFilesFromPath(loadTime, cacheObjects, path)); 
            }

            string className = Path.GetFileNameWithoutExtension(path);
            string csvText = File.ReadAllText(path);
            return new (string,string)[] { (Path.GetFileNameWithoutExtension(path), GenerateClassFile(className, csvText, loadTime, cacheObjects)) };
        }

        static IEnumerable<(string, string)> SourceFileFromPaths(IEnumerable<(string, bool, string[])> pathsData) =>
            pathsData.SelectMany(d => d.Item3.SelectMany(p => SourceFilesFromPath(d.Item1, d.Item2, p)));


        static IEnumerable<(string, bool, string[])> GetLoadOptions(Compilation compilation)
        {
            // Get all CSV attributes
            IEnumerable<SyntaxNode>? allNodes = compilation.SyntaxTrees.SelectMany(s => s.GetRoot().DescendantNodes());
            var allAttributes = allNodes.Where((d) => d.IsKind(SyntaxKind.Attribute)).OfType<AttributeSyntax>();
            var attributes = allAttributes.Where(d => d.Name.ToString() == "CsvFileLoadOptions")
                .ToImmutableArray();

            foreach (var att in attributes)
            {
                string loadTime = "";
                bool cacheObjects = true;
                List<string> paths = new List<string>();
                if(att.ArgumentList == null) throw new Exception("Constructor of attribute must have arguments");

                var m = compilation.GetSemanticModel(att.SyntaxTree);
                foreach (var arg in att.ArgumentList.Arguments)
                {
                    var expr = arg.Expression;

                    var t = m.GetTypeInfo(expr);
                    var v = m.GetConstantValue(expr);
                    if(v.HasValue) {
                        if(t.Type!.Name == "Boolean") cacheObjects = (bool)v.Value;
                        if(t.Type.Name == "String") paths.Add((string)v.Value);
                    } else
                    {
                        string s = expr.ToString();
                        if(s.StartsWith("LoadTime."))
                        {
                            loadTime = s;
                        }
                    }
                }
                yield return (loadTime, cacheObjects, paths.ToArray());
            }
        }

        public void Execute(SourceGeneratorContext context)
        {
            string attributeSource = @"
namespace CSV {
    public enum LoadTime { Compilation, Startup, OnDemand }

    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple=true)]
    public class CsvFileLoadOptionsAttribute: System.Attribute
    {
        public LoadTime LoadTime { get; }
        public bool CacheObjects { get; }
        public string[] Paths { get; }
        public CsvFileLoadOptionsAttribute(LoadTime loadTime = LoadTime.Compilation, bool cacheObjects = true, params string[] paths)
            => (LoadTime, CacheObjects, Paths) = (loadTime, cacheObjects, paths);
    }
}
";

            context.AddSource("Cvs_MainAttributes__", SourceText.From(attributeSource, Encoding.UTF8));

            var options = GetLoadOptions(context.Compilation);
            var nameCodeSequence = SourceFileFromPaths(options);
            foreach (var (name, code) in nameCodeSequence)
                context.AddSource($"Csv_{name}", SourceText.From(code, Encoding.UTF8));
        }

        public void Initialize(InitializationContext context)
        {
        }
    }
}
#pragma warning restore IDE0008 // Use explicit type
