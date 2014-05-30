using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monticello.Common;

namespace Monticello.Parsing {
    public abstract class AstNode {
        protected AstNode(Token startToken)
        {
            this.StartToken = startToken;
        }

        public Token StartToken { get; set; }
    }


    public enum Op {
        BooleanOr, 
        BooleanAnd, 
        InclusiveOr, 
        ExclusiveOr, 
        BitwiseAnd, 
        EqualEqual, 
        NotEqual, 
        LessThan, 
        GreaterThan, 
        LessThanEqual, 
        GreaterThanEqual, 
        Is, 
        As, 
        LeftShift, 
        RightShift, 
        Plus, 
        Minus, 
        Multiply, 
        Divide, 
        Mod,
        PlusPlus, 
        MinusMinus, 
        Not, 
        BitwiseNot, 
        PointerDeref,
        Equal, 
        AddEqual, 
        SubtractEqual, 
        MultiplyEqual, 
        DivideEqual, 
        ModEqual, 
        BitwiseAndEqual, 
        BitwiseOrEqual, 
        BitwiseXorEqual, 
        LeftShiftEqual, 
        RightShiftEqual
    }


    public enum PredefinedType {
        Unknown, 
        Bool, 
        Byte, 
        Char, 
        Decimal, 
        Double, 
        Dynamic, 
        Float, 
        Int, 
        Long, 
        Object, 
        Sbyte, 
        Short, 
        String, 
        Uint, 
        Ulong, 
        Ushort
    }


    /// <summary>
    /// Top-level AST node for a source file.
    /// </summary>
    public class CompilationUnit : AstNode {
        List<UsingDirective> usings = new List<UsingDirective>();
        List<AttrSection> globalAttrs = new List<AttrSection>();
        List<NamespaceMemberDeclaration> decls = new List<NamespaceMemberDeclaration>();

        public CompilationUnit()
            : base(null)
        {

        }

        public List<UsingDirective> Usings { get { return usings; } }
        public List<AttrSection> GlobalAttributes { get { return globalAttrs; } }
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
        private List<AttrArgument> args = new List<AttrArgument>();
        
        public Attr(Token start)
            : base(start)
        {

        }

        public QualifiedIdExp AttrTypeName { get; set; }
        public List<AttrArgument> Args { get { return args; } }
    }


    public abstract class AttrArgument : AstNode {
        protected AttrArgument(Token start)
            : base(start)
        {

        }

        public Exp Exp { get; set; }
    }

    public class PositionalAttrArgument : AttrArgument {
        public PositionalAttrArgument(Token start) 
            : base(start)
        {

        }

        public int Position { get; set; }
    }


    public class NamedAttrArgument : AttrArgument {
        public NamedAttrArgument(Token start)
            : base(start)
        {

        }

        public IdExp Name { get; set; }
    }


    public abstract class Exp : AstNode {
        protected Exp(Token start)
            : base(start)
        {

        }
    }


    public class ExpList : Exp {
        public ExpList(Token start) 
            : base(start)
        {
            this.Exps = new List<Exp>();
        }

        public List<Exp> Exps { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("(exp-list (");
            sb.AppendItems(items: this.Exps);
            sb.Append("))");
            return sb.ToString();
        }
    }


    public class ConditionalExp : Exp {
        public ConditionalExp(Token start)
            : base(start)
        {

        }

        public Exp Test { get; set; }
        public Exp Then { get; set; }
        public Exp Else { get; set; }
    }


    public abstract class BinaryExp : Exp {
        protected BinaryExp(Token start, Op op)
            : base(start)
        {
            this.Op = op;
        }

        public Op Op { get; set; }
        public Exp Lhs { get; set; }
        public Exp Rhs { get; set; }

        protected string ToString(string name)
        {
            return string.Format("({0} {1} {2})", name, Lhs, Rhs);
        }
    }


    public class ConditionalOrExp : BinaryExp {
        public ConditionalOrExp(Token start)
            : base(start, Op.BooleanOr)
        {

        }

