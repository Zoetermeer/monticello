using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Monticello.Parsing;

namespace MonticelloTests
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void TestUsings1()
        {
            string input = "using System;";
            var cu = Parser.Parse(input);

            Assert.AreEqual(1, cu.Usings.Count);
            var ud = cu.Usings[0];
            var und = ud as UsingNamespaceDirective;
            Assert.IsNotNull(und);
            Assert.AreEqual("System", und.NamespaceName.Parts[0].Spelling.Value);
        }

        [TestMethod]
        public void TestUsings2()
        {
            string input = "using System; using System.Text; ";
            var cu = Parser.Parse(input);

            Assert.AreEqual(2, cu.Usings.Count);
            var ud = cu.Usings[0];
            var und = ud as UsingNamespaceDirective;
            Assert.IsNotNull(und);
            Assert.IsTrue(und.NamespaceName.PartsAre("System"));

            und = cu.Usings[1] as UsingNamespaceDirective;
            Assert.IsNotNull(und);
            Assert.IsTrue(und.NamespaceName.PartsAre("System", "Text"));
        }

        [TestMethod]
        public void TestUsings3()
        {
            string input = "using System; using foo = System.Text.StringBuilder;";
            var cu = Parser.Parse(input);

            Assert.AreEqual(2, cu.Usings.Count);
            var uad = cu.Usings[1] as UsingAliasDirective;
            Assert.IsNotNull(uad);
            Assert.AreEqual("foo", uad.Alias.Spelling.Value);
            Assert.IsTrue(uad.NamespaceOrTypeName.PartsAre("System", "Text", "StringBuilder"));
        }
    }
}
