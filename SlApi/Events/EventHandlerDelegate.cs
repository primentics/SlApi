using PluginAPI.Enums;

namespace SlApi.Events
{
    public class EventHandlerDelegateBase
    {
        public virtual ServerEventType Type { get; }

        public virtual void Trigger(params object[] args) { }
    }
}