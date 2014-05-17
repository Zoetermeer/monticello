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
        /// using-directive :=
        ///     using-alias-directive
        ///     using-namespace-directive
        ///     
        /// using-alias-directive := using id = qualified-id ;
        /// using-namespace-directive := using qualified-id ;
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public List<UsingDirective> ParseUsingDirectives()
        {
            var usings = new List<UsingDirective>();
            while (true) {
                using (var la = new LookaheadFrame(buf)) {
                    Token start;
                    if (Accept(Sym.KwUsing, out start)) {
                        var qid = ParseQualifiedId();
                        if (null != qid) {
                            //If the qualified-id has only a single part, 
                            //this can be a using-alias directive; otherwise, 
                            //must be a using-namespace 
                            if (qid.Parts.Count == 1) {
                                if (Accept(Sym.AssignEqual)) {
                                    //Using-alias directive
                                    var rhs = ParseQualifiedId();
                                    if (null == rhs) {
                                        result.Error("Expected namespace or type name", buf);
                                        break;
                                    }

                                    if (Accept(Sym.Semicolon)) {
                                        var uad = new UsingAliasDirective(start);
                                        uad.Alias = qid.Parts.First();
                                        uad.NamespaceOrTypeName = rhs;
                                        usings.Add(uad);
                                        la.Commit();
                                        continue;
                                    }

                                    result.Error("Expected semicolon", buf);
                                    break;
                                }
                            }

                            //Using-namespace 
                            if (Accept(Sym.Semicolon)) {
                                var und = new UsingNamespaceDirective(start);
                                und.NamespaceName = qid;
                                usings.Add(und);
                                la.Commit();
                                continue;
                            }   

                            result.Error("Expected semicolon", buf);
                            break;
                        }

                        result.Error("Expected identifier", buf);
                        break;
                    }

                    //Otherwise, not a using
                    break;
                }
            }

            return usings;
        }

        public List<AttrSection> ParseGlobalAttrs()
        {
            var sections = new List<AttrSection>();
            AttrSection section;
            while (null != (section = ParseGlobalAttr())) {
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
                    do {
                        Attr attr;
                        if (!Accept(ParseAttribute, out attr))
                            break;

                        section.Attrs.Add(attr);
                    } while (Accept(Sym.Comma));

                    Expect(Sym.CloseIndexer);                        
                }

                return section;
            }

            return null;
        }

        public Attr ParseAttribute()
        {
            QualifiedIdExp qid;
            if (Expect(ParseQualifiedId, "Expected identifier", out qid)) {
                var attr = new Attr(qid.StartToken);
                attr.AttrTypeName = qid;
                return attr;
            }

            return null;
        }

        public List<NamespaceMemberDeclaration> ParseNamespaceMemberDecls()
        {
            throw new NotImplementedException();
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
