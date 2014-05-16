using System;
using System.Collections.Generic;
using System.Text;

namespace Monticello.Parsing {
    public class LexicalRule : IComparable<LexicalRule> {
        public LexicalRule(string pat, Sym symbol)
        {
            Pattern = pat;
            Symbol = symbol;
        }

        public virtual char? StartChar { get { return Pattern[0]; } }
        public string Pattern { get; private set; }
        public Sym Symbol { get; private set; }

        public virtual bool IsPossibleStartChar(char c)
        {
            return c == StartChar.GetValueOrDefault();
        }

        public virtual Token Match(Lexer lexer)
        {
            bool match = true;
            var tok = new Token() { Line = lexer.Line, Col = lexer.Col, Sym = this.Symbol };
            var sb = new StringBuilder();
            lexer.PushMark();
            foreach (char c in Pattern) {
                if (!lexer.CanRead) {
                    match = false;
                    break;
                }

                char i = lexer.NextChar();
                if (c != i) {
                    match = false;
                    break;
                }

                sb.Append(i);
            }

            if (!match) {
                lexer.PopMark();
                return null;
            }

            tok.Value = sb.ToString();
            return tok;
        }

        public int CompareTo(LexicalRule other)
        {
            return other.Pattern.Length.CompareTo(this.Pattern.Length);
        }
    }
}
