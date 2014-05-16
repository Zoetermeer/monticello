using System;

namespace Monticello.Parsing
{
    public class LookaheadFrame : IDisposable
    {
        private ICanLookahead buf;

        public LookaheadFrame(ICanLookahead buf)
        {
            this.buf = buf;
            buf.PushMark();
        }

        public void Dispose()
        {
            buf.PopMark();
        }
    }
}
