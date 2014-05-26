using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monticello.Parsing
{
    [DebuggerDisplay("{Method}")]
    public delegate T Rule<out T>();

    public class Parser
    {
        public class LR : AstNode
        {
            public LR(bool wasDetected) 
                : base(null)
            {
                this.WasDetected = wasDetected;
            }

            public bool WasDetected { get; set; }

            public override string ToString()
            {
                return string.Format("(lr {0})", WasDetected);
            }
        }


        private string input;
        private Lexer lexer;
        private ParseResult result;
        private MemoTable memoTable = new MemoTable();

        public Parser(string input)
        {
            this.input = input;
            this.lexer = new Lexer(input);
            this.result = new ParseResult();
        }

        public int Pos { get { return lexer.Pos; } }
        public MemoTable MemoTable { get { return memoTable; } }

        private bool Expect<T>(
            Func<T> parser, 
            string errMsg, 
            out T ast)
            where T : AstNode
        {
            ast = parser();
            if (null == ast) {
                result.Error(errMsg, lexer);
                return false;
            }

            return true;
        }

        private bool Expect(Sym expected)
        {
            Token t;
            return Expect(expected, out t);
        }

        private bool Expect(Sym expected, out Token tok)
        {
            tok = lexer.Read();
            bool match = tok.Is(expected);
            if (!match) {
                result.Error(string.Format("Expected {0}", expected), tok);
            }

            return match;
        }

        private bool Accept<T>(Func<T> parser, out T ast) 
            where T : AstNode
        {
            ast = parser();
            if (null == ast)
                return false;

            return true;
        }

        private bool Accept(Sym expected)
        {
            if (lexer.PeekToken().Is(expected)) {
                lexer.Read();
                return true;
            }

            return false;
        }

        private bool Accept(Sym expected, out Token tok)
        {
            if (lexer.PeekToken().Is(expected)) {
                tok = lexer.Read();
                return true;
            }

            tok = null;
            return false;
        }

        private T GrowLR<T>(Rule<T> rule, int p, MemoEntry m) 
            where T : AstNode
        {
            //...
            while (true) {
                lexer.Pos = p;
                //...
                var ans = rule();
                if (null == ans || lexer.Pos <= m.Pos)
                    break;

                m.Ast = ans;
                m.Pos = lexer.Pos;
            }

            //...

            lexer.Pos = m.Pos;
            return m.Ast as T;
        }

        [DebuggerHidden]
        public T ApplyRule<T>(Rule<T> rule)
            where T : AstNode
        {
            return ApplyRule<T>(rule, lexer.Pos);
        }

        private T ApplyRule<T>(Rule<T> rule, int pos) 
            where T : AstNode
        {
            MemoEntry m = null;
            LR lr;
            string ruleName = rule.Method.Name;
            var key = Tuple.Create(ruleName, pos);
            if (!memoTable.TryGetValue(key, out m)) {
                lr = new LR(false);
                m = new MemoEntry(pos, lr); //Failure
                memoTable.Add(key, m);

                var ans = rule();
                m.Ast = ans;
                m.Pos = lexer.Pos;
                if (lr.WasDetected && null != ans) {
                    return GrowLR(rule, pos, m);
                }

                return ans;
            }

            lexer.Pos = m.Pos;
            lr = m.Ast as LR;
            if (null != lr) {
                lr.WasDetected = true;
                return null;
            }

            return m.Ast as T;
        }

        public static CompilationUnit Parse(string input)
        {
            return new Parser(input).ParseCompilationUnit();
        }

        public CompilationUnit ParseCompilationUnit()
        {
            //var decls = ParseNamespaceMemberDecls(buf, result);

            var cu = new CompilationUnit();
            cu.Usings.AddRange(ParseUsingDirectives());
            cu.GlobalAttributes.AddRange(ParseGlobalAttrs());
            //cu.Decls.AddRange(decls);

            return cu;
        }

        /// <summary>
        /// using-directives := (using-directive)*
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public List<UsingDirective> ParseUsingDirectives()
        {
            var usings = new List<UsingDirective>();
            UsingDirective ud;
            while (Accept(ParseUsingDirective, out ud)) {
                usings.Add(ud);
            }

            return usings;
        }

        /// <summary>
        /// using-directive :=
        ///     using-alias-directive
        ///     using-namespace-directive
        ///     
        /// using-alias-directive := 'using' id '=' qualified-id ';'
        /// using-namespace-directive := 'using' qualified-id ';'
        /// </summary>
        /// <returns></returns>
        public UsingDirective ParseUsingDirective()
        {
            Token start;
            if (Accept(Sym.KwUsing, out start)) {
                QualifiedIdExp nameOrAlias;
                if (Expect(ParseQualifiedId, "Expected identifier", out nameOrAlias)) {
                    if (nameOrAlias.Parts.Count == 1) {
                        if (Accept(Sym.AssignEqual)) {
                            //Using-alias-directive
                            QualifiedIdExp nsOrTypeName;
                            if (Expect(ParseQualifiedId, "Expected namespace or type name", out nsOrTypeName)
                                && Expect(Sym.Semicolon)) {
                                var uad = new UsingAliasDirective(start);
                                uad.Alias = nameOrAlias.Parts.First();
                                uad.NamespaceOrTypeName = nsOrTypeName;
                                return uad;
                            }
                        }
                    } 

                    //Otherwise, using-namespace-directive
                    if (Expect(Sym.Semicolon)) {
                        var und = new UsingNamespaceDirective(start);
                        und.NamespaceName = nameOrAlias;
                        return und;
                    }
                }
            }

            return null;
        }

        public List<AttrSection> ParseGlobalAttrs()
        {
            var sections = new List<AttrSection>();
            AttrSection section;
            while (Accept(ParseGlobalAttrSection, out section)) {
                sections.Add(section);
            }

            return sections;
        }

        public AttrSection ParseGlobalAttrSection()
        {
            Token t;
            if (Accept(Sym.OpenIndexer, out t)) {
                var section = new AttrSection(t);
                if (Expect(Sym.Id, out t)) {
                    switch (t.Value) {
                        case "assembly":
                            section.Target = AttrTarget.Assembly;
                            break;
                        case "module":
                            section.Target = AttrTarget.Module;
                            break;
                        default:
                            //Add an error, but try to continue parsing
                            result.Error("Expected 'module' or 'assembly' target", t);
                            break;
                    }
                }

                if (Expect(Sym.Colon)) {
                    Attr attr;
                    if (Expect(ParseAttribute, "Expected attribute", out attr)) {
                        section.Attrs.Add(attr);
                        while (Accept(Sym.Comma)) {
                            if (!Accept(ParseAttribute, out attr))
                                break;

                            section.Attrs.Add(attr);
                        }
                    }

                    Expect(Sym.CloseIndexer);                        
                }

                return section;
            }

            return null;
        }

        /// <summary>
        /// attribute := qualified-id { attribute-args }
        /// attribute-args := 
        ///     '(' { positional-arg-list }')'
        ///     '(' positional-arg-list ',' named-arg-list ')'
        ///     '(' named-arg-list ')'
        /// </summary>
        /// <returns></returns>
        public Attr ParseAttribute()
        {
            QualifiedIdExp qid;
            if (Expect(ParseQualifiedId, "Expected identifier", out qid)) {
                var attr = new Attr(qid.StartToken);
                attr.AttrTypeName = qid;
                if (Accept(Sym.OpenParen)) {
                    attr.Args.AddRange(ParseAttrArgs());
                }

                return attr;
            }

            return null;
        }

        public List<AttrArgument> ParseAttrArgs()
        {
            var args = new List<AttrArgument>();
            bool namedArgs = false;
            AttrArgument arg;
            if (!Accept(Sym.CloseParen)) {
                do {
                    if (Expect(ParseAttrArgument, "Expected expression or named argument", out arg)) {
                        if (namedArgs) {
                            //All subsequent arguments must be named-args
                            if (!(arg is NamedAttrArgument)) {
                                result.Error("Named argument expected", arg.StartToken);
                                continue;
                            }

                            args.Add(arg);
                        } else {
                            if (arg is NamedAttrArgument)
                                namedArgs = true;

                            args.Add(arg);
                        }
                    }
                } while (Accept(Sym.Comma));

                Expect(Sym.CloseParen);
            }

            return args;
        }

        public AttrArgument ParseAttrArgument()
        {
            using (var la = new LookaheadFrame(lexer)) {
                //Try named-arg first, using two-token lookahead
                //(a positional arg is just an expression, which can 
                //be an id)
                Token start;
                if (Accept(Sym.Id, out start) && Accept(Sym.AssignEqual)) {
                    Exp exp;
                    if (Expect(ParseExp, "Expected expression", out exp)) {
                        var na = new NamedAttrArgument(start);
                        na.Name = new IdExp(start);
                        na.Exp = exp;
                        la.Commit();
                        return na;
                    }
                }
            }

            //Otherwise, backtrack and try a positional arg
            Exp argExp;
            if (Expect(ParseExp, "Expected expression", out argExp)) {
                var pa = new PositionalAttrArgument(argExp.StartToken);
                pa.Exp = argExp;
                return pa;
            }

            return null;
        }

        public List<NamespaceMemberDeclaration> ParseNamespaceMemberDecls()
        {
            throw new NotImplementedException();
        }

        public LiteralExp ParseLiteral()
        {
            var tok = lexer.Read();
            switch (tok.Sym) {
                case Sym.IntLiteral:
                    return new IntLiteralExp(tok);
                case Sym.RealLiteral:
                    return new RealLiteralExp(tok);
                case Sym.CharLiteral:
                    return new CharLiteralExp(tok);
                case Sym.StringLiteral:
                    return new StringLiteralExp(tok);
                case Sym.KwNull:
                    return new NullLiteralExp(tok);
                case Sym.KwFalse:
                case Sym.KwTrue:
                    return new BooleanLiteralExp(tok);
            }

            return null;
        }

        /// <summary>
        /// primary-no-array-creation-exp :=
        ///     invocation-exp
        ///     member-access
        ///     element-access
        ///     post-incr-exp
        ///     post-decr-exp
        ///     simple-name
        ///     literal
        ///     parenthesized-exp
        ///     this-access
        ///     base-access
        ///     object-creation-exp
        ///     delegate-creation-exp
        ///     typeof-exp
        ///     sizeof-exp
        ///     checked-exp
        ///     unchecked-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParsePrimaryNoArrayCreationExp()
        {
            //TODO: All other cases besides literals
            return ApplyRule(ParseLiteral);
            
        }

        /// <summary>
        /// primary-exp :=
        ///     primary-no-array-creation-exp
        ///     array-creation-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParsePrimaryExp()
        {
            //TODO: Array creation expressions
            return ApplyRule(ParsePrimaryNoArrayCreationExp);
        }

        /// <summary>
        /// pre-incr-exp := '++' unary-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParsePreIncrExp()
        {
            Token tok;
            if (Accept(Sym.PlusPlus, out tok)) {
                var exp = ApplyRule(ParseUnaryExp);
                if (null != exp)
                    return new PreIncrExp(tok) { Exp = exp };
            }

            return null;
        }

        /// <summary>
        /// pre-decr-exp := '--' unary-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParsePreDecrExp()
        {
            Token tok;
            if (Accept(Sym.MinusMinus, out tok)) {
                var exp = ApplyRule(ParseUnaryExp);
                if (null != exp)
                    return new PreDecrExp(tok) { Exp = exp };
            }

            return null;
        }

        /// <summary>
        /// cast-exp := '(' qualified-id ')' unary-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParseCastExp()
        {
            Token tok;
            if (Accept(Sym.OpenParen, out tok)) {
                var typeId = ApplyRule(ParseQualifiedId);
                if (null != typeId) {
                    Expect(Sym.CloseParen);
                    var exp = ApplyRule(ParseUnaryExp);
                    if (null != exp)
                        return new CastExp(tok) { TargetType = typeId, Exp = exp };
                }
            }

            return null;
        }

        /// <summary>
        /// unary-exp :=
        ///     '+' unary-exp
        ///     '-' unary-exp
        ///     '!' unary-exp
        ///     '~' unary-exp 
        ///     '*' unary-exp (???)
        ///     pre-incr-exp
        ///     pre-decr-exp
        ///     cast-exp
        ///     primary-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParseUnaryExp()
        {
            var t = lexer.PeekToken();
            Func<Op, Exp> un = (op) =>
            {
                lexer.Read();
                return new UnaryExp(t, op) { Exp = ApplyRule(ParseUnaryExp) };
            };

            switch (t.Sym) {
                case Sym.Plus:
                    return un(Op.Plus);
                case Sym.Minus:
                    return un(Op.Minus);
                case Sym.Not:
                    return un(Op.Not);
                case Sym.BitwiseNot:
                    return un(Op.BitwiseNot);
                case Sym.PlusPlus:
                    return ApplyRule(ParsePreIncrExp);
                case Sym.MinusMinus:
                    return ApplyRule(ParsePreDecrExp);
                case Sym.OpenParen:
                    return ApplyRule(ParseCastExp);
                default:
                    return ApplyRule(ParsePrimaryExp);
            }
        }

        /// <summary>
        /// mult-exp :=
        ///     mult-exp '*' unary-exp
        ///     mult-exp '/' unary-exp
        ///     mult-exp '%' unary-exp
        ///     unary-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParseMultExp()
        {
            var lhs = ApplyRule(ParseMultExp);
            if (null == lhs)
                return ApplyRule(ParseUnaryExp);

            if (Accept(Sym.Mult))
                return new MultiplicativeExp(lhs.StartToken, Op.Multiply) { Lhs = lhs, Rhs = ApplyRule(ParseUnaryExp) };
            else if (Accept(Sym.Div))
                return new MultiplicativeExp(lhs.StartToken, Op.Multiply) { Lhs = lhs, Rhs = ApplyRule(ParseUnaryExp) };
            else if (Accept(Sym.Mod))
                return new MultiplicativeExp(lhs.StartToken, Op.Mod) { Lhs = lhs, Rhs = ApplyRule(ParseUnaryExp) };

            return lhs;
        }

        /// <summary>
        /// additive-exp :=
        ///     additive-exp '+' mult-exp
        ///     additive-exp '-' mult-exp
        ///     mult-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParseAdditiveExp()
        {
            var lhs = ApplyRule(ParseAdditiveExp);
            if (null == lhs)
                return ApplyRule(ParseMultExp);

            if (Accept(Sym.Plus))
                return new AdditiveExp(lhs.StartToken, Op.Plus) { Lhs = lhs, Rhs = ApplyRule(ParseMultExp) };
            else if (Accept(Sym.Minus))
                return new AdditiveExp(lhs.StartToken, Op.Minus) { Lhs = lhs, Rhs = ApplyRule(ParseMultExp) };

            return lhs;
        }

        /// <summary>
        /// shift-exp :=
        ///     shift-exp '&lt;&lt;' additive-exp
        ///     shift-exp '>>' additive-exp
        ///     additive-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParseShiftExp()
        {
            var lhs = ApplyRule(ParseShiftExp);
            if (null == lhs) {
                return ApplyRule(ParseAdditiveExp);
            }

            if (Accept(Sym.LeftShift))
                return new ShiftExp(lhs.StartToken, Op.LeftShift) { Lhs = lhs, Rhs = ApplyRule(ParseAdditiveExp) };
            else if (Accept(Sym.RightShift))
                return new ShiftExp(lhs.StartToken, Op.RightShift) { Lhs = lhs, Rhs = ApplyRule(ParseAdditiveExp) };

            return lhs;
        }

        /// <summary>
        /// relational-exp :=
        ///     relational-exp '&lt;' shift-exp
        ///     relational-exp '>' shift-exp
        ///     relational-exp '&lt;=' shift-exp
        ///     relational-exp '>=' shift-exp 
        ///     relational-exp 'is' qualified-id
        ///     relational-exp 'as' qualified-id
        ///     shift-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParseRelationalExp()
        {
            var lhs = ApplyRule(ParseRelationalExp);
            if (null == lhs)
                return ApplyRule(ParseShiftExp);

            Func<Op, Exp> getRel = (op) => new RelationalExp(lhs.StartToken, op) { Lhs = lhs, Rhs = ApplyRule(ParseShiftExp) };

            if (Accept(Sym.LessThan))
                getRel(Op.LessThan);
            else if (Accept(Sym.GreaterThan))
                getRel(Op.GreaterThan);
            else if (Accept(Sym.LtEqual))
                getRel(Op.LessThanEqual);
            else if (Accept(Sym.GtEqual))
                getRel(Op.GreaterThanEqual);
            else if (Accept(Sym.KwIs))
                return new RelationalExp(lhs.StartToken, Op.Is) { Lhs = lhs, Rhs = ApplyRule(ParseQualifiedId) };
            else if (Accept(Sym.KwAs))
                return new RelationalExp(lhs.StartToken, Op.As) { Lhs = lhs, Rhs = ApplyRule(ParseQualifiedId) };

            return lhs;
        }

        /// <summary>
        /// equality-exp :=
        ///     equality-exp '==' relational-exp
        ///     equality-exp '!=' relational-exp
        ///     relational-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParseEqualityExp()
        {
            var lhs = ApplyRule(ParseEqualityExp);
            if (null == lhs) {
                return ApplyRule(ParseRelationalExp);
            }

            if (Accept(Sym.EqualEqual)) {
                return new EqualityExp(lhs.StartToken, Op.EqualEqual) { Lhs = lhs, Rhs = ApplyRule(ParseRelationalExp) };
            } else if (Accept(Sym.NotEqual)) {
                return new EqualityExp(lhs.StartToken, Op.NotEqual) { Lhs = lhs, Rhs = ApplyRule(ParseRelationalExp) };
            }

            return lhs;
        }

        /// <summary>
        /// and-exp :=
        ///     and-exp '&' equality-exp
        ///     equality-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParseAndExp()
        {
            var lhs = ApplyRule(ParseAndExp);
            if (null == lhs) {
                return ApplyRule(ParseEqualityExp);
            }

            if (Accept(Sym.BitwiseAnd)) {
                var rhs = ApplyRule(ParseEqualityExp);
                return new BitwiseAndExp(lhs.StartToken) { Lhs = lhs, Rhs = rhs };
            }

            return lhs;
        }

        /// <summary>
        /// exclusive-or-exp :=
        ///     exclusive-or-exp '^' and-exp
        ///     and-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParseExclusiveOrExp()
        {
            var lhs = ApplyRule(ParseExclusiveOrExp);
            if (null == lhs) {
                return ApplyRule(ParseAndExp);
            }

            if (Accept(Sym.BitwiseXor)) {
                var rhs = ApplyRule(ParseExclusiveOrExp);
                return new ExclusiveOrExp(lhs.StartToken) { Lhs = lhs, Rhs = rhs };
            }

            return lhs;
        }

        /// <summary>
        /// inclusive-or-exp :=
        ///     inclusive-or-exp '|' exclusive-or-exp 
        ///     exclusive-or-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParseInclusiveOrExp()
        {
            var lhs = ApplyRule(ParseInclusiveOrExp);
            if (null == lhs) {
                return ApplyRule(ParseExclusiveOrExp);
            }

            if (Accept(Sym.BitwiseOr)) {
                var rhs = ApplyRule(ParseExclusiveOrExp);
                return new InclusiveOrExp(lhs.StartToken) { Lhs = lhs, Rhs = rhs };
            }

            return lhs;
        }

        /// <summary>
        /// conditional-and-exp :=
        ///     conditional-and-exp '&&' inclusive-or-exp
        ///     inclusive-or-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParseConditionalAndExp()
        {
            var lhs = ApplyRule(ParseConditionalAndExp);
            if (null == lhs) {
                return ApplyRule(ParseInclusiveOrExp);
            }

            if (Accept(Sym.BooleanAnd)) {
                var rhs = ApplyRule(ParseConditionalAndExp);
                return new ConditionalAndExp(lhs.StartToken) { Lhs = lhs, Rhs = rhs };
            }

            return lhs;
        }

        /// <summary>
        /// conditional-or-exp :=
        ///     conditional-or-exp '||' conditional-and-exp
        ///     conditional-and-exp 
        /// </summary>
        /// <returns></returns>
        public Exp ParseConditionalOrExp()
        {
            var lhs = ApplyRule(ParseConditionalOrExp);
            if (null == lhs) {
                return ApplyRule(ParseConditionalAndExp);
            }

            if (Accept(Sym.BooleanOr)) {
                var rhs = ApplyRule(ParseConditionalAndExp);
                return new ConditionalOrExp(lhs.StartToken) { Lhs = lhs, Rhs = rhs };
            }

            return lhs;
        }

        /// <summary>
        /// conditional-exp :=
        ///     conditional-or-exp 
        ///     conditional-or-exp '?' exp ':' exp
        /// </summary>
        /// <returns></returns>
        public Exp ParseConditionalExp()
        {
            var test = ApplyRule(ParseConditionalOrExp);
            if (Accept(Sym.QuestionMark)) {
                var @then = ApplyRule(ParseExp);
                Expect(Sym.Colon);
                var @else = ApplyRule(ParseExp);

                return new ConditionalExp(test.StartToken) { Then = @then, Else = @else };
            }

            return test;
        }

        /// <summary>
        /// assignment-exp :=
        ///     unary-exp assignment-op exp
        /// </summary>
        /// <returns></returns>
        public Exp ParseAssignmentExp()
        {
            var lhs = ApplyRule(ParseUnaryExp);
            if (null != lhs) {
                var opTok = lexer.PeekToken();
                Op theOp;
                switch (opTok.Sym) {
                    case Sym.AssignEqual:
                        theOp = Op.Equal;
                        break;
                    case Sym.PlusEqual:
                        theOp = Op.AddEqual;
                        break;
                    case Sym.MinusEqual:
                        theOp = Op.SubtractEqual;
                        break;
                    case Sym.MultEqual:
                        theOp = Op.MultiplyEqual;
                        break;
                    case Sym.DivEqual:
                        theOp = Op.DivideEqual;
                        break;
                    case Sym.ModEqual:
                        theOp = Op.ModEqual;
                        break;
                    case Sym.BitwiseAndEqual:
                        theOp = Op.BitwiseAndEqual;
                        break;
                    case Sym.BitwiseOrEqual:
                        theOp = Op.BitwiseOrEqual;
                        break;
                    case Sym.BitwiseXorEqual:
                        theOp = Op.BitwiseXorEqual;
                        break;
                    case Sym.LeftShiftEqual:
                        theOp = Op.LeftShiftEqual;
                        break;
                    case Sym.RightShiftEqual:
                        theOp = Op.RightShiftEqual;
                        break;
                    default:
                        return null;
                }

                lexer.Read();
                var rhs = ApplyRule(ParseExp);
                if (null != lhs) {
                    return new AssignmentExp(lhs.StartToken, theOp) { Lhs = lhs, Rhs = rhs };
                }
            }

            return null;
        }

        /// <summary>
        /// exp :=
        ///     conditional-exp
        ///     assignment-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParseExp()
        {
            using (var la = new LookaheadFrame(lexer)) {
                var e = ApplyRule(ParseAssignmentExp);
                if (null != e) {
                    la.Commit();
                    return e;
                }
            }

            return ApplyRule(ParseConditionalExp);
        }

        /// <summary>
        /// qualified-id := id ('.' id)*
        /// </summary>
        /// <returns></returns>
        public QualifiedIdExp ParseQualifiedId()
        {
            using (var la = new LookaheadFrame(lexer)) {
                var t = lexer.Read();
                if (t.Is(Sym.Id)) {
                    var qid = new QualifiedIdExp(t);
                    qid.Parts.Add(new IdExp(t));
                    while (Accept(Sym.Dot)) {
                        t = lexer.Read();
                        if (t.Is(Sym.Id)) {
                            qid.Parts.Add(new IdExp(t));
                            continue;
                        }

                        //Error -- can't end a qualified-id (or any expression) with a '.'
                        result.Error("Expected identifier", t);
                        return null;
                    }

                    la.Commit();
                    return qid;
                }
            }

            //Not a qualified-id at all, so no error here
            return null;
        }
    }
}
