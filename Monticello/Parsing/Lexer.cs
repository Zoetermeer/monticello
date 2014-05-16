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
            table.Add(".", Sym.Dot);
            table.Add("+", Sym.Plus);
            table.Add("-", Sym.Minus);
            table.Add("*", Sym.Mult);
            table.Add("/", Sym.Div);
            table.Add("%", Sym.Mod);
            table.Add("+=", Sym.PlusEqual);
            table.Add("-=", Sym.MinusEqual);
            table.Add("*=", Sym.MultEqual);
            table.Add("/=", Sym.DivEqual);
            table.Add("%=", Sym.ModEqual);
            table.Add("<", Sym.LessThan);
            table.Add(">", Sym.GreaterThan);
            table.Add("<=", Sym.LtEqual);
            table.Add(">=", Sym.GtEqual);
            table.Add("<<", Sym.LeftShift);
            table.Add(">>", Sym.RightShift);
            table.Add("<<=", Sym.LeftShiftEqual);
            table.Add(">>=", Sym.RightShiftEqual);
            table.Add("&", Sym.BitwiseAnd);
            table.Add("|", Sym.BitwiseOr);
            table.Add("^", Sym.BitwiseXor);
            table.Add("~", Sym.BitwiseNot);
            table.Add("&=", Sym.BitwiseAndEqual);
            table.Add("|=", Sym.BitwiseOrEqual);
            table.Add("^=", Sym.BitwiseXorEqual);
            table.Add("&&", Sym.BooleanAnd);
            table.Add("||", Sym.BooleanOr);
            table.Add("++", Sym.PlusPlus);
            table.Add("--", Sym.MinusMinus);
            table.Add(";", Sym.Semicolon);
            table.Add("=", Sym.AssignEqual);
            table.Add("==", Sym.EqualEqual);
            table.Add("!=", Sym.NotEqual);
            table.Add("!", Sym.Not);
            table.Add("abstract", Sym.KwAbstract);
            table.Add("as", Sym.KwAs);
            table.Add("base", Sym.KwBase);
            table.Add("bool", Sym.KwBool);
            table.Add("break", Sym.KwBreak);
            table.Add("byte", Sym.KwByte);
            table.Add("case", Sym.KwCase);
            table.Add("catch", Sym.KwCatch);
            table.Add("char", Sym.KwChar);
            table.Add("checked", Sym.KwChecked);
            table.Add("class", Sym.KwClass);
            table.Add("const", Sym.KwConst);
            table.Add("continue", Sym.KwContinue);
            table.Add("decimal", Sym.KwDecimal);
            table.Add("default", Sym.KwDefault);
            table.Add("delegate", Sym.KwDelegate);
            table.Add("do", Sym.KwDo);
            table.Add("double", Sym.KwDouble);
            table.Add("else", Sym.KwElse);
            table.Add("enum", Sym.KwEnum);
            table.Add("event", Sym.KwEvent);
            table.Add("explicit", Sym.KwExplicit);
            table.Add("extern", Sym.KwExtern);
            table.Add("false", Sym.KwFalse);
            table.Add("finally", Sym.KwFinally);
            table.Add("fixed", Sym.KwFixed);
            table.Add("float", Sym.KwFloat);
            table.Add("for", Sym.KwFor);
            table.Add("foreach", Sym.KwForeach);
            table.Add("goto", Sym.KwGoto);
            table.Add("if", Sym.KwIf);
            table.Add("implicit", Sym.KwImplicit);
            table.Add("in", Sym.KwIn);
            table.Add("int", Sym.KwInt);
            table.Add("interface", Sym.KwInterface);
            table.Add("internal", Sym.KwInternal);
            table.Add("is", Sym.KwIs);
            table.Add("lock", Sym.KwLock);
            table.Add("long", Sym.KwLong);
            table.Add("namespace", Sym.KwNamespace);
            table.Add("new", Sym.KwNew);
            table.Add("null", Sym.KwNull);
            table.Add("object", Sym.KwObject);
            table.Add("operator", Sym.KwOperator);
            table.Add("out", Sym.KwOut);
            table.Add("override", Sym.KwOverride);
            table.Add("params", Sym.KwParams);
            table.Add("private", Sym.KwPrivate);
            table.Add("protected", Sym.KwProtected);
            table.Add("public", Sym.KwPublic);
            table.Add("readonly", Sym.KwReadonly);
            table.Add("ref", Sym.KwRef);
            table.Add("return", Sym.KwReturn);
            table.Add("sbyte", Sym.KwSbyte);
            table.Add("sealed", Sym.KwSealed);
            table.Add("short", Sym.KwShort);
            table.Add("sizeof", Sym.KwSizeof);
            table.Add("stackalloc", Sym.KwStackalloc);
            table.Add("static", Sym.KwStatic);
            table.Add("string", Sym.KwString);
            table.Add("struct", Sym.KwStruct);
            table.Add("switch", Sym.KwSwitch);
            table.Add("this", Sym.KwThis);
            table.Add("throw", Sym.KwThrow);
            table.Add("true", Sym.KwTrue);
            table.Add("try", Sym.KwTry);
            table.Add("typeof", Sym.KwTypeof);
            table.Add("uint", Sym.KwUint);
            table.Add("ulong", Sym.KwUlong);
            table.Add("unchecked", Sym.KwUnchecked);
            table.Add("unsafe", Sym.KwUnsafe);
            table.Add("ushort", Sym.KwUshort);
            table.Add("using", Sym.KwUsing);
            table.Add("virtual", Sym.KwVirtual);
            table.Add("void", Sym.KwVoid);
            table.Add("volatile", Sym.KwVolatile);
            table.Add("while", Sym.KwWhile);
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
