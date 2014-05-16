using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monticello.Parsing
{
    public class TokenBuffer : ICanLookahead
    {
        private Lexer lexer;
        private List<Token> buf = new List<Token>();
        private int pos = 1;
        private Stack<int> marks = new Stack<int>();
        private bool atEof = false;

        public TokenBuffer(Lexer lexer)
        {
            this.lexer = lexer;
        }

        public int Size
        {
            get { return buf.Count; }
        }

        public void PushMark()
        {
            marks.Push(pos);
        }

        public void PopMark()
        {
            pos = marks.Pop();
        }

        public Token Next()
        {
            if (pos > buf.Count)
            {
                if (atEof)
                    return buf.Last();
                else
                {
                    var t = lexer.Read();
                    if (t.Sym == Sym.Eof)
                        atEof = true;

                    buf.Add(t);
                }

            }

            return buf[(pos++) - 1];
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("buf = { ");
            foreach (Token t in this.buf)
                sb.AppendFormat("{0} ", t.ToString());

            sb.Append("} ");
            sb.Append("marks = { ");
            foreach (var m in this.marks)
                sb.AppendFormat("{0} ", m);
            sb.Append("} ");
            sb.AppendFormat("pos = {0}", this.pos);
            return sb.ToString();
        }
    }
}
