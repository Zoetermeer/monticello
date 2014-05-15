using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monticello.Parsing
{
    public class Parser
    {
        public AstNode Parse(string input)
        {
            var lexer = new Lexer(input);
            throw new NotImplementedException();
        }

        public CompilationUnit ParseCompilationUnit(TokenBuffer buf)
        {
            var usings = ParseUsingDirectives(buf);
            var decls = ParseNamespaceMemberDecls(buf);

            var cu = new CompilationUnit();
            cu.Usings.AddRange(usings);
            cu.Decls.AddRange(decls);

            return cu;
        }

        public List<UsingDirective> ParseUsingDirectives(TokenBuffer buf)
        {
            using (new LookaheadFrame(buf))
            {
                var t = buf.Next();

            }

            throw new NotImplementedException();
        }

        public List<NamespaceMemberDeclaration> ParseNamespaceMemberDecls(TokenBuffer buf)
        {
            throw new NotImplementedException();
        }
    }
}
