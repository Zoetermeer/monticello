using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monticello.Parsing
{
    public class Token
    {
        public Sym Sym { get; set; }
        public string File { get; set; }
        public int Line { get; set; }
        public int Col { get; set; }
        public string Value { get; set; } 

        public bool Is(Sym expected)
        {
            return this.Sym == expected;
        }

        public override string ToString()
        {
            return Sym.ToString();
        }
    }
}
