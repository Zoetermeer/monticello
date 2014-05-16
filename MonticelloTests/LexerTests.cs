using System;
using System.Collections.Generic;
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

        private Sym[] ReadToEnd(string input)
        {
            var lexer = new Lexer(input);
            var ls = new List<Sym>();
            while (lexer.CanRead) {
                ls.Add(lexer.Read().Sym);
            }

            return ls.ToArray();
        }

        private void AssertListsEqual<T>(T[] exp, T[] actual)
        {
            if (exp.Length != actual.Length) {
                Assert.Fail("Expected {0}, but got {1}", exp, actual);
            }

            for (int i = 0; i < exp.Length; i++) {
                if (!exp[i].Equals(actual[i]))
                    Assert.Fail("Expected {0}, but got {1}", exp, actual);
            }
        }

        private void AssertSymsMatch(string input, params Sym[] syms)
        {
            AssertListsEqual(syms, ReadToEnd(input));
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
        public void TestHexIntLiteral()
        {
            var lexer = new Lexer("0x43334534abced");
            var t = lexer.Read();
            Assert.AreEqual(Sym.HexIntLiteral, t.Sym);
            Assert.AreEqual("0x43334534abced", t.Value);
        }

        [TestMethod]
        public void TestFloatLiteral1()
        {
            var lexer = new Lexer("42.3");
            var t = lexer.Read();
            Assert.AreEqual(Sym.RealLiteral, t.Sym);
            Assert.AreEqual("42.3", t.Value);
        }

        [TestMethod]
        public void TestFloatLiteral2()
        {
            var lexer = new Lexer("43f 465e-1 78.01e-22M");
            var t = lexer.Read();
            Assert.AreEqual(Sym.RealLiteral, t.Sym);
            t = lexer.Read();
            Assert.AreEqual(Sym.RealLiteral, t.Sym);
            t = lexer.Read();
            Assert.AreEqual(Sym.RealLiteral, t.Sym);
            Assert.AreEqual("78.01e-22M", t.Value);

            t = lexer.Read();
            Assert.AreEqual(Sym.Eof, t.Sym);
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
            Assert.AreEqual(Sym.RealLiteral, t.Sym);
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

        [TestMethod]
        public void TestLexing1()
        {
            string input = "public class Foo { private int bar; }";
            AssertSymsMatch(input,
                Sym.KwPublic,
                Sym.KwClass,
                Sym.Id,
                Sym.OpenBrace,
                Sym.KwPrivate,
                Sym.KwInt,
                Sym.Id,
                Sym.Semicolon,
                Sym.CloseBrace);
        }

        [TestMethod]
        public void TestLexing2()
        {
            string input = "protected class Bar { private string name = \"james \"; }";
            AssertSymsMatch(input,
                Sym.KwProtected,
                Sym.KwClass,
                Sym.Id,
                Sym.OpenBrace,
                Sym.KwPrivate,
                Sym.KwString,
                Sym.Id,
                Sym.AssignEqual,
                Sym.StringLiteral,
                Sym.Semicolon,
                Sym.CloseBrace);
        }

        [TestMethod]
        public void TestEscapedIds()
        {
            string input = "var @int = 3;";
            AssertSymsMatch(input,
                Sym.KwVar,
                Sym.Id,
                Sym.AssignEqual,
                Sym.IntLiteral,
                Sym.Semicolon);
        }

        [TestMethod]
        public void TestStringLiteral1()
        {
            string input = @" ""hello world"" ";
            var tok = new Lexer(input).Read();

            Assert.AreEqual(Sym.StringLiteral, tok.Sym);
            Assert.AreEqual(@"""hello world""", tok.Value);
        }

        [TestMethod]
        public void TestStringLiteral2()
        {
            string input = @" ""hello \""james\"" swaine"" ";
            var tok = new Lexer(input).Read();
            Assert.AreEqual(Sym.StringLiteral, tok.Sym);
            Assert.AreEqual(@"""hello \""james\"" swaine""", tok.Value);
        }

        [TestMethod]
        public void TestLexing3()
        {
            string input =
                @"namespace Foo {
                    public class Bar {
                        public Bar() { 
                            Console.WriteLine(""bar ctor""); 
                        }
                    }
                  }";

            AssertSymsMatch(input,
                Sym.KwNamespace,
                Sym.Id,
                Sym.OpenBrace,
                Sym.KwPublic,
                Sym.KwClass,
                Sym.Id,
                Sym.OpenBrace,
                Sym.KwPublic,
                Sym.Id,
                Sym.OpenParen,
                Sym.CloseParen,
                Sym.OpenBrace,
                Sym.Id,
                Sym.Dot,
                Sym.Id,
                Sym.OpenParen,
                Sym.StringLiteral,
                Sym.CloseParen,
                Sym.Semicolon,
                Sym.CloseBrace,
                Sym.CloseBrace,
                Sym.CloseBrace);
        }
    }
}
