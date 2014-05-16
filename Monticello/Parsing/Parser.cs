using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monticello.Parsing
{
    public class Parser
    {
        private static bool Expect(TokenBuffer buf, Sym expected)
        {
            return buf.Next().Is(expected);
        }

        private static bool Accept(TokenBuffer buf, Sym expected)
        {
            if (buf.Peek().Is(expected)) {
                buf.Next();
                return true;
            }

            return false;
        }

        public static CompilationUnit Parse(string input)
        {
            var result = new ParseResult();
            return ParseCompilationUnit(new TokenBuffer(new Lexer(input)), result);
        }

        public static CompilationUnit ParseCompilationUnit(TokenBuffer buf, ParseResult result)
        {
            var usings = ParseUsingDirectives(buf, result);
            //var decls = ParseNamespaceMemberDecls(buf, result);

            var cu = new CompilationUnit();
            cu.Usings.AddRange(usings);
            //cu.Decls.AddRange(decls);

            return cu;
        }

        public static List<UsingDirective> ParseUsingDirectives(TokenBuffer buf, ParseResult result)
        {
            var usings = new List<UsingDirective>();
            while (true) {
                using (var la = new LookaheadFrame(buf)) {
                    if (Accept(buf, Sym.KwUsing)) {
                        var qid = ParseQualifiedId(buf, result);
                        if (null != qid) {
                            //If the qualified-id has only a single part, 
                            //this can be a using-alias directive; otherwise, 
                            //must be a using-namespace 
                            if (qid.Parts.Count == 1) {
                                if (Accept(buf, Sym.AssignEqual)) {
                                    //Using-alias directive
                                    var rhs = ParseQualifiedId(buf, result);
                                    if (null == rhs) {
                                        result.Error("Expected namespace or type name", buf);
                                        break;
                                    }

                                    if (Accept(buf, Sym.Semicolon)) {
                                        var uad = new UsingAliasDirective();
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
                            if (Accept(buf, Sym.Semicolon)) {
                                var und = new UsingNamespaceDirective();
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

        public static List<NamespaceMemberDeclaration> ParseNamespaceMemberDecls(TokenBuffer buf, ParseResult result)
        {
            throw new NotImplementedException();
        }

        public static QualifiedIdExp ParseQualifiedId(TokenBuffer buf, ParseResult result)
        {
            using (var la = new LookaheadFrame(buf)) {
                var t = buf.Next();
                if (t.Is(Sym.Id)) {
                    var qid = new QualifiedIdExp();
                    qid.Parts.Add(new IdExp(t));
                    while (Accept(buf, Sym.Dot)) {
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
