using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monticello.Parsing
{
    public class Parser
    {
        private string input;
        private TokenBuffer buf;
        private ParseResult result;

        public Parser(string input)
        {
            this.input = input;
            this.buf = new TokenBuffer(new Lexer(input));
            this.result = new ParseResult();
        }

        private bool Expect<T>(
            Func<T> parser, 
            string errMsg, 
            out T ast)
            where T : AstNode
        {
            ast = parser();
            if (null == ast) {
                result.Error(errMsg, buf);
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
            tok = buf.Next();
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
            if (buf.Peek().Is(expected)) {
                buf.Next();
                return true;
            }

            return false;
        }

        private bool Accept(Sym expected, out Token tok)
        {
            if (buf.Peek().Is(expected)) {
                tok = buf.Next();
                return true;
            }

            tok = null;
            return false;
        }

        public static CompilationUnit Parse(string input)
        {
            return new Parser(input).ParseCompilationUnit();
        }

        public CompilationUnit ParseCompilationUnit()
        {
            var usings = ParseUsingDirectives();
            //var decls = ParseNamespaceMemberDecls(buf, result);

            var cu = new CompilationUnit();
            cu.Usings.AddRange(usings);
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
            while (Accept(ParseGlobalAttr, out section)) {
                sections.Add(section);
            }

            return sections;
        }

        public AttrSection ParseGlobalAttr()
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
            using (var la = new LookaheadFrame(buf)) {
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

        public Exp ParseExp()
        {
            //Only parse simple literals for now, so we can 
            //test attribute parsing
            var next = buf.Peek();
            switch (next.Sym) {
                case Sym.RealLiteral:
                case Sym.IntLiteral:
                case Sym.HexIntLiteral:
                    buf.Next();
                    return new NumericLiteralExp(next);
                case Sym.StringLiteral:
                    buf.Next();
                    return new StringLiteralExp(next);
                case Sym.CharLiteral:
                    buf.Next();
                    return new CharLiteralExp(next);
            }

            return null;
        }

        public QualifiedIdExp ParseQualifiedId()
        {
            using (var la = new LookaheadFrame(buf)) {
                var t = buf.Next();
                if (t.Is(Sym.Id)) {
                    var qid = new QualifiedIdExp(t);
                    qid.Parts.Add(new IdExp(t));
                    while (Accept(Sym.Dot)) {
                        t = buf.Next();
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