        public override string ToString()
        {
            return ToString("conditional-or");
        }
    }


    public class ConditionalAndExp : BinaryExp {
        public ConditionalAndExp(Token start) 
            : base(start, Op.BooleanAnd)
        {

        }

        public override string ToString()
        {
            return ToString("conditional-and");
        }
    }


    public class InclusiveOrExp : BinaryExp {
        public InclusiveOrExp(Token start)
            : base(start, Op.InclusiveOr)
        {

        }
    }


    public class ExclusiveOrExp : BinaryExp {
        public ExclusiveOrExp(Token start)
            : base(start, Op.ExclusiveOr)
        {

        }
    }


    public class BitwiseAndExp : BinaryExp {
        public BitwiseAndExp(Token start)
            : base(start, Op.BitwiseAnd)
        {

        }
    }


    public class EqualityExp : BinaryExp {
        public EqualityExp(Token start, Op eqOp)
            : base(start, eqOp)
        {

        }
    }


    public class RelationalExp : BinaryExp {
        public RelationalExp(Token start, Op relOp)
            : base(start, relOp)
        {

        }
    }


    public class ShiftExp : BinaryExp {
        public ShiftExp(Token start, Op shOp)
            : base(start, shOp)
        {

        }
    }


    public class AdditiveExp : BinaryExp {
        public AdditiveExp(Token start, Op op) 
            : base(start, op)
        {

        }

        public override string ToString()
        {
            string name = "add";
            switch (this.Op) {
                case Parsing.Op.Plus:
                    name = "add";
                    break;
                case Parsing.Op.Minus:
                    name = "sub";
                    break;
                default:
                    name = "<unknown op>";
                    break;
            }

            return ToString(name);
        }
    }


    public class MultiplicativeExp : BinaryExp {
        public MultiplicativeExp(Token start, Op op)
            : base(start, op)
        {

        }

        public override string ToString()
        {
            string opName = "mult";
            switch (this.Op) {
                case Parsing.Op.Multiply:
                    opName = "mult";
                    break;
                case Parsing.Op.Divide:
                    opName = "div";
                    break;
                default:
                    opName = "<unknown op>";
                    break;
            }

            return ToString(opName);
        }
    }


    public class UnaryExp : Exp {
        public UnaryExp(Token start, Op op)
            : base(start)
        {
            this.Op = op;
        }

        public Op Op { get; private set; }
        public Exp Exp { get; set; }

        public override string ToString()
        {
            string name = "";
            switch (this.Op) {
                case Parsing.Op.Plus:
                    name = "+";
                    break;
                case Parsing.Op.Minus:
                    name = "-";
                    break;
                case Parsing.Op.Not:
                    name = "!";
                    break;
                case Parsing.Op.BitwiseNot:
                    name = "~";
                    break;
                case Parsing.Op.PlusPlus:
                    name = "++";
                    break;
                case Parsing.Op.MinusMinus:
                    name = "--";
                    break;
                default:
                    name = "<unknown unary op>";
                    break;
            }

            return string.Format("(unary {0} {1})", name, this.Exp.ToString());
        }
    }


    public class PreIncrExp : UnaryExp {
        public PreIncrExp(Token start)
            : base(start, Op.PlusPlus)
        {

        }
    }


    public class PreDecrExp : UnaryExp {
        public PreDecrExp(Token start)
            : base(start, Op.MinusMinus)
        {

        }
    }


    public class MemberAccessExp : Exp {
        public MemberAccessExp(Token start) 
            : base(start)
        {

        }

        public Exp Target { get; set; }
        public IdExp Member { get; set; }

        public override string ToString()
        {
            return string.Format("(member-access {0} {1})", Target, Member);
        }
    }


    public class InvocationExp : Exp {
        public InvocationExp(Token start)
            : base(start)
        {

        }

