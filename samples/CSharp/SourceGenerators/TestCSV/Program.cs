using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Pluralize.NET;
using static System.Console;

namespace CSV {
    public class Foo2
    {
        [Name("id")]
        public int Id { get; set; }
        [Name("name")]
        public string Name { get; set; }
    }

    public static class CsvFiles
    {
        public static string GetType(string exemplar) => exemplar switch
        {
            _ when Boolean.TryParse(exemplar, out _)    => "bool",
            _ when int.TryParse(exemplar, out _)        => "int",
            _ when double.TryParse(exemplar, out _)     => "double",
            _                                           => "string"
        };
        public static IEnumerable<(string, string)> GetHeader(string csvText)
        {
            using TextReader reader = new StringReader(csvText);
            using CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Read();
            csv.ReadHeader();
            csv.Read();

            string[] h = csv.Context.HeaderRecord;
            for (int i = 0; i < h.Length; i++)
            {
                yield return (GetType(csv.GetField(i)), h[i]); 
            }

        }
        public static IEnumerable<Foo2> ReadFoos()
        {
            using StreamReader reader = new StreamReader("./Foos.csv", Encoding.UTF8);
            using CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            while(csv.Read()) yield return csv.GetRecord<Foo2>();
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
        public static string GenerateCSVClass(string fileName, string csvText)
        {
            IPluralize pluralizer = new Pluralizer();
            string className = pluralizer.IsSingular(fileName)
                               ? fileName
                               : pluralizer.Singularize(fileName);
            string startFile = @"
namespace CSV {
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Pluralize.NET;
using static System.Console;

";
            string startClass = $"public class {className} {{\n";
            StringBuilder props = new StringBuilder();
            foreach ((string type, string name) in GetHeader(csvText))
            {
                props.Append($"    [Name(\"{name}\")]\n");
                props.Append($"    public {type} {UppercaseFirst(name)} {{ get; set;}}\n");
            }
            props.Append("\n");
            props.Append($"    public static IEnumerable<{className}> Read{pluralizer.Pluralize(className)}() {{\n");
            props.Append($@"
        using TextReader reader = new StringReader(@""{csvText}"");
        using CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        while(csv.Read()) yield return csv.GetRecord<Foo>();
    }}
");
            return $"{startFile}{startClass}{props.ToString()}\n}}\n}}"; 

        }
    }
    class Program {
        static void Main(string[] args) {
            //CsvFiles.GetHeader("./Foos.csv").ToList().ForEach(s => { WriteLine(s); });
            CsvFiles.ReadFoos().ToList().ForEach(f => { WriteLine(f.Name);});
            WriteLine(CsvFiles.GenerateCSVClass(Path.GetFileNameWithoutExtension("./Foos.csv"), File.ReadAllText("./Foos.csv")));
        }
    }
}
