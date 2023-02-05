namespace SlApi.CustomEvents
{
    public class CustomEventBase
    {
        public virtual bool RequiresRoundRestart { get; }

        public virtual string Name { get; }

        public virtual void PrepareEvent()
        {

        }

        public virtual void StartEvent()
        {

        }

        public virtual void TickEvent()
        {

        }

        public virtual void EndEvent()
        {

        }

        public virtual bool CheckEndCondition()
        {
            return false;
        }
    }
}
