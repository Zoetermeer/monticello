using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monticello.Parsing {
    public abstract class AstNode {
        protected AstNode(Token startToken)
        {
            this.StartToken = startToken;
        }

        public Token StartToken { get; set; }
    }


    public class CompilationUnit : AstNode {
        List<UsingDirective> usings = new List<UsingDirective>();
        List<NamespaceMemberDeclaration> decls = new List<NamespaceMemberDeclaration>();

        public CompilationUnit()
            : base(null)
        {

        }

        public List<UsingDirective> Usings { get { return usings; } }
        public List<NamespaceMemberDeclaration> Decls { get { return decls; } }
    }


    public abstract class NamespaceMemberDeclaration : AstNode {
        protected NamespaceMemberDeclaration(Token start)
            : base(start)
        {

        }
    }


    public class NamespaceDeclaration : NamespaceMemberDeclaration {
        private List<UsingDirective> usings = new List<UsingDirective>();
        private List<NamespaceMemberDeclaration> decls = new List<NamespaceMemberDeclaration>();

        public NamespaceDeclaration(Token start)
            : base(start)
        {

        }

        public IdExp QualifiedId { get; set; }
        public List<UsingDirective> Usings { get { return usings; } }
        public List<NamespaceMemberDeclaration> Decls { get { return decls; } }
    }


    public abstract class TypeDeclaration : NamespaceMemberDeclaration {
        public TypeDeclaration(Token start) 
            : base(start)
        {

        }
    }


    public abstract class UsingDirective : AstNode {
        public UsingDirective(Token start)
            : base(start)
        {

        }
    }


    public class UsingNamespaceDirective : UsingDirective {
        public UsingNamespaceDirective(Token start)
            : base(start)
        {

        }

        public QualifiedIdExp NamespaceName { get; set; }
    }


    public class UsingAliasDirective : UsingDirective {
        public UsingAliasDirective(Token start)
            : base(start)
        {

        }

        public IdExp Alias { get; set; }
        public QualifiedIdExp NamespaceOrTypeName { get; set; }
    }


    public enum AttrTarget {
        Assembly,
        Module,
        Field,
        Event,
        Method,
        Param,
        Property,
        Return,
        Type
    }


    public class AttrSection : AstNode {
        private List<Attr> attrs = new List<Attr>();

        public AttrSection(Token start)
            : base(start)
        {

        }

        public AttrTarget Target { get; set; }
        public List<Attr> Attrs { get { return attrs; } }
    }


    public class Attr : AstNode {
        public Attr(Token start)
            : base(start)
        {

        }

        public QualifiedIdExp AttrTypeName { get; set; }
    }


    public abstract class Exp : AstNode {
        protected Exp(Token start)
            : base(start)
        {

        }
    }


    public class IdExp : Exp {
        public IdExp(Token spelling)
            : base(spelling)
        {
            this.Spelling = spelling;
        }

        public Token Spelling { get; set; }
    }


    public class QualifiedIdExp : Exp {
        private List<IdExp> parts = new List<IdExp>();

        public QualifiedIdExp(Token start)
            : base(start)
        {

        }

        public List<IdExp> Parts { get { return parts; } }

        public bool PartsAre(params string[] parts)
        {
            if (Parts.Count != parts.Length)
                return false;

            for (int i = 0; i < Parts.Count; ++i) {
                if (Parts[i].Spelling.Value.CompareTo(parts[i]) != 0)
                    return false;
            }

            return true;
        }
    }


    public class IntLiteralExp : Exp {
        public IntLiteralExp(Token start)
            : base(start)
        {

        }
    }


    public class FloatLiteralExp : Exp {
        public FloatLiteralExp(Token start)
            : base(start)
        {

        }
    }
}
