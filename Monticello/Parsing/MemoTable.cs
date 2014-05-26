using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monticello.Parsing {
    public class MemoTable {
        private Dictionary<Tuple<string, int>, MemoEntry> memoTable = new Dictionary<Tuple<string, int>, MemoEntry>();

        public void Add(Tuple<string, int> key, MemoEntry m)
        {
            memoTable.Add(key, m);
        }

        public bool TryGetValue(Tuple<string, int> key, out MemoEntry m)
        {
            return memoTable.TryGetValue(key, out m);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var kv in memoTable) {
                sb.AppendFormat("({0}, {1}) --> {2}\n", kv.Key.Item1, kv.Key.Item2, kv.Value);
            }

            return sb.ToString();
        }
    }


    public class MemoEntry {
        public MemoEntry(int pos)
        {
            Pos = pos;
            HasResult = false;
        }

        public MemoEntry(int pos, AstNode node)
        {
            Pos = pos;
            Ast = node;
            HasResult = null != node;
        }

        public bool IsFail { get { return !HasResult; } }
        public bool HasResult { get; set; }
        public AstNode Ast { get; set; }
        public int Pos { get; set; }

        [Pure]
        public override string ToString()
        {
            string v = "<no result>";
            if (HasResult && null != Ast) {
                v = Ast.ToString();
            }

            return string.Format("({0}, {1})", Pos, v);
        }
    }
}
