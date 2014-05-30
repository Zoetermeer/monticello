using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using Monticello.Common;

namespace Monticello.Parsing
{
    [DebuggerDisplay("{Method}")]
    public delegate T Rule<out T>();

    public class RuleComparer : IComparer<Rule<AstNode>> {
        public int Compare(Rule<AstNode> x, Rule<AstNode> y)
        {
            return x.Method.Name.CompareTo(y.Method.Name);
        }
    }

    public class Parser
    {
        public class Head {
            private SortedSet<Rule<AstNode>> involvedSet = new SortedSet<Rule<AstNode>>(new RuleComparer());
            private SortedSet<Rule<AstNode>> evalSet = new SortedSet<Rule<AstNode>>(new RuleComparer());
            
            public Head(Rule<AstNode> rule)
            {
                this.Rule = rule;
            }

            public Rule<AstNode> Rule { get; set; }
            public SortedSet<Rule<AstNode>> InvolvedSet { get { return involvedSet; } }
            public SortedSet<Rule<AstNode>> EvalSet 
            { 
                get { return evalSet; }
                set { evalSet = value; }
            }
            
        }


        public class LR : AstNode {
            public LR(AstNode seed, Rule<AstNode> rule, Head head, LR next) 
                : base(null)
            {
                this.Seed = seed;
                this.Rule = rule;
                this.Head = head;
                this.Next = next;
            }

            public AstNode Seed { get; set; }
            public Rule<AstNode> Rule { get; set; }
            public Head Head { get; set; }
            public LR Next { get; set; }

            public override string ToString()
            {
                return StringFormatting.SExp("lr", Seed, Rule, Head, (null == Next ? "<null>" : "more lrs..."));
            }
        }

        private Lexer lexer;
        private ParseResult result;
        private MemoTable memoTable = new MemoTable();
        private LR lrStack;
        private Dictionary<int, Head> heads = new Dictionary<int, Head>();