        public Exp Target { get; set; }
        public List<ArgumentExp> Args { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("(invocation {0} (", this.Target);
            sb.AppendItems(Args);

            sb.Append("))");
            return sb.ToString();
        }
    }


    public class ArgumentExp : Exp {
        public ArgumentExp(Token start)
            : base(start)
        {

        }

        public bool IsRef { get; set; }
        public bool IsOut { get; set; }
        public Exp Exp { get; set; }

        public override string ToString()
        {
            string modstr = "";
            if (IsRef)
                modstr = " ref ";
            else if (IsOut)
                modstr = " out ";

            return StringFormatting.SExp("arg", modstr, this.Exp);
            //return string.Format("(arg{0} {1}", modstr, this.Exp.ToString());
        }
    }


    public class CastExp : Exp {
        public CastExp(Token start)
            : base(start)
        {

        }

        public QualifiedIdExp TargetType { get; set; }
        public Exp Exp { get; set; }
    }


    public class PostIncrExp : Exp {
        public PostIncrExp(Token start) 
            : base(start)
        {

        }

        public Exp Exp { get; set; }

        public override string ToString()
        {
            return StringFormatting.SExp("post-incr", this.Exp);
        }
    }


    public class PostDecrExp : Exp {
        public PostDecrExp(Token start) 
            : base(start)
        {

        }

        public Exp Exp { get; set; }

        public override string ToString()
        {
            return StringFormatting.SExp("post-decr", this.Exp);
        }
    }


    public class ThisAccessExp : Exp {
        public ThisAccessExp(Token start) 
            : base(start)
        {

        }

        public override string ToString()
        {
            return StringFormatting.SExp("this-access");
        }
    }


    public abstract class BaseAccessExp : Exp {
        protected BaseAccessExp(Token start)
            : base(start)
        {
            
        }
    }


    public class BaseMemberAccessExp : BaseAccessExp {
        public BaseMemberAccessExp(Token start) 
            : base(start)
        {

        }

        public IdExp Member { get; set; }

        public override string ToString()
        {
            return StringFormatting.SExp("base-member-access", this.Member);
        }
    }


    public class BaseIndexerAccessExp : BaseAccessExp {
        public BaseIndexerAccessExp(Token start, Exp es)
            : base(start)
        {
            var elist = es as ExpList;
            if (null != elist)
                this.IndexerExps = elist;
            else this.IndexerExps = new ExpList(es.StartToken) { Exps = new List<Exp>() { es } };
        }

        /// <summary>
        /// An exp-list is either an exp or an exp-list.
        /// </summary>
        public ExpList IndexerExps { get; private set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("(base-indexer-access (");
            sb.AppendItems(this.IndexerExps.Exps);
            sb.Append("))");
            return sb.ToString();
        }
    }


    public abstract class AnonFunctionSig : Exp {
        public AnonFunctionSig(Token start) 
            : base(start)
        {

        }
    }


    public class ExplicitAnonFunctionSig : AnonFunctionSig {
        public ExplicitAnonFunctionSig(Token start)
            : base(start)
        {

        }
    }


    public class ImplicitAnonFunctionSig : AnonFunctionSig {
        public ImplicitAnonFunctionSig(Token start)
            : base(start)
        {

        }
    }


    /// <summary>
    /// type-name
    /// </summary>
    public abstract class TypeNameExp : Exp {
        protected TypeNameExp(Token start)
            : base(start)
        {

        }

        public abstract bool IsPredefinedType { get; }
    }


    /// <summary>
    /// Either a simple-type or one of object, dynamic, string
    /// </summary>
    public class PredefinedTypeNameExp : TypeNameExp {
        public PredefinedTypeNameExp(Token start)
            : base(start)
        {

        }

        public override bool IsPredefinedType { get { return true; } }
        public PredefinedType Type { get; set; }

        public override string ToString()
        {
            return StringFormatting.SExp("predefined-type", Type.ToString().ToLower());
        }
    }


