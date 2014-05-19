/*
 * This file is used only for playing around with the MS C# compiler 
 * (to see how it handles weird syntax/type errors, etc.).
 */ 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Monticello.Parsing.FooAttribute(1, 2, Name = "whatever")]

namespace Monticello.Parsing {



    [AttributeUsage(AttributeTargets.All)]
    public class FooAttribute : Attribute {
        public FooAttribute(int x, int y)
        {

        }

        public string Name { get; set; }
    }

    class Sandbox {
    }
}
