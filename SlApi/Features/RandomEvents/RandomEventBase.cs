using System;

namespace SlApi.Features.RandomEvents
{
    public class RandomEventBase
    {
        public virtual string Id { get; }

        public virtual int Chance { get; }

        public DateTime LastTime { get; set; }

        public virtual void DoEvent() { }
        public virtual bool IsFinished() { return false; }
        public virtual bool CanDoEvent() { return false; }
        public virtual bool CheckSpawnInterval() { return false; }
        public virtual bool CheckRoundState() { return false; }
    }
}
