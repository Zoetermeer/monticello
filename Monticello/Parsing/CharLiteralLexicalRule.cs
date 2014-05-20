using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Monticello.Parsing {
    public class CharLiteralLexicalRule : LexicalRule {
        public CharLiteralLexicalRule()
            : base("", Sym.Unknown)
        {

        }

        public override char? StartChar { get { return null; } }
        private Regex singleCharRe = new Regex(@"\G'[^\u0027\u005c]'", RegexOptions.Compiled);
        private Regex escapeCharRe = new Regex(@"\G'\\(""|'|\\|0|a|b|f|n|r|t|v)'", RegexOptions.Compiled);
        private Regex hexEscapeRe = new Regex(@"\G'\\x[0-9a-fA-F]{1, 4}'", RegexOptions.Compiled);
        private Regex unicEscapeRe = new Regex(@"\G'(\\u[0-9a-fA-F]{4}|\\U[0-9a-fA-F]{8})'", RegexOptions.Compiled);

        public override bool IsPossibleStartChar(char c)
        {
            return c == '\'';
        }

        public override Token Match(Lexer lexer)
        {
            var rem = lexer.RemainingInput;
            if (lexer.CanRead) {
                string value = null;
                var match = singleCharRe.Match(rem);
                if (match.Success) {
                    value = match.Value;
                } else if ((match = escapeCharRe.Match(rem)).Success) {
                    value = match.Value;
                } else if ((match = hexEscapeRe.Match(rem)).Success) {
                    value = match.Value;
                } else if ((match = unicEscapeRe.Match(rem)).Success) {
                    value = match.Value;
                }

                if (null != value) {
                    var tok = new Token() { Line = lexer.Line, Col = lexer.Col };
                    tok.Sym = Sym.CharLiteral;
                    tok.Value = value.Substring(1, value.Length - 2); ;
                    lexer.Advance(value.Length);

                    return tok;
                }
            }

            return null;
        }
    }
}
