using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monticello.Parsing
{
    public class Lexer : ICanLookahead
    {
        private string input;
        private int pos;
        private int? markPos = null;
        private LexicalRuleTable table = new LexicalRuleTable();

        /// <summary>
        /// Can be overridden in derived classes (mocks)?
        /// </summary>
        protected Lexer()
        {
            table.Add(new LexicalRule(".", Sym.Dot));
            table.Add(new LexicalRule("+", Sym.Plus));
            table.Add(new LexicalRule(";", Sym.Semicolon));
            table.Add(new LexicalRule("=", Sym.AssignEqual));
            table.Add(new LexicalRule("==", Sym.EqualEqual));
            table.Add(new LexicalRule("class", Sym.KwClass));
            table.Add(new LexicalRule("namespace", Sym.KwNamespace));
            table.Add(new NumericLiteralLexicalRule());
            table.Add(new IdLexicalRule());
        }

        public Lexer(string input)
            :this()
        {
            this.input = input;
        }

        public int Line { get; private set; }
        public int Col { get { return pos; } }
        public string Input { get { return input; } }
        public string RemainingInput
        {
            get
            {
                if (!CanRead)
                    return string.Empty;

                return input.Substring(pos);
            }
        }

        public bool CanRead
        {
            get
            {
                return pos < input.Length;
            }
        }

        public void SkipWs()
        {
            if (!CanRead)
                return;

            while (CanRead && char.IsWhiteSpace(input[pos]))
            {
                ++pos;
            }
        }

        public char NextChar()
        {
            if (pos >= input.Length)
                throw new InvalidOperationException("The end of the input string has already been reached.");

            return input[pos++];
        }

        public void PushMark()
        {
            markPos = pos;
        }

        public void PopMark()
        {
            if (!markPos.HasValue)
                throw new InvalidOperationException("Cannot backtrack without a marked position.");

            pos = markPos.GetValueOrDefault();
            markPos = null;
        }

        public char? Peek()
        {
            if (pos > input.Length - 1)
                return null;

            return input[pos];
        }

        public virtual Token Read()
        {
            SkipWs();
            char? p = Peek();
            if (!p.HasValue)
            {
                return new Token() { Line = this.Line, Col = this.Col, Sym = global::Sym.Eof };
            }
            else
            {
                char c = p.GetValueOrDefault();
                Token tok = null;
                foreach (var rule in table.RulesForStartChar(c))
                {
                    tok = rule.Match(this);
                    if (null != tok)
                        return tok;
                }
            }

            //No match, error 
            throw new RecognitionException(this.Line, this.Col);
        }
    }
}