        public Parser(string input)
        {
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

        private static bool Accept<T>(Func<T> parser, out T ast) 
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

        private static Tuple<string, int> MemoKey(Rule<AstNode> rule, int pos)
        {
            return Tuple.Create(rule.Method.Name, pos);
        }

        private T GrowLR<T>(Rule<T> rule, int p, MemoEntry m, Head h) 
            where T : AstNode
        {
            heads[p] = h;
            while (true) {
                lexer.Pos = p;

                h.EvalSet = new SortedSet<Rule<AstNode>>(h.InvolvedSet, new RuleComparer());

                var ans = rule();
                if (null == ans || lexer.Pos <= m.Pos)
                    break;

                m.Ast = ans;
                m.Pos = lexer.Pos;
            }

            heads[p] = null;

            lexer.Pos = m.Pos;
            return m.Ast as T;
        }

        private T LRAnswer<T>(Rule<T> rule, int p, MemoEntry m)
            where T : AstNode
        {
            #region Contracts
            Contract.Requires(m != null);
            Contract.Requires(m.Ast != null);
            Contract.Requires(m.Ast is LR);
            #endregion

            var lr = m.Ast as LR;
            var h = lr.Head;
            if (h.Rule != rule) 
                return lr.Seed as T;

            m.Ast = lr.Seed;
            if (m.Ast == null)
                return null;

            return GrowLR(rule, p, m, h);
        }

        private MemoEntry Recall<T>(Rule<T> rule, int p)
            where T : AstNode
        {
            MemoEntry m = null;
            memoTable.TryGetValue(MemoKey(rule, p), out m);
            Head h = null;
            heads.TryGetValue(p, out h);
            
            //If not growing a seed parse, just return what is stored 
            //in the memo table
            if (null == h) {
                return m;
            }

            //Do not evaluate any rule that is not 
            //involved in this left recursion
            if (null == m && rule != h.Rule && !h.InvolvedSet.Contains(rule))
                return new MemoEntry(p);

            //Allow involved rules to be evaluated, 
            //but only once, during a seed-growing iteration
            if (h.EvalSet.Contains(rule)) {
                h.EvalSet.Remove(rule);
                var ans = rule();
                m.Ast = ans;
                m.Pos = lexer.Pos;
            }

            return m;
        }

        private void SetupLR<T>(Rule<T> rule, LR l)
            where T : AstNode
        {
            #region Contracts
            Contract.Requires(null != lrStack);
            #endregion

            if (null == l.Head)
                l.Head = new Head(rule);

            var s = lrStack;
            while (s.Head != l.Head) {
                s.Head = l.Head;
                l.Head.InvolvedSet.Add(s.Rule);
                s = s.Next;
            }
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
            var key = MemoKey(rule, pos);
            Debug.WriteLine("Apply rule ({0}): {1}", pos, key.Item1);
            MemoEntry m = Recall(rule, pos);
            LR lr;
            if (null == m) {
                //Create a new LR and push it onto 
                //the rule invocation stack
                lr = new LR(null, rule, null, lrStack);
                lrStack = lr;

                //Memoize lr, then evaluate rule
                m = new MemoEntry(pos, lr);
                memoTable.Add(key, m);

                var ans = rule();

                //Pop lr off the rule invocation stack
                lrStack = lrStack.Next;
                m.Pos = lexer.Pos;
                if (lr.Head != null) {
                    lr.Seed = ans;
                    return LRAnswer(rule, pos, m);
                } else {
                    m.Ast = ans;
                    return ans;
                }
            }

            lexer.Pos = m.Pos;
            lr = m.Ast as LR;
            if (null != lr) {
                SetupLR(rule, lr);
                return lr.Seed as T;
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
        /// argument :=
        ///     'ref' exp
        ///     'out' exp
        ///     exp
        /// </summary>
        /// <returns></returns>
        public ArgumentExp ParseArgument()
        {
            bool isRef = false, isOut = false;
            Token t = lexer.PeekToken(), start = null;
            switch (t.Sym) {
                case Sym.KwRef:
                    isRef = true;
                    start = t;
                    lexer.Read();
                    break;
                case Sym.KwOut:
                    isOut = true;
                    start = t;
                    lexer.Read();
                    break;
            }

            var e = ApplyRule(ParseExp);
            if (null != e)
                return new ArgumentExp(start ?? e.StartToken) { Exp = e, IsOut = isOut, IsRef = isRef };

            return null;
        }

        /// <summary>
        /// argument-list := 
        ///     argument (',' argument)*
        ///     ε
        /// </summary>
        /// <returns></returns>
        public List<ArgumentExp> ParseArgumentList()
        {
            var args = new List<ArgumentExp>();
            ArgumentExp ae;
            if (Accept(ParseArgument, out ae)) {
                args.Add(ae);
                while (Accept(Sym.Comma)) {
                    //Try to continue as long as we get 
                    //commas
                    if (Expect(ParseArgument, "Expected argument expression", out ae))
                        args.Add(ae);
                }
            }

            return args;
        }


        /// <summary>
        /// exp-list :=
        ///     exp
        ///     exp-list ',' exp
        /// </summary>
        /// <returns></returns>
        public Exp ParseExpList()
        {
            var es = new ExpList(lexer.PeekToken());
            var e = ApplyRule(ParseExp);
            if (null != e) {
                es.Exps.Add(e);
                while (Accept(Sym.Comma)) {
                    e = ApplyRule(ParseExp);
                    if (null != e)
                        es.Exps.Add(e);
                }

                return es;
            }

            return null;
        }

        /// <summary>
        /// element-access-exp :=
        ///     primary-no-array-creation-exp '[' exp-list ']'
        /// </summary>
        /// <returns></returns>
        public Exp ParseElementAccessExp()
        {
            //TODO: This rule causes a problem, because we can only 
            //grow one left-recursive (indirect) rule at a time.  
            //But trying to apply this one causes primary-no-array-creation-exp 
            //to be the current indirect LR rule being grown, while primary-exp 
            //is already the current one (via invocation-exp/member-access-exp). 
            //How do we solve this? 

            //var e = ApplyRule(ParsePrimaryNoArrayCreationExp);
            //if (null != e && Accept(Sym.OpenIndexer)) {

            //}

            return null;
        }

        /// <summary>
        /// invocation-exp :=
        ///     primary-exp '(' argument-list ')'
        /// </summary>
        /// <returns></returns>
        public Exp ParseInvocationExp()
        {
            var e = ApplyRule(ParsePrimaryExp);
            if (null != e) {
                if (Accept(Sym.OpenParen)) {
                    var args = ParseArgumentList();
                    if (Accept(Sym.CloseParen)) {
                        return new InvocationExp(e.StartToken) { Target = e, Args = args };
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// member-access-exp :=
        ///     primary-exp '.' id
        ///     predefined-type '.' id
        /// </summary>
        /// <returns></returns>
        public Exp ParseMemberAccessExp()
        {
            var e = ApplyRule(ParsePrimaryExp);
            if (null != e) {
                if (Accept(Sym.Dot)) {
                    IdExp ide;
                    if (Expect(ParseId, "Expected identifier", out ide)) {
                        return new MemberAccessExp(e.StartToken) { Target = e, Member = ide };
                    }
                }
            }

            //Second alternative
            e = ApplyRule(ParsePredefinedType);
            if (null != e) {
                if (Expect(Sym.Dot)) {
                    IdExp ide;
                    if (Expect(ParseId, "Expected identifier", out ide)) {
                        return new MemberAccessExp(e.StartToken) { Target = e, Member = ide };
                    }
                }
            }

            return null;
        }

        public Exp ParsePostIncrExp()
        {
            var e = ApplyRule(ParsePrimaryExp);
            if (null != e) {
                if (Accept(Sym.PlusPlus)) {
                    return new PostIncrExp(e.StartToken) { Exp = e };
                }
            }

            return null;
        }

        public Exp ParsePostDecrExp()
        {
            var e = ApplyRule(ParsePrimaryExp);
            if (null != e) {
                if (Accept(Sym.MinusMinus)) {
                    return new PostDecrExp(e.StartToken) { Exp = e };
                }
            }

            return null;
        }

        /// <summary>
        /// this-access := 'this'
        /// </summary>
        /// <returns></returns>
        public Exp ParseThisAccessExp()
        {
            Token t;
            if (Accept(Sym.KwThis, out t))
                return new ThisAccessExp(t);

            return null;
        }

        /// <summary>
        /// base-access :=
        ///     'base' . id                --> base-member-access 
        ///     'base' '[' exp-list ']'    --> base-indexer-access
        /// </summary>
        /// <returns></returns>
        public Exp ParseBaseAccessExp()
        {
            Token t;
            if (Accept(Sym.KwBase, out t)) {
                if (Accept(Sym.Dot)) {
                    IdExp id;
                    if (Expect(ParseId, "Expected identifier", out id)) {
                        return new BaseMemberAccessExp(t) { Member = id };
                    }
                } else if (Accept(Sym.OpenIndexer)) {
                    Exp es;
                    if (Expect(ParseExpList, "Expected expression(s)", out es)) {
                        return new BaseIndexerAccessExp(t, es);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// object-creation :=
        ///     'new' type '(' (arg-list)? ')' (obj-or-collection-initializer)?
        /// </summary>
        /// <returns></returns>
        public Exp ParseObjectCreationExp()
        {
            return null;
        }

        /// <summary>
        /// delegate-creation :=
        ///     'new' delegate-type '(' exp ')'
        /// </summary>
        /// <returns></returns>
        public Exp ParseDelegateCreationExp()
        {
            return null;
        }

        /// <summary>
        /// typeof-exp :=
        ///     'typeof' '(' type ')'
        ///     'typeof' '(' 'void' ')'
        /// </summary>
        /// <returns></returns>
        public Exp ParseTypeofExp()
        {
            return null;
        }

        /// <summary>
        /// sizeof-exp := 'sizeof' '(' unmanaged-type ')'
        /// Policy question: do we want to support this?
        /// </summary>
        /// <returns></returns>
        public Exp ParseSizeofExp()
        {
            return null;
        }

        /// <summary>
        /// checked-exp := 'checked' '(' exp ')'
        /// </summary>
        /// <returns></returns>
        public Exp ParseCheckedExp()
        {
            Token start;
            if (Accept(Sym.KwChecked, out start) && Accept(Sym.OpenParen)) {
                var e = ApplyRule(ParseExp);
                if (null != e && Expect(Sym.CloseParen))
                    return new CheckedExp(start) { Exp = e };

                result.Error("Expected expression", start);
            }

            return null;
        }

        /// <summary>
        /// unchecked-exp := 'unchecked' '(' exp ')'
        /// </summary>
        /// <returns></returns>
        public Exp ParseUncheckedExp()
        {
            Token start;
            if (Accept(Sym.KwUnchecked, out start) && Accept(Sym.OpenParen)) {
                var e = ApplyRule(ParseExp);
                if (null != e && Expect(Sym.CloseParen))
                    return new UncheckedExp(start) { Exp = e };

                result.Error("Expected expression", start);
            }

            return null;
        }

        /// <summary>
        /// default-value-exp := 'default' '(' type ')'
        /// </summary>
        /// <returns></returns>
        public Exp ParseDefaultValueExp()
        {
            return null;
        }

        /// <summary>
        /// parenthesized-exp := '(' exp ')'
        /// </summary>
        /// <returns></returns>
        public Exp ParseParenExp()
        {
            if (Accept(Sym.OpenParen)) {
                var e = ApplyRule(ParseExp);
                if (null != e && Expect(Sym.CloseParen))
                    return e;
            }

            return null;
        }

        /// <summary>
        /// anon-function-sig :=
        ///     explicit-anon-function-sig
        ///     implicit-anon-function-sig
        /// </summary>
        /// <returns></returns>
        public AnonFunctionSig ParseAnonFunctionSig()
        {
            return null;
        }

        /// <summary>
        /// anon-method-exp :=
        /// </summary>
        /// <returns></returns>
        public Exp ParseAnonMethodExp()
        {
            return null;
        }

        /// <summary>
        /// primary-no-array-creation-exp :=
        ///     parenthesized-exp
        ///     invocation-exp
        ///     member-access
        ///     element-access
        ///     post-incr-exp
        ///     post-decr-exp
        ///     simple-name
        ///     literal
        ///     this-access
        ///     base-access
        ///     object-creation-exp
        ///     delegate-creation-exp
        ///     typeof-exp
        ///     sizeof-exp
        ///     checked-exp
        ///     unchecked-exp
        ///     default-value-exp
        ///     anon-method-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParsePrimaryNoArrayCreationExp()
        {
            //TODO: All other cases besides literals
            Func<Rule<Exp>, Exp> tryRule = (rule) => 
                {
                    using (var la = new LookaheadFrame(lexer)) {
                        var e = ApplyRule(rule);
                        if (null != e)
                            la.Commit();

                        return e;
                    }
                };

            Exp exp;
            if (null != (exp = tryRule(ParseParenExp)))
                return exp;
            if (null != (exp = tryRule(ParseInvocationExp)))
                return exp;
            if (null != (exp = tryRule(ParseMemberAccessExp)))
                return exp;
            if (null != (exp = tryRule(ParseElementAccessExp)))
                return exp;
            if (null != (exp = tryRule(ParsePostIncrExp)))
                return exp;
            if (null != (exp = tryRule(ParsePostDecrExp)))
                return exp;
            if (null != (exp = tryRule(ParseThisAccessExp)))
                return exp;
            if (null != (exp = tryRule(ParseBaseAccessExp)))
                return exp;
            if (null != (exp = tryRule(ParseObjectCreationExp)))
                return exp;
            if (null != (exp = tryRule(ParseDelegateCreationExp)))
                return exp;
            if (null != (exp = tryRule(ParseTypeofExp)))
                return exp;
            if (null != (exp = tryRule(ParseSizeofExp)))
                return exp;
            if (null != (exp = tryRule(ParseCheckedExp)))
                return exp;
            if (null != (exp = tryRule(ParseUncheckedExp)))
                return exp;
            if (null != (exp = tryRule(ParseDefaultValueExp)))
                return exp;
            if (null != (exp = tryRule(ParseAnonMethodExp)))
                return exp;
            if (null != (exp = tryRule(ParseLiteral)))
                return exp;
            if (null != (exp = tryRule(ParseId)))
                return exp;

            return null;
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
        /// cast-exp := '(' type-name ')' unary-exp
        /// </summary>
        /// <returns></returns>
        public Exp ParseCastExp()
        {
            Token tok;
            if (Accept(Sym.OpenParen, out tok)) {
                var typeId = ApplyRule(ParseTypeName);
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
                {
                    //Need to backtrack here if not a cast, because a parenthesized-exp 
                    //is a primary exp
                    using (var la = new LookaheadFrame(lexer)) {
                        var e = ApplyRule(ParseCastExp);
                        if (null != e) {
                            la.Commit();
                            return e;
                        }
                    }

                    return ApplyRule(ParsePrimaryExp);
                }
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
            
            if (Accept(Sym.Div))
                return new MultiplicativeExp(lhs.StartToken, Op.Multiply) { Lhs = lhs, Rhs = ApplyRule(ParseUnaryExp) };
            
            if (Accept(Sym.Mod))
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
            
            if (Accept(Sym.RightShift))
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
            }

            if (Accept(Sym.NotEqual)) {
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
                if (null != rhs) {
                    return new AssignmentExp(lhs.StartToken, theOp) { Lhs = lhs, Rhs = rhs };
                }
            }

            return null;
        }

        /// <summary>
        /// lambda-exp :=
        ///     anon-function-sig '=>' anon-function-body
        /// </summary>
        /// <returns></returns>
        public Exp ParseLambdaExp()
        {
            return null;
        }

        /// <summary>
        /// query-exp := from-clause query-body
        /// </summary>
        /// <returns></returns>
        public Exp ParseQueryExp()
        {
            return null;
        }

        public Exp ParseNonAssignmentExp()
        {
            return ApplyRule(ParseConditionalExp);
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

            return ApplyRule(ParseNonAssignmentExp);
        }

        /// <summary>
        /// type-name :=
        ///     predefined-type
        ///     user-type
        /// </summary>
        /// <returns></returns>
        public TypeNameExp ParseTypeName()
        {
            TypeNameExp tn = ParsePredefinedType();
            return tn ?? ParseUserTypeNameExp();
        }

        /// <summary>
        /// predefined-type :=
        ///     bool | byte | char | decimal | double | float | int | long
        ///     object | sbyte | short | string | uint | ulong | ushort
        /// </summary>
        /// <returns></returns>
        public PredefinedTypeNameExp ParsePredefinedType()
        {
            var t = lexer.PeekToken();
            var type = PredefinedType.Unknown;
            var valid = true;
            switch (t.Sym) {
                case Sym.KwBool:
                    type = PredefinedType.Bool;
                    break;
                case Sym.KwByte:
                    type = PredefinedType.Byte;
                    break;
                case Sym.KwChar:
                    type = PredefinedType.Char;
                    break;
                case Sym.KwDecimal:
                    type = PredefinedType.Decimal;
                    break;
                case Sym.KwDouble:
                    type = PredefinedType.Double;
                    break;
                case Sym.KwFloat:
                    type = PredefinedType.Float;
                    break;
                case Sym.KwInt:
                    type = PredefinedType.Int;
                    break;
                case Sym.KwLong:
                    type = PredefinedType.Long;
                    break;
                case Sym.KwObject:
                    type = PredefinedType.Object;
                    break;
                case Sym.KwSbyte:
                    type = PredefinedType.Sbyte;
                    break;
                case Sym.KwShort:
                    type = PredefinedType.Short;
                    break;
                case Sym.KwString:
                    type = PredefinedType.String;
                    break;
                case Sym.KwUint:
                    type = PredefinedType.Uint;
                    break;
                case Sym.KwUlong:
                    type = PredefinedType.Ulong;
                    break;
                case Sym.KwUshort:
                    type = PredefinedType.Ushort;
                    break;
                default:
                    valid = false;
                    break;
            }

            if (valid) {
                lexer.Read();
                return new PredefinedTypeNameExp(t) { Type = type };
            }

            return null;
        }

        /// <summary>
        /// type-arg-list :=
        ///     '&lt;' type (',' type)* '>'
        /// </summary>
        /// <returns></returns>
        public List<TypeNameExp> ParseTypeArgList()
        {
            var targs = new List<TypeNameExp>();
            if (Accept(Sym.LessThan)) {
                do {
                    var ty = ApplyRule(ParseTypeName);
                    if (null == ty) {
                        result.Error("Expected type argument", lexer);
                        continue;
                    }

                    targs.Add(ty);
                } while (Accept(Sym.Comma));
            }

            return targs;
        }

        /// <summary>
        /// (spec rule):
        /// user-type-name :=
        ///     user-type-name '.' id (type-arg-list)? 
        ///     id '::' id (type-arg-list)?                   --> qualified-alias-member
        ///     id (type-arg-list)?
        /// 
        /// (factored):
        /// user-type-name :=
        ///     part (part)*
        ///     
        /// 
        /// part := id (type-arg-list)?
        /// </summary>
        /// <returns></returns>
        public UserTypeNameExp ParseUserTypeNameExp()
        {
            var typeName = new UserTypeNameExp(lexer.PeekToken());
            UserTypeNameExp.Part p;
            var prev = ApplyRule(ParseUserTypeNameExp);
            IdExp id;
            if (null != prev) {
                typeName.Parts.AddRange(prev.Parts);
                if (Accept(Sym.Dot) && Accept(ParseId, out id)) {
                    p = new UserTypeNameExp.Part(id.StartToken) { Id = id };
                    p.TypeArgs.AddRange(ParseTypeArgList());
                    typeName.Parts.Add(p);

                    return typeName;
                }

                return prev;
            }

            if (Accept(ParseId, out id)) {
                p = new UserTypeNameExp.Part(id.StartToken);
                if (Accept(Sym.ScopeResolution)) {
                    //qualified-alias-member
                    p.AliasId = id;
                    if (Expect(ParseId, "Expected identifier", out id)) {
                        p.Id = id;
                        p.TypeArgs.AddRange(ParseTypeArgList());
                        typeName.Parts.Add(p);
                        return typeName;
                    }

                    return null;
                }

                //Otherwise, just id + type-args
                p.Id = id;
                typeName.Parts.Add(p);
                p.TypeArgs.AddRange(ParseTypeArgList());
                return typeName;
            }

            return null;
        }

        /// <summary>
        /// id-exp := id
        /// </summary>
        /// <returns></returns>
        public IdExp ParseId()
        {
            Token idt;
            return Accept(Sym.Id, out idt) ? new IdExp(idt) : null;
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
