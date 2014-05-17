using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Monticello.Parsing;

namespace MonticelloTests
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void TestQualifiedId()
        {
            var parser = new Parser("System.Collections.Generic.List");
            var id = parser.ParseQualifiedId();

            Assert.IsNotNull(id);
            Assert.IsTrue(id.PartsAre("System", "Collections", "Generic", "List"));
        }

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

        [TestMethod]
        public void TestGlobalAttrs1()
        {
            var parser = new Parser("[assembly: CLSCompliant]");
            var attrs = parser.ParseGlobalAttrs();
            Assert.AreEqual(1, attrs.Count);
        }

        [TestMethod]
        public void TestGlobalAttrs2()
        {
            var parser = new Parser("[module: FooAttribute, Something.Else.BarAttribute]");
            var attrs = parser.ParseGlobalAttrs();
            Assert.AreEqual(1, attrs.Count);

            var sect = attrs[0];
            var fa = sect.Attrs[0];
            var ba = sect.Attrs[1];

            Assert.IsTrue(fa.AttrTypeName.PartsAre("FooAttribute"));
            Assert.IsTrue(ba.AttrTypeName.PartsAre("Something", "Else", "BarAttribute"));
        }
    }
}
