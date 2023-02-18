using PluginAPI.Enums;
using PluginAPI.Events;

using SlApi.Events.Handlers;
using SlApi.Features.Overwatch;
using System.Collections.Generic;
using System.Linq;

namespace SlApi.Events
{
    public static class EventHandlers
    {
        private static HashSet<EventHandlerDelegateBase> CustomHandlers = new HashSet<EventHandlerDelegateBase>();

        public static void RegisterBase()
        {
            EntryPoint.OnUnloading += OnUnloading;

            EventManager.RegisterEvents<RoundHandlers>(EntryPoint.Instance);
            EventManager.RegisterEvents<PlayerHandlers>(EntryPoint.Instance);

            PersistentOverwatch.Init();
        }

        public static void UnregisterBase()
        {
            EntryPoint.OnUnloading -= OnUnloading;

            EventManager.UnregisterEvents<RoundHandlers>(EntryPoint.Instance);
            EventManager.UnregisterEvents<PlayerHandlers>(EntryPoint.Instance);
        }

        public static bool RegisterEvent<T>(T handler) where T : EventHandlerDelegateBase
        {
            return CustomHandlers.Add(handler);
        }

        public static bool UnregisterEvent<T>(T handler) where T: EventHandlerDelegateBase
        { 
            return CustomHandlers.Remove(handler); 
        }

        public static void TriggerEvents(ServerEventType type, params object[] args)
        {
            for (int i = 0; i < CustomHandlers.Count; i++)
            {
                var element = CustomHandlers.ElementAt(i);

                if (element.Type == type)
                {
                    try
                    {
                        element.Trigger(args);
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Error($"EventHandlers: Failed to trigger event {element.Type} ({element.DelegateName}) ({ex.Message})");
                    }
                }
            }
        }

        private static void OnUnloading()
        {
            CustomHandlers.Clear();
        }
    }
}
