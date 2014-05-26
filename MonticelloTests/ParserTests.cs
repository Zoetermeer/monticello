﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Monticello.Parsing;

namespace MonticelloTests
{
    [TestClass]
    public class ParserTests
    {
        private void AssertExp(string input, string expected)
        {
            var parser = new Parser(input);
            Assert.AreEqual(expected, parser.ParseExp().ToString());
        }

        [TestMethod]
        public void TestPosResetsOnFail()
        {
            var parser = new Parser("4 + 5");
            Assert.AreEqual(0, parser.Pos);
            Assert.IsNull(parser.ApplyRule(parser.ParseAssignmentExp));
            Assert.AreEqual(0, parser.Pos);
        }

        [TestMethod]
        public void TestQualifiedId()
        {
            var parser = new Parser("System.Collections.Generic.List");
            var id = parser.ParseQualifiedId();

            Assert.IsNotNull(id);
            Assert.IsTrue(id.PartsAre("System", "Collections", "Generic", "List"));
        }

        [TestMethod]
        public void TestIds()
        {
            AssertExp("System.Foo.Bar", "(qualified-id System.Foo.Bar)");
        }

        [TestMethod]
        public void TestUsing1()
        {
            string input = "using System.Text;";
            var ud = new Parser(input).ParseUsingDirective();
            UsingNamespaceDirective und = ud as UsingNamespaceDirective;
            Assert.IsNotNull(und);
            Assert.IsTrue(und.NamespaceName.PartsAre("System", "Text"));
        }

