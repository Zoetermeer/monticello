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
            return char.IsLetter(c) || c == '_' || c == '@';
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
            bool isKeyword = false;
            string word = string.Empty;
            using (var la = new LookaheadFrame(lexer)) {
                var c = lexer.NextChar();
                if (IsIdentifierStartChar(c)) {
                    var tok = new Token() { Line = lexer.Line, Col = lexer.Col, Sym = Sym.Id };
                    var sb = new StringBuilder();
                    sb.Append(c);
                    while (IsIdentifierPartChar(lexer.Peek())) {
                        sb.Append(lexer.NextChar());
                    }

                    //Since the id rule tries to match before any 
                    //reserved-word ones, we need to make sure this id isn't 
                    //actually a keyword
                    word = sb.ToString();
                    isKeyword = lexer.KeywordTable.ContainsKey(word);
                    if (!isKeyword) {
                        la.Commit();
                        tok.Value = word;
                        return tok;
                    }
                }
            }

            //If it is a kw, invoke that rule instead
            if (isKeyword) {
                return lexer.KeywordTable[word].Match(lexer);
            }
            
            return null;
        }
    }
}
