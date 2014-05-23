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
        private Dictionary<string, LexicalRule> keywordTable = new Dictionary<string, LexicalRule>();

        /// <summary>
        /// Can be overridden in derived classes (mocks)?
        /// </summary>
        protected Lexer()
        {
            AddRule(".", Sym.Dot);
            AddRule(",", Sym.Comma);
            AddRule("+", Sym.Plus);
            AddRule("-", Sym.Minus);
            AddRule("*", Sym.Mult);
            AddRule("/", Sym.Div);
            AddRule("%", Sym.Mod);
            AddRule("+=", Sym.PlusEqual);
            AddRule("-=", Sym.MinusEqual);
            AddRule("*=", Sym.MultEqual);
            AddRule("/=", Sym.DivEqual);
            AddRule("%=", Sym.ModEqual);
            AddRule("<", Sym.LessThan);
            AddRule(">", Sym.GreaterThan);
            AddRule("<=", Sym.LtEqual);
            AddRule(">=", Sym.GtEqual);
            AddRule("<<", Sym.LeftShift);
            AddRule(">>", Sym.RightShift);
            AddRule("<<=", Sym.LeftShiftEqual);
            AddRule(">>=", Sym.RightShiftEqual);
            AddRule("&", Sym.BitwiseAnd);
            AddRule("|", Sym.BitwiseOr);
            AddRule("^", Sym.BitwiseXor);
            AddRule("~", Sym.BitwiseNot);
            AddRule("&=", Sym.BitwiseAndEqual);
            AddRule("|=", Sym.BitwiseOrEqual);
            AddRule("^=", Sym.BitwiseXorEqual);
            AddRule("&&", Sym.BooleanAnd);
            AddRule("||", Sym.BooleanOr);
            AddRule("++", Sym.PlusPlus);
            AddRule("--", Sym.MinusMinus);
            AddRule(";", Sym.Semicolon);
            AddRule(":", Sym.Colon);
            AddRule("=", Sym.AssignEqual);
            AddRule("==", Sym.EqualEqual);
            AddRule("!=", Sym.NotEqual);
            AddRule("!", Sym.Not);
            AddRule("{", Sym.OpenBrace);
            AddRule("}", Sym.CloseBrace);
            AddRule("(", Sym.OpenParen);
            AddRule(")", Sym.CloseParen);
            AddRule("[", Sym.OpenIndexer);
            AddRule("]", Sym.CloseIndexer);
            AddRule("abstract", Sym.KwAbstract, isKeyword: true);
            AddRule("as", Sym.KwAs, isKeyword: true);
            AddRule("base", Sym.KwBase, isKeyword: true);
            AddRule("bool", Sym.KwBool, isKeyword: true);
            AddRule("break", Sym.KwBreak, isKeyword: true);
            AddRule("byte", Sym.KwByte, isKeyword: true);
            AddRule("case", Sym.KwCase, isKeyword: true);
            AddRule("catch", Sym.KwCatch, isKeyword: true);
            AddRule("char", Sym.KwChar, isKeyword: true);
            AddRule("checked", Sym.KwChecked, isKeyword: true);
            AddRule("class", Sym.KwClass, isKeyword: true);
            AddRule("const", Sym.KwConst, isKeyword: true);
            AddRule("continue", Sym.KwContinue, isKeyword: true);
            AddRule("decimal", Sym.KwDecimal, isKeyword: true);
            AddRule("default", Sym.KwDefault, isKeyword: true);
            AddRule("delegate", Sym.KwDelegate, isKeyword: true);
            AddRule("do", Sym.KwDo, isKeyword: true);
            AddRule("double", Sym.KwDouble, isKeyword: true);
            AddRule("else", Sym.KwElse, isKeyword: true);
            AddRule("enum", Sym.KwEnum, isKeyword: true);
            AddRule("event", Sym.KwEvent, isKeyword: true);
            AddRule("explicit", Sym.KwExplicit, isKeyword: true);
            AddRule("extern", Sym.KwExtern, isKeyword: true);
            AddRule("false", Sym.KwFalse, isKeyword: true);
            AddRule("finally", Sym.KwFinally, isKeyword: true);
            AddRule("fixed", Sym.KwFixed, isKeyword: true);
            AddRule("float", Sym.KwFloat, isKeyword: true);
            AddRule("for", Sym.KwFor, isKeyword: true);
            AddRule("foreach", Sym.KwForeach, isKeyword: true);
            AddRule("goto", Sym.KwGoto, isKeyword: true);
            AddRule("if", Sym.KwIf, isKeyword: true);
            AddRule("implicit", Sym.KwImplicit, isKeyword: true);
            AddRule("in", Sym.KwIn, isKeyword: true);
            AddRule("int", Sym.KwInt, isKeyword: true);
            AddRule("interface", Sym.KwInterface, isKeyword: true);
            AddRule("internal", Sym.KwInternal, isKeyword: true);
            AddRule("is", Sym.KwIs, isKeyword: true);
            AddRule("lock", Sym.KwLock, isKeyword: true);
            AddRule("long", Sym.KwLong, isKeyword: true);
            AddRule("namespace", Sym.KwNamespace, isKeyword: true);
            AddRule("new", Sym.KwNew, isKeyword: true);
            AddRule("null", Sym.KwNull, isKeyword: true);
            AddRule("object", Sym.KwObject, isKeyword: true);
            AddRule("operator", Sym.KwOperator, isKeyword: true);
            AddRule("out", Sym.KwOut, isKeyword: true);
            AddRule("override", Sym.KwOverride, isKeyword: true);
            AddRule("params", Sym.KwParams, isKeyword: true);
            AddRule("private", Sym.KwPrivate, isKeyword: true);
            AddRule("protected", Sym.KwProtected, isKeyword: true);
            AddRule("public", Sym.KwPublic, isKeyword: true);
            AddRule("readonly", Sym.KwReadonly, isKeyword: true);
            AddRule("ref", Sym.KwRef, isKeyword: true);
            AddRule("return", Sym.KwReturn, isKeyword: true);
            AddRule("sbyte", Sym.KwSbyte, isKeyword: true);
            AddRule("sealed", Sym.KwSealed, isKeyword: true);
            AddRule("short", Sym.KwShort, isKeyword: true);
            AddRule("sizeof", Sym.KwSizeof, isKeyword: true);
            AddRule("stackalloc", Sym.KwStackalloc, isKeyword: true);
            AddRule("static", Sym.KwStatic, isKeyword: true);
            AddRule("string", Sym.KwString, isKeyword: true);
            AddRule("struct", Sym.KwStruct, isKeyword: true);
            AddRule("switch", Sym.KwSwitch, isKeyword: true);
            AddRule("this", Sym.KwThis, isKeyword: true);
            AddRule("throw", Sym.KwThrow, isKeyword: true);
            AddRule("true", Sym.KwTrue, isKeyword: true);
            AddRule("try", Sym.KwTry, isKeyword: true);
            AddRule("typeof", Sym.KwTypeof, isKeyword: true);
            AddRule("uint", Sym.KwUint, isKeyword: true);
            AddRule("ulong", Sym.KwUlong, isKeyword: true);
            AddRule("unchecked", Sym.KwUnchecked, isKeyword: true);
            AddRule("unsafe", Sym.KwUnsafe, isKeyword: true);
            AddRule("ushort", Sym.KwUshort, isKeyword: true);
            AddRule("using", Sym.KwUsing, isKeyword: true);
            AddRule("var", Sym.KwVar, isKeyword: true);
            AddRule("virtual", Sym.KwVirtual, isKeyword: true);
            AddRule("void", Sym.KwVoid, isKeyword: true);
            AddRule("volatile", Sym.KwVolatile, isKeyword: true);
            AddRule("while", Sym.KwWhile, isKeyword: true);
            AddRule(new StringLiteralLexicalRule());
            AddRule(new CharLiteralLexicalRule());
            AddRule(new NumericLiteralLexicalRule());
            AddRule(new IdLexicalRule());
        }

        public Lexer(string input)
            :this()
        {
            this.input = input;
        }

        public int Line { get; private set; }
        //TODO: Make line and col work (by counting newlines)
        public int Col { get { return pos; } }
        /// <summary>
        /// Gets the index of the current character in the input stream.
        /// </summary>
        public int Pos { get { return pos; } set { pos = value; } }
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

        public Dictionary<string, LexicalRule> KeywordTable
        {
            get { return keywordTable; }
        }

        private void AddRule(LexicalRule rule)
        {
            table.Add(rule);
        }

        private void AddRule(string pat, Sym sym, bool isKeyword = false)
        {
            var rule = table.Add(pat, sym);
            if (isKeyword)
                keywordTable.Add(pat, rule);
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

        public void Advance(int chars)
        {
            pos += chars;
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
                return new Token() { Line = this.Line, Col = this.Col, Sym = Sym.Eof };
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
            throw new RecognitionException(this.Line, this.Col, p.GetValueOrDefault());
        }
    }
}
