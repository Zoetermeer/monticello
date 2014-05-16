using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Monticello.Parsing {
    /// <summary>
    /// Recognizer for all numeric literal types.
    /// </summary>
    public class NumericLiteralLexicalRule : LexicalRule {
        Regex decIntLit = new Regex(@"\G[0-9]+(U|u|L|l|UL|Ul|uL|ul|LU|Lu|lU|lu)?", RegexOptions.Compiled);
        Regex hexIntLit = new Regex(@"\G0(x|X)[0-9A-Fa-f]+", RegexOptions.Compiled);
        Regex realWithDecPtLit = new Regex(@"\G[0-9]*\.[0-9]+((e|E)(-|\+)?[0-9]+)?(F|f|D|d|M|m)?", RegexOptions.Compiled);
        Regex realWithExptLit = new Regex(@"\G[0-9]+(e|E)(-|\+)?[0-9]+(F|f|D|d|M|m)?", RegexOptions.Compiled);
        Regex realWithSuffLit = new Regex(@"\G[0-9]+(F|f|D|d|M|m)", RegexOptions.Compiled);
        List<Regex> realRes;

        public NumericLiteralLexicalRule()
            : base("", Sym.Unknown)
        {
            realRes = new List<Regex>() { realWithDecPtLit, realWithExptLit, realWithSuffLit };
        }

        public override char? StartChar { get { return null; } }

        public override bool IsPossibleStartChar(char c)
        {
            return c == StartChar || char.IsDigit(c);
        }

        public override Token Match(Lexer lexer)
        {
            var tok = new Token() { Line = lexer.Line, Col = lexer.Col };
            string rem = lexer.RemainingInput;
            bool isMatch = false;

            Match match = hexIntLit.Match(rem);
            if (match.Success) {
                tok.Value = match.Value;
                tok.Sym = Sym.HexIntLiteral;
                isMatch = true;
            }

            foreach (var re in realRes) {
                match = re.Match(rem);
                if (match.Success) {
                    tok.Value = match.Value;
                    tok.Sym = Sym.RealLiteral;
                    isMatch = true;
                    break;
                }
            }

            if (!isMatch) {
                match = decIntLit.Match(rem);
                if (match.Success) {
                    tok.Value = match.Value;
                    tok.Sym = Sym.IntLiteral;
                    isMatch = true;
                }
            }

            if (isMatch) {
                lexer.Advance(tok.Value.Length);
                return tok;
            }

            return null;
        }
    }
}
