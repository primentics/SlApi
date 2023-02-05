using GameCore;

using MEC;

using PluginAPI.Core;

using SlApi.Features;

using System.Collections.Generic;
using System.Linq;

namespace SlApi.CustomEvents
{
    public static class CustomEventManager
    {
        private static CustomEventBase _nextEvent;
        private static CoroutineHandle _eventTickHandle;

        public static HashSet<CustomEventBase> RegisteredEvents { get; } = new HashSet<CustomEventBase>();

        public static CustomEventBase CurrentEvent { get; private set; }

        static CustomEventManager()
        {

        }

        public static void OnRoundRestarted()
        {
            if (CurrentEvent != null)
            {
                CurrentEvent = null;
            }

            if (_nextEvent != null)
            {
                ServerHelper.DoGlobalBroadcast($"<b><color=#16EEBA>Připravuje se event:</color> <color=#ff0000>{_nextEvent.Name}</color></b>", 5);

                _nextEvent.PrepareEvent();

                CurrentEvent = _nextEvent;

                _eventTickHandle = Timing.RunCoroutine(EventTicker());
                _nextEvent.StartEvent();
            }
        }

        public static void EndCurrentEvent()
        {
            if (CurrentEvent != null)
            {
                Timing.KillCoroutines(_eventTickHandle);

                CurrentEvent.EndEvent();

                _nextEvent = null;
                CurrentEvent = null;
            }
        }

        public static void RegisterEvent(CustomEventBase customEventBase)
        {
            RegisteredEvents.Add(customEventBase);
        }

        public static void LaunchEvent(CustomEventBase customEventBase)
        {
            if (customEventBase.RequiresRoundRestart && RoundStart.RoundStarted)
            {
                ServerHelper.DoGlobalBroadcast($"<b><color=#16EEBA>Restartuje se kolo kvůli eventu:</color> <color=#ff0000>{customEventBase.Name}</color></b>", 15);

                Timing.CallDelayed(3f, () =>
                {
                    Round.Restart(false, true, ServerStatic.NextRoundAction.DoNothing);
                });

                return;
            }

            CurrentEvent = customEventBase;

            ServerHelper.DoGlobalBroadcast($"<b><color=#16EEBA>Připravuje se event:</color> <color=#ff0000>{_nextEvent.Name}</color></b>", 5);

            CurrentEvent.PrepareEvent();

            _eventTickHandle = Timing.RunCoroutine(EventTicker());

            CurrentEvent.StartEvent();
        }

        public static bool TryGetEvent(string name, out CustomEventBase customEvent)
        {
            customEvent = RegisteredEvents.FirstOrDefault(x => x.Name == name);

            return customEvent != null;
        }

        private static IEnumerator<float> EventTicker()
        {
            while (true)
            {
                yield return Timing.WaitForOneFrame;

                if (CurrentEvent.CheckEndCondition())
                {
                    EndCurrentEvent();
                    yield break;
                }

                CurrentEvent.TickEvent();
            }
        }
    }
}