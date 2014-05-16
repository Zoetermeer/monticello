using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Monticello.Parsing {
    class StringLiteralLexicalRule : LexicalRule {
        public StringLiteralLexicalRule()
            : base("", Sym.Unknown)
        {

        }

        public override char? StartChar { get { return null; } }
        private Regex simpleLit = new Regex(@"\G(?(\\)\\(""|''|\\|0|a|b|f|n|r|t|v)|[^\\]*)*""", RegexOptions.Compiled);

        public override bool IsPossibleStartChar(char c)
        {
            return c == '"' || c == '@';
        }

        public override Token Match(Lexer lexer)
        {
            if (lexer.CanRead) {
                var match = simpleLit.Match(lexer.RemainingInput);
                if (match.Success) {
                    var tok = new Token() { Line = lexer.Line, Col = lexer.Col };
                    tok.Sym = Sym.StringLiteral;
                    tok.Value = match.Value;
                    lexer.Advance(tok.Value.Length);
                    return tok;
                }
            }

            return null;
        }
    }
}
