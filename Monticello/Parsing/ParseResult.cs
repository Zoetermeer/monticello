using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monticello.Parsing {
    public class ParseResult {
        private List<string> errors = new List<string>();
        
        private string FormatError(string msg, Token tok)
        {
            return string.Format("Line {0}, col {1}: {2}", tok.Line, tok.Col, msg);
        }

        private string FormatError(string msg, int line, int col)
        {
            return string.Format("Line {0}, col {1}: {2}", line, col, msg);
        }

        private string FormatError(string msg, TokenBuffer buf)
        {
            var tok = buf.LastOne();
            if (null == tok)
                return msg;

            return string.Format("Line {0}, col {1}: {2}", tok.Line, tok.Col, msg);
        }

        public void Error(string msg, Token tok)
        {
            errors.Add(FormatError(msg, tok));
        }

        public void Error(string msg, int line, int col)
        {
            errors.Add(FormatError(msg, line, col));
        }

        public void Error(string msg, TokenBuffer buf)
        {
            errors.Add(FormatError(msg, buf));
        }
    }
}
