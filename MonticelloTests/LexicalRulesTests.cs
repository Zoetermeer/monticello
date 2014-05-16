using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Monticello.Parsing;

namespace MonticelloTests
{
    [TestClass]
    public class LexicalRulesTests
    {
        [TestMethod]
        public void TestSortOrder()
        {
            var ra = new LexicalRule("x", Sym.Id);
            var rb = new LexicalRule("xx", Sym.Id);

            var la = new List<LexicalRule>() { ra, rb };
            var lb = new List<LexicalRule>() { rb, ra };

            la.Sort();
            lb.Sort();

            Assert.AreEqual(rb, la[0]);
            Assert.AreEqual(rb, lb[0]);
        }

        [TestMethod]
        public void TestLexicalMatch1()
        {
            var lexer = new Lexer("foo bar");
            var rule = new LexicalRule("foo", Sym.KwClass);
            Assert.IsNotNull(rule.Match(lexer));
        }

        [TestMethod]
        public void RuleTableTest1()
        {
            var tbl = new LexicalRuleTable();
            tbl.Add(new LexicalRule(".", Sym.Dot));
            tbl.Add(new LexicalRule("..", Sym.Colon));

            Assert.AreEqual(2, new List<LexicalRule>(tbl.RulesForStartChar('.')).Count);
        }

        [TestMethod]
        public void RuleTableTest2()
        {
            var tbl = new LexicalRuleTable();
            tbl.Add(new NumericLiteralLexicalRule());

            Assert.AreEqual(1, new List<LexicalRule>(tbl.RulesForStartChar('3')).Count);
        }
    }
}
