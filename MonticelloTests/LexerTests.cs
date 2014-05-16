using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Monticello.Parsing;

namespace MonticelloTests
{
    [TestClass]
    public class LexerTests
    {
        private Token GetTok(string input)
        {
            var lexer = new Lexer(input);
            return lexer.Read();
        }

        [TestMethod]
        public void TestNextChar()
        {
            var lexer = new Lexer("foo bar");
            Assert.AreEqual('f', lexer.NextChar());
            Assert.AreEqual('o', lexer.NextChar());
            Assert.AreEqual('o', lexer.NextChar());

            lexer.SkipWs();
            Assert.AreEqual('b', lexer.NextChar());
        }

        [TestMethod]
        public void TestPeek()
        {
            var lexer = new Lexer("fot bar ;;++=");
            Assert.AreEqual('f', lexer.Peek());
            Assert.AreEqual('f', lexer.Peek());
            lexer.NextChar();
            Assert.AreEqual('o', lexer.Peek());
            Assert.AreEqual('o', lexer.Peek());
        }

        [TestMethod]
        public void TestEof()
        {
            var lexer = new Lexer("");
            var t = lexer.Read();
            Assert.AreEqual(Sym.Eof, t.Sym);
        }

        [TestMethod]
        public void TestEof2()
        {
            var lexer = new Lexer("   ");
            var t = lexer.Read();
            Assert.AreEqual(Sym.Eof, t.Sym);
        }

        [TestMethod]
        public void TestIntLiteral()
        {
            var lexer = new Lexer("33");
            var t = lexer.Read();
            Assert.AreEqual(Sym.IntLiteral, t.Sym);
            Assert.AreEqual("33", t.Value);
            Assert.AreEqual(0, t.Col);
        }

        [TestMethod]
        public void TestIntLiteral2()
        {
            var lexer = new Lexer("    423455   ");
            var t = lexer.Read();
            Assert.AreEqual(Sym.IntLiteral, t.Sym);
            Assert.AreEqual("423455", t.Value);
            Assert.AreEqual(4, t.Col);
        }

        [TestMethod]
        public void TestFloatLiteral()
        {
            var lexer = new Lexer("42.3");
            var t = lexer.Read();
            Assert.AreEqual(Sym.FloatLiteral, t.Sym);
            Assert.AreEqual("42.3", t.Value);
        }

        [TestMethod]
        public void TestMultipleNumericLiterals()
        {
            var lexer = new Lexer("42 45.6");
            var t = lexer.Read();
            Assert.AreEqual(Sym.IntLiteral, t.Sym);
            Assert.AreEqual("42", t.Value);
            Assert.AreEqual(0, t.Col);

            t = lexer.Read();
            Assert.AreEqual(Sym.FloatLiteral, t.Sym);
            Assert.AreEqual("45.6", t.Value);
            Assert.AreEqual(3, t.Col);
        }

        [TestMethod]
        public void TestKws()
        {
            var t = GetTok("class");
            Assert.AreEqual(Sym.KwClass, t.Sym);

            t = GetTok("namespace");
            Assert.AreEqual(Sym.KwNamespace, t.Sym);
        }

        [TestMethod]
        public void TestOperators()
        {
            var t = GetTok("=");
            Assert.AreEqual(Sym.AssignEqual, t.Sym);

            t = GetTok("==");
            Assert.AreEqual(Sym.EqualEqual, t.Sym);

            t = GetTok(";");
            Assert.AreEqual(Sym.Semicolon, t.Sym);
        }

        [TestMethod]
        public void TestDelimitsToks1()
        {
            var lexer = new Lexer("foo;");
            var t = lexer.Read();
            Assert.AreEqual(Sym.Id, t.Sym);
            Assert.AreEqual("foo", t.Value);

            t = lexer.Read();
            Assert.AreEqual(Sym.Semicolon, t.Sym);
        }

        [TestMethod]
        public void TestDelimitsToks2()
        {
            var lexer = new Lexer(".;+");
            var t = lexer.Read();
            Assert.AreEqual(Sym.Dot, t.Sym);
            t = lexer.Read();
            Assert.AreEqual(Sym.Semicolon, t.Sym);
            Assert.AreEqual('+', lexer.Peek());
            t = lexer.Read();
            Assert.AreEqual(Sym.Plus, t.Sym);

            t = lexer.Read();
            Assert.AreEqual(Sym.Eof, t.Sym);
            t = lexer.Read();
            Assert.AreEqual(Sym.Eof, t.Sym);
        }
    }
}