        [TestMethod]
        public void TestUsing2()
        {
            string input = "using System.Foo; using System.Bar.Whatever ;";
            var parser = new Parser(input);
            UsingNamespaceDirective und = parser.ParseUsingDirective() as UsingNamespaceDirective;
            Assert.IsNotNull(und);
            Assert.IsTrue(und.NamespaceName.PartsAre("System", "Foo"));

            und = parser.ParseUsingDirective() as UsingNamespaceDirective;
            Assert.IsNotNull(und);
            Assert.IsTrue(und.NamespaceName.PartsAre("System", "Bar", "Whatever"));
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
        public void TestAttribute1()
        {
            var parser = new Parser("System.FooAttribute");
            var attr = parser.ParseAttribute();
            Assert.IsNotNull(attr);
            Assert.IsTrue(attr.AttrTypeName.PartsAre("System", "FooAttribute"));
        }

        [TestMethod]
        public void TestAttribute2()
        {
            var parser = new Parser("System.Foo.BarAttribute()");
            var attr = parser.ParseAttribute();
            Assert.IsNotNull(attr);
            Assert.IsTrue(attr.AttrTypeName.PartsAre("System", "Foo", "BarAttribute"));
            Assert.AreEqual(0, attr.Args.Count);
        }

        [TestMethod]
        public void TestAttribute3()
        {
            var parser = new Parser("System.Foo.BarAttribute(1, 2)");
            var attr = parser.ParseAttribute();
            Assert.IsNotNull(attr);
            Assert.IsTrue(attr.AttrTypeName.PartsAre("System", "Foo", "BarAttribute"));
            Assert.AreEqual(2, attr.Args.Count);

            Assert.IsTrue(attr.Args[0] is PositionalAttrArgument);
            Assert.IsTrue(attr.Args[1] is PositionalAttrArgument);
            Assert.IsTrue(attr.Args[0].Exp is IntLiteralExp);
            Assert.IsTrue(attr.Args[1].Exp is IntLiteralExp);
        }

        [TestMethod]
        public void TestAttribute4()
        {
            var parser = new Parser("Foo(SomeNumberArg=42)");
            var attr = parser.ParseAttribute();
            Assert.IsNotNull(attr);
            Assert.IsTrue(attr.AttrTypeName.PartsAre("Foo"));
            Assert.AreEqual(1, attr.Args.Count);

            var na = attr.Args[0] as NamedAttrArgument;
            Assert.IsNotNull(na);
            Assert.AreEqual("SomeNumberArg", na.Name.Spelling.Value);
            Assert.IsTrue(na.Exp is IntLiteralExp);
        }

        [TestMethod]
        public void TestAttribute5()
        {
            var parser = new Parser("Foo(42, SomeNumberArg=43)");
            var attr = parser.ParseAttribute();
            Assert.IsNotNull(attr);
            Assert.IsTrue(attr.AttrTypeName.PartsAre("Foo"));
            Assert.AreEqual(2, attr.Args.Count);

            var pa = attr.Args[0] as PositionalAttrArgument;
            var na = attr.Args[1] as NamedAttrArgument;
            Assert.IsNotNull(pa);
            Assert.IsNotNull(na);

            Assert.IsTrue(pa.Exp is IntLiteralExp);
            Assert.AreEqual("SomeNumberArg", na.Name.Spelling.Value);
            Assert.IsTrue(na.Exp is IntLiteralExp);
        }

        [TestMethod]
        public void TestAttribute6()
        {
            var parser = new Parser("FooAttribute(1, 2, Name=\"whatever\")");
            var attr = parser.ParseAttribute();
            Assert.IsNotNull(attr);
            Assert.IsTrue(attr.AttrTypeName.PartsAre("FooAttribute"));

            Assert.AreEqual(3, attr.Args.Count);
            var pa1 = attr.Args[0] as PositionalAttrArgument;
            var pa2 = attr.Args[1] as PositionalAttrArgument;
            var na = attr.Args[2] as NamedAttrArgument;

            Assert.IsNotNull(pa1);
            Assert.IsNotNull(pa2);
            Assert.IsNotNull(na);

            Assert.IsTrue(pa1.Exp is IntLiteralExp);
            Assert.IsTrue(pa2.Exp is IntLiteralExp);
            Assert.AreEqual("Name", na.Name.Spelling.Value);
            Assert.IsTrue(na.Exp is StringLiteralExp);
        }

        [TestMethod]
        public void TestGlobalAttr1()
        {
            var parser = new Parser("[module: FooAttribute(1, 2, Name=\"whatever\")]");
            var sect = parser.ParseGlobalAttrSection();
            Assert.IsNotNull(sect);
            Assert.AreEqual(1, sect.Attrs.Count);
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

        [TestMethod]
        public void TestGlobalAttrs3()
        {
            var parser = new Parser("[assembly: Foo(1, 2, Arg=\"blah\"), Bar(Prop='c')]");
            var sections = parser.ParseGlobalAttrs();
            Assert.AreEqual(1, sections.Count);

            var sect = sections[0];
            Assert.AreEqual(2, sect.Attrs.Count);
            var fa = sect.Attrs[0];
            var fb = sect.Attrs[1];

            Assert.IsTrue(fa.AttrTypeName.PartsAre("Foo"));
            Assert.IsTrue(fb.AttrTypeName.PartsAre("Bar"));

            Assert.AreEqual(3, fa.Args.Count);
            Assert.AreEqual(1, fb.Args.Count);

            Assert.IsTrue(fa.Args[0] is PositionalAttrArgument);
            Assert.IsTrue(fa.Args[1] is PositionalAttrArgument);
            Assert.IsTrue(fa.Args[2] is NamedAttrArgument);

            NamedAttrArgument bArg = fb.Args[0] as NamedAttrArgument;
            Assert.IsNotNull(bArg);
            Assert.AreEqual("Prop", bArg.Name.Spelling.Value);
            Assert.IsTrue(bArg.Exp is CharLiteralExp);
        }

        [TestMethod]
        public void TestGlobalAttrs4()
        {
            var parser = new Parser("[assembly: FooAttribute(1, 2, Name=\"whatever\")]");
            var attrs = parser.ParseGlobalAttrs();
            Assert.AreEqual(1, attrs.Count);

            var sect = attrs[0];
            Assert.AreEqual(1, sect.Attrs.Count);
            var fa = sect.Attrs[0];
            Assert.AreEqual(3, fa.Args.Count);
            Assert.IsTrue(fa.Args[0] is PositionalAttrArgument);
            Assert.IsTrue(fa.Args[1] is PositionalAttrArgument);

            NamedAttrArgument na = fa.Args[2] as NamedAttrArgument;
            Assert.IsNotNull(na);
            Assert.AreEqual("Name", na.Name.Spelling.Value);
        }

        [TestMethod]
        public void TestGlobalAttrs5()
        {
            var parser = new Parser("[assembly: Foo(1, 2)]  [module: Bar(\"\", \"\")]");
            var sects = parser.ParseGlobalAttrs();

            Assert.AreEqual(2, sects.Count);
            var sect1 = sects[0];
            var sect2 = sects[1];
            Assert.AreEqual(1, sect1.Attrs.Count);
            Assert.AreEqual(1, sect2.Attrs.Count);
        }

        [TestMethod]
        public void TestLiterals()
        {
            var parser = new Parser("1");
            var e = parser.ParseExp();

            Assert.IsNotNull(e);
            IntLiteralExp le = e as IntLiteralExp;
            Assert.IsNotNull(le);
            Assert.AreEqual(1, le.Value);
        }

        [TestMethod]
        public void TestAdditives()
        {
            var parser = new Parser("1 + 2");
            var e = parser.ParseExp();
            Assert.AreEqual("(add (int 1) (int 2))", e.ToString());

            Assert.IsNotNull(e);
            AdditiveExp ae = e as AdditiveExp;
            Assert.IsNotNull(ae);

            IntLiteralExp lhs = ae.Lhs as IntLiteralExp;
            IntLiteralExp rhs = ae.Rhs as IntLiteralExp;
            Assert.IsNotNull(lhs);
            Assert.IsNotNull(rhs);

            Assert.AreEqual(1, lhs.Value);
            Assert.AreEqual(2, rhs.Value);
        }

        [TestMethod]
        public void TestConditionals()
        {
            AssertExp(
                "1 + 2 || false",
                "(conditional-or (add (int 1) (int 2)) (bool false))");

            AssertExp(
                "false && true || false",
                "(conditional-or (conditional-and (bool false) (bool true)) (bool false))");
        }

        [TestMethod]
        public void TestAssignments()
        {
            AssertExp("4 = 3", "(assign (int 4) Equal (int 3))");
            AssertExp("4 = 2 + 1", "(assign (int 4) Equal (add (int 2) (int 1)))");
            AssertExp("x = 3", "(assign (qualified-id x) Equal (int 3))");
        }

        [TestMethod]
        public void TestAdditive2()
        {
            AssertExp("4 + 5", "(add (int 4) (int 5))");
            AssertExp("4+5", "(add (int 4) (int 5))");
        }

        [TestMethod]
        public void TestAdditive3()
        {
            var parser = new Parser("4 + 5");
            var e = parser.ParseConditionalExp();
            Assert.AreEqual("(add (int 4) (int 5))", e.ToString());

            parser = new Parser("4 + 5 + 6");
            e = parser.ParseConditionalExp();
            Assert.AreEqual("(add (add (int 4) (int 5)) (int 6))", e.ToString());
        }

        [TestMethod]
        public void TestMultiplicative()
        {
            var parser = new Parser("4 * 5");
            var e = parser.ParseAdditiveExp();
            Assert.AreEqual("(mult (int 4) (int 5))", e.ToString());
        }

        [TestMethod]
        public void TestArith()
        {
            var parser = new Parser("4 + 5 * 6");
            var e = parser.ParseExp();
            Assert.AreEqual("(add (int 4) (mult (int 5) (int 6)))", e.ToString());
        }

        [TestMethod]
        public void TestUnary1()
        {
            var parser = new Parser("+ 33");
            var e = parser.ParseExp();
            Assert.AreEqual("(unary + (int 33))", e.ToString());
        }

        [TestMethod]
        public void TestInvocation()
        {
            AssertExp("Foo()", "(invocation (qualified-id Foo) ())");
        }
    }
}
