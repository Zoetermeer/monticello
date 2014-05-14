using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Covenant.Parsing
{
    public abstract class AstNode
    {

    }

    public class CompilationUnit : AstNode
    {
        List<UsingDirective> usings = new List<UsingDirective>();
        List<NamespaceMemberDeclaration> decls = new List<NamespaceMemberDeclaration>();

        public List<UsingDirective> Usings { get { return usings; } }
        public List<NamespaceMemberDeclaration> Decls { get { return decls; } }
    }

    public abstract class NamespaceMemberDeclaration : AstNode
    {

    }

    public class NamespaceDeclaration : NamespaceMemberDeclaration
    {
        private List<UsingDirective> usings = new List<UsingDirective>();
        private List<NamespaceMemberDeclaration> decls = new List<NamespaceMemberDeclaration>();

        public IdExp QualifiedId { get; set; }
        public List<UsingDirective> Usings { get { return usings; } }
        public List<NamespaceMemberDeclaration> Decls { get { return decls; } }
    }

    public abstract class TypeDeclaration : NamespaceMemberDeclaration
    {

    }

    public abstract class UsingDirective : AstNode
    {

    }

    public class UsingNamespaceDirective : UsingDirective
    {
        public IdExp NamespaceName { get; set; }
    }

    public class UsingAliasDirective : UsingDirective
    {
        public IdExp Ident { get; set; }
        public IdExp NamespaceOrTypeName { get; set; }
    }

    public abstract class Exp : AstNode
    {

    }

    public class IdExp : Exp
    {
        public string Value { get; set; }
    }

    public class IntLiteralExp : Exp
    {

    }

    public class FloatLiteralExp : Exp
    {

    }
}
