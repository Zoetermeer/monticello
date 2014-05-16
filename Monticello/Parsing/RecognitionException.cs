using System;

namespace Monticello.Parsing
{
    public class RecognitionException : Exception
    {
        public RecognitionException(int line, int col)
            : base(string.Format("Unrecognized sequence at line {0}, column {1}", line, col))
        {

        }
    }
}
