using System;

namespace Monticello.Parsing
{
    public class RecognitionException : Exception
    {
        public RecognitionException(int line, int col, char c)
            : base(string.Format("Unrecognized sequence '{0}' at line {1}, column {2}", c, line, col))
        {

        }
    }
}
