using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Covenant.Parsing;

namespace CovenantTests
{
    [TestClass]
    public class TokenBufferTests
    {
        public class MockLexer : Lexer
        {
            private Queue<Token> toks = new Queue<Token>();

            public MockLexer(params Token[] toks)
                :base()
            {
                foreach (var t in toks)
                    this.toks.Enqueue(t);
            }

            public MockLexer(params Sym[] syms)
                : base()
            {
                foreach (var s in syms)
                    this.toks.Enqueue(new Token() { Sym = s });
            }

            public override Token Read()
            {
                return toks.Dequeue();
            }
        }

        [TestMethod]
        public void TokBufferTest1()
        {
            var ml = new MockLexer(Sym.IntLiteral, Sym.KwInt, Sym.FloatLiteral);
            var b = new TokenBuffer(ml);

            var t = b.Next();
            Assert.AreEqual(Sym.IntLiteral, t.Sym);
            t = b.Next();
            Assert.AreEqual(Sym.KwInt, t.Sym);
            t = b.Next();
            Assert.AreEqual(Sym.FloatLiteral, t.Sym);
        }

        [TestMethod]
        public void TokBufferTest2()
        {
            var ml = new MockLexer(Sym.IntLiteral, Sym.KwInt, Sym.FloatLiteral);
            var b = new TokenBuffer(ml);

            var t = b.Next();
            Assert.AreEqual(Sym.IntLiteral, t.Sym);
            using (new LookaheadFrame(b))
            {
                t = b.Next();
                Assert.AreEqual(Sym.KwInt, t.Sym);
                t = b.Next();
                Assert.AreEqual(Sym.FloatLiteral, t.Sym);
            }

            t = b.Next();
            Assert.AreEqual(Sym.KwInt, t.Sym);
            t = b.Next();
            Assert.AreEqual(Sym.FloatLiteral, t.Sym);
        }

        [TestMethod]
        public void TokBufferTest3()
        {
            var ml = new MockLexer(Sym.IntLiteral, Sym.KwInt, Sym.KwClass, Sym.Semicolon, Sym.KwVoid);
            var b = new TokenBuffer(ml);

            var t = b.Next();
            Assert.AreEqual(Sym.IntLiteral, t.Sym);
            using (new LookaheadFrame(b))
            {
                t = b.Next();
                Assert.AreEqual(Sym.KwInt, t.Sym);
                using (new LookaheadFrame(b))
                {
                    t = b.Next();
                    Assert.AreEqual(Sym.KwClass, t.Sym);
                }

                t = b.Next();
                Assert.AreEqual(Sym.KwClass, t.Sym);
            }

            t = b.Next();
            Assert.AreEqual(Sym.KwInt, t.Sym);
        }

        [TestMethod]
        public void OnlyBufferOneEof()
        {
            var l = new Lexer("");
            var b = new TokenBuffer(l);
            var t = b.Next();
            Assert.AreEqual(Sym.Eof, t.Sym);

            t = b.Next();
            Assert.AreEqual(Sym.Eof, t.Sym);
            Assert.AreEqual(1, b.Size);
        }

        [TestMethod]
        public void TestMarkAtZero()
        {
            var ml = new MockLexer(Sym.KwInt, Sym.KwClass, Sym.KwVoid);
            var b = new TokenBuffer(ml);

            using (new LookaheadFrame(b))
            {
                var t = b.Next();
                Assert.AreEqual(Sym.KwInt, t.Sym);
            }

            var t1 = b.Next();
            Assert.AreEqual(Sym.KwInt, t1.Sym);

            t1 = b.Next();
            Assert.AreEqual(Sym.KwClass, t1.Sym);
        }
    }
}
