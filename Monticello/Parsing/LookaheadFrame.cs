using System;

namespace Monticello.Parsing
{
    public class LookaheadFrame : IDisposable
    {
        private ICanLookahead buf;
        private bool committed = false;

        public LookaheadFrame(ICanLookahead buf)
        {
            this.buf = buf;
            buf.PushMark();
        }

        public void Commit()
        {
            committed = true;
            buf.Commit();
        }

        public void Dispose()
        {
            if (!committed)
                buf.PopMark();
        }
    }
}
