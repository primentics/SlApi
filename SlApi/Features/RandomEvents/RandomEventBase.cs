using System;

namespace SlApi.RandomEvents
{
    public class RandomEventBase
    {
        public virtual string Id { get; }

        public DateTime LastTime { get; set; }

        public virtual void DoEvent() { }

        public virtual bool CanDoEvent() { return false; }
    }
}