    /// <summary>
    /// Any type-name that is not a predefined type
    /// </summary>
    public class UserTypeNameExp : TypeNameExp {
        #region Nested
        public class Part : Exp {
            private readonly List<TypeNameExp> typeArgs = new List<TypeNameExp>();

            public Part(Token start)
                : base(start)
            {
                
            }

            public IdExp AliasId { get; set; }
            public IdExp Id { get; set; }
            public List<TypeNameExp> TypeArgs { get { return typeArgs; } }
        }
        #endregion

        private readonly List<Part> parts = new List<Part>();
        
        public UserTypeNameExp(Token start)
            : base(start)
        {

        }

        public override bool IsPredefinedType { get { return false; } }
        public List<Part> Parts { get { return parts; } }
    }


    public class IdExp : Exp {
        public IdExp(Token spelling)
            : base(spelling)
        {
            this.Spelling = spelling;
        }

        public Token Spelling { get; set; }

        public override string ToString()
        {
            return string.Format("(id {0})", Spelling.Value);
        }
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

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("(qualified-id ");
            for (int i = 0; i < Parts.Count; i++) {
                sb.Append(Parts[i].Spelling.Value);
                if (i < Parts.Count - 1)
                    sb.Append(".");
            }

            sb.Append(")");
            return sb.ToString();
        }
    }


    public abstract class LiteralExp : Exp {
        protected LiteralExp(Token start)
            : base(start)
        {

        }
    }


    //TODO: Figure out the best way to represent numeric literals 
    //in the AST.
    //The lexer doesn't know any more about a numeric literal token 
    //than the fact that it's a valid numeric literal.  The actual type 
    //and characteristics should be figured out here.
    public class NumericLiteralExp : LiteralExp {
        public NumericLiteralExp(Token start)
            : base(start)
        {

        }

        public int NumValue
        {
            get
            {
                return int.Parse(this.StartToken.Value);
            }
        }

        public override string ToString()
        {
            return StartToken.Value;
        }
    }


    //TODO: What about longs, etc?
    public class IntLiteralExp : LiteralExp {
        public IntLiteralExp(Token start)
            : base(start)
        {

        }

        public int Value
        {
            get { return int.Parse(StartToken.Value); }
        }

        public override string ToString()
        {
            return string.Format("(int {0})", StartToken.Value);
        }
    }


    public class RealLiteralExp : LiteralExp {
        public RealLiteralExp(Token start)
            : base(start)
        {

        }

        //TODO: How to represent the value? 
        //Could be a float, double, etc.
    }


    public class StringLiteralExp : LiteralExp {
        public StringLiteralExp(Token start) 
            : base(start)
        {

        }

        public string Value { get { return StartToken.Value; } }

        public override string ToString()
        {
            return string.Format("(string \"{0}\")", Value);
        }
    }


    public class CharLiteralExp : LiteralExp {
        public CharLiteralExp(Token start) 
            : base(start)
        {

        }

        public char Value { get { return char.Parse(StartToken.Value); } }
    }


    public class BooleanLiteralExp : LiteralExp {
        public BooleanLiteralExp(Token start)
            : base(start)
        {

        }

        public bool Value
        {
            get
            {
                switch (StartToken.Sym) {
                    case Sym.KwTrue:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public override string ToString()
        {
            return string.Format("(bool {0})", Value.ToString().ToLower());
        }
    }


    public class NullLiteralExp : LiteralExp {
        public NullLiteralExp(Token start) 
            : base(start)
        {

        }

        public override string ToString()
        {
            return "null";
        }
    }


    public class AssignmentExp : Exp {
        public AssignmentExp(Token start, Op assignOp) 
            : base(start)
        {
            this.AssignOp = assignOp;
        }

        public Exp Lhs { get; set; }
        public Op AssignOp { get; private set; }
        public Exp Rhs { get; set; }

        public override string ToString()
        {
            return string.Format("(assign {0} {1} {2})",
                Lhs,
                AssignOp,
                Rhs);
        }
    }
}
