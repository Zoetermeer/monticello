using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monticello.Parsing
{
    public class Lexer
    {
        private string input;
        private int pos;

        /// <summary>
        /// Can be overridden in derived classes (mocks)?
        /// </summary>
        protected Lexer()
        {

        }

        public Lexer(string input)
        {
            this.input = input;
        }

        public bool CanRead
        {
            get
            {
                return pos < input.Length;
            }
        }

        private void SkipWs()
        {
            if (!CanRead)
                return;

            while (CanRead && char.IsWhiteSpace(input[pos]))
            {
                ++pos;
            }
        }

        private static bool IsIdentifierChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        private static bool IsIdentifierStartChar(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private static bool IsNumericLiteralStartChar(char c)
        {
            return char.IsDigit(c) || c == '.';
        }

        private bool NextAtom(ref string val)
        {
            SkipWs();
            if (!CanRead)
                return false;

            var sb = new StringBuilder();
            var c = input[pos++];

            if (IsIdentifierStartChar(c))
            {
                sb.Append(c);
                while (CanRead)
                {
                    c = input[pos++];
                    if (!IsIdentifierChar(c))
                    {
                        pos--;
                        break;
                    }

                    sb.Append(c);
                }
            }
            else if (c == '.')
            {
                var c1 = input[pos++];
                if (char.IsWhiteSpace(c1) || !char.IsDigit(c1))
                {
                    //Then this is a reference to a method, property, field, constant, etc.
                    //Just return the '.' as a token
                    pos--;
                    sb.Append(c);
                }
                else
                {
                    //Numeric literal
                    sb.Append(c);

                }
            }
            else
            {
                switch (c)
                {
                    case '{':
                    case '}':
                    case '[':
                    case ']':
                    case '(':
                    case ')':
                    case ',':
                    case ':':
                    case ';':
                    case '?':
                        sb.Append(c);
                        break;
                    case '=':
                        {
                            sb.Append(c);
                            var c1 = input[pos++];
                            if (c1 == '=')
                                sb.Append(c1);
                            else
                                pos--;
                        }
                        break;
                    

                }
            }

            val = sb.ToString();
            return true;
        }

        public virtual Token Read()
        {
            SkipWs();
            Token tok = new Token() { Col = pos };
            string v = null;
            if (!NextAtom(ref v))
            {
                tok.Sym = Sym.Eof;
                return tok;
            }

            tok.Value = v;
            switch (v)
            {
                case "void":
                    tok.Sym = Sym.KwVoid;
                    break;
                case "return":
                    tok.Sym = Sym.KwReturn;
                    break;
                case "class":
                    tok.Sym = Sym.KwClass;
                    break;
                case "namespace":
                    tok.Sym = Sym.KwNamespace;
                    break;
                case "int":
                    tok.Sym = Sym.KwInt;
                    break;
                case "float":
                    tok.Sym = Sym.KwFloat;
                    break;
                case ";":
                    tok.Sym = Sym.Semicolon;
                    break;
                case "=":
                    tok.Sym = Sym.AssignEqual;
                    break;
                case "==":
                    tok.Sym = Sym.EqualEqual;
                    break;
                default:
                    {
                        int x;
                        float y;
                        if (int.TryParse(v, out x))
                        {
                            tok.Sym = Sym.IntLiteral;
                            break;
                        }
                        else if (float.TryParse(v, out y))
                        {
                            tok.Sym = Sym.FloatLiteral;
                            break;
                        }
                    }
                    break;
            }

            return tok;
        }
    }
}
