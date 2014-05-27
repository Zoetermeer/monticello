using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Monticello.Common {
    public static class StringFormatting {
        public static StringBuilder AppendItems(this StringBuilder sb, IEnumerable items, string delim = " ")
        {
            var it = items.GetEnumerator();
            int i = 0;
            while (it.MoveNext()) {
                if (i > 0)
                    sb.Append(delim);

                sb.Append(it.Current);
                ++i;
            }

            return sb;
        }

        public static string SExp(string name, params object[] args)
        {
            var sb = new StringBuilder();
            sb.Append("(");
            sb.Append(name);
            if (args.Length > 0)
                sb.Append(" ");
            for (int i = 0; i < args.Length; i++) {
                //Skip if this arg is the empty string
                object o = args[i];
                if (null != o) {
                    var s = o as string;
                    if (null != s && string.IsNullOrEmpty(s))
                        continue;
                }

                sb.Append(args[i] ?? "<null>");
                if (i < args.Length - 1)
                    sb.Append(" ");
            }

            sb.Append(")");
            return sb.ToString();
        }
    }
}
