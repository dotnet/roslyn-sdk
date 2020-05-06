namespace CSV {
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using static System.Console;

public class FoosItem {
    [Name("id")]
    public int Id { get; set;}
    [Name("name")]
    public string Name { get; set;}

    public static IEnumerable<FoosItem> ReadFoos() {

        using TextReader reader = new StringReader(@"id,name
1,one
2,two");
        using CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        while(csv.Read()) yield return csv.GetRecord<FoosItem>();
    }

}
}
