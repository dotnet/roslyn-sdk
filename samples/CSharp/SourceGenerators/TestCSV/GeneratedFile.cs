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

public class Foo {
    [Name("id")]
    public int Id { get; set;}
    [Name("name")]
    public string Name { get; set;}

    public static IEnumerable<Foo> ReadFoos() {

        using StreamReader reader = new StreamReader("./Foos.csv", Encoding.UTF8);
        using CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        while(csv.Read()) yield return csv.GetRecord<Foo>();
    }

}
}
