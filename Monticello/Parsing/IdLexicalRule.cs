using System;
using System.Text;

namespace Monticello.Parsing {
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
}
