using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

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

    /// <summary>
    /// Recognizer for any C# identifier.
    /// </summary>
    public class IdLexicalRule : LexicalRule {
        public IdLexicalRule()
            : base("", Sym.Id)
        {

        }

        public override char? StartChar { get { return null; } }

        private static bool IsIdentifierStartChar(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private static bool IsIdentifierPartChar(char? c)
        {
            if (!c.HasValue)
                return false;

            char cc = c.GetValueOrDefault();
            return char.IsLetterOrDigit(cc) || cc == '_';
        }

        public override bool IsPossibleStartChar(char c)
        {
            return IsIdentifierStartChar(c);
        }

        public override Token Match(Lexer lexer)
        {
            var c = lexer.NextChar();
            if (IsIdentifierStartChar(c)) {
                var tok = new Token() { Line = lexer.Line, Col = lexer.Col, Sym = Sym.Id };
                var sb = new StringBuilder();
                sb.Append(c);
                while (IsIdentifierPartChar(lexer.Peek())) {
                    sb.Append(lexer.NextChar());
                }

                tok.Value = sb.ToString();
                return tok;
            }

            return null;
        }
    }

    /// <summary>
    /// Recognizer for all numeric literal types.
    /// </summary>
    public class NumericLiteralLexicalRule : LexicalRule {
        public NumericLiteralLexicalRule()
            : base("", Sym.Unknown)
        {

        }

        public override char? StartChar { get { return null; } }

        public override bool IsPossibleStartChar(char c)
        {
            return c == StartChar || char.IsDigit(c);
        }

        //TODO: Real-literal reading isn't feature-complete
        //TODO: Hexadecimal integer literals
        public override Token Match(Lexer lexer)
        {
            var tok = new Token() { Line = lexer.Line, Col = lexer.Col };
            string rem = lexer.RemainingInput;
            var decIntLit = new Regex(@"\G[0-9]+(U|u|L|l|UL|Ul|uL|ul|LU|Lu|lU|lu)?");
            var match = decIntLit.Match(rem);
            if (match.Success) {
                tok.Value = match.Value;
                tok.Sym = Sym.IntLiteral;
                return tok;
            }

            return null;
        }
    }
}
