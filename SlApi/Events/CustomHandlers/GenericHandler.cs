using PluginAPI.Enums;

using System;

namespace SlApi.Events.CustomHandlers
{
    public class GenericHandler : EventHandlerDelegateBase
    {
        private Action<object[]> _cachedAction;
        private ServerEventType _type;

        public override string DelegateName
        {
            get
            {
                if (_cachedAction != null)
                {
                    return $"{_cachedAction.Method.DeclaringType.FullName}::{_cachedAction.Method.Name}";
                }
                else
                {
                    return $"Unknown - {_type}";
                }
            }
        }

        public GenericHandler(ServerEventType type, Action<object[]> action)
        {
            _type = type;
            _cachedAction = action;
        }

        public override ServerEventType Type { get => _type; }

        public override void Trigger(params object[] args)
        {
            _cachedAction?.Invoke(args);
        }
    }
}