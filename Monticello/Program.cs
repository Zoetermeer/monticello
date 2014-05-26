using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Monticello.Parsing;

namespace Monticello
{
    [System.Diagnostics.DebuggerDisplay("{Method}")]
    public delegate void Foo(int arg);

    class Program
    {
        static void Whatever(int x)
        {
            Console.Write(x);
        }

        static void Main(string[] args)
        {
            Foo f = (i) => Console.WriteLine(i);
            Foo g = (i) => Console.Write(i);
            Foo h = Whatever;

            Console.WriteLine(f.ToString());
        }
    }
}
