using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Console;

namespace GeneratedDemo
{
    class UseCsvGenerator
    {
        public static void Run()
        {
           CSV.Foos.All.ToList().ForEach(x => WriteLine($"<id={x.Id} name={x.Name}>"));
        }
    }
}
