using System;

namespace Monticello.Parsing
{
    public interface ICanLookahead
    {
        void PushMark();
        void PopMark();
    }
}
