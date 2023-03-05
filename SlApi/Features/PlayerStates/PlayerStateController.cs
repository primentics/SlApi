using AzyWorks.System;
using AzyWorks.Utilities;

using PluginAPI.Core;

using SlApi.Events;
using SlApi.Events.CustomHandlers;
using SlApi.Extensions;

using System.Collections.Generic;
using System.Linq;

namespace SlApi.Features.PlayerStates
{
    public static class PlayerStateController
    {
        private static List<PlayerStateBase> ActiveStates = new List<PlayerStateBase>();

        public static bool EnableUpdate { get; set; } = true;

        static PlayerStateController()
        {
            StaticUnityMethods.OnUpdate += OnUpdate;

            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.PlayerSpawn, OnRoleChanged));
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.PlayerLeft, OnPlayerLeft));
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.PlayerDying, OnDied));
        }

        public static PlayerStateBase[] GetPlayerStates(this ReferenceHub hub)
        {
            return ActiveStates.Where(x => x.Target == hub).ToArray();
        }

        public static T[] GetPlayerStates<T>(this ReferenceHub hub) where T : PlayerStateBase
        {
            return GetPlayerStates(hub).Select(x => (T)x).ToArray();
        }

        public static bool ToggleState<T>(this ReferenceHub hub) where T : PlayerStateBase
        {
            if (TryGetState<T>(hub, out var state))
            {
                state.IsActive = !state.IsActive;
                return true;
            }

            return false;
        }

        public static bool SetActive<T>(this ReferenceHub hub, bool state) where T : PlayerStateBase
        {
            if (TryGetState<T>(hub, out var playerState))
            {
                playerState.IsActive = state;
                return true;
            }

            return false;
        }

        public static bool TryGetState<T>(this ReferenceHub hub, out T state) where T : PlayerStateBase
        {
            var result = ActiveStates.FirstOrDefault(x => x.Target == hub && x is T);

            if (result is null)
            {
                state = default;
                return false;
            }

            state = (T)result;
            return true;
        }

        public static bool TryAddState<T>(this ReferenceHub hub, T state) where T : PlayerStateBase
        {
            if (!TryGetState<T>(hub, out _))
            {
                ActiveStates.Add(state);
                state.OnAdded();
                hub.ConsoleMessage($"[PlayerStateController] Assigned state {typeof(T).FullName}");
                SetActive<T>(hub, true);
                return true;
            }

            return false;
        }

        public static bool TryRemoveState<T>(this ReferenceHub hub) where T : PlayerStateBase
        {
            var index = ActiveStates.FindIndex(x => x is T && x.Target == hub);

            if (index != -1)
            {
                ActiveStates[index].DisposeState();
                ActiveStates.RemoveAt(index);
                hub.ConsoleMessage($"[PlayerStateController] Removed state ({typeof(T).FullName})", "red");
                return true;
            }

            return false;
        }

        private static void OnPlayerLeft(object[] args)
        {
            var hub = args[0].As<Player>().ReferenceHub;

            lock (ActiveStates)
            {
                for (int i = 0; i < ActiveStates.Count; i++)
                {
                    var item = ActiveStates.ElementAt(i);

                    if (item.Target == hub)
                        ActiveStates.RemoveAt(i);
                }
            }
        }

        private static void OnRoleChanged(object[] args)
        {
            var hub = args[0].As<Player>().ReferenceHub;

            lock (ActiveStates)
            {
                for (int i = 0; i < ActiveStates.Count; i++)
                {
                    var state = ActiveStates.ElementAt(i);

                    if (state.Target == hub)
                    {
                        state.OnRoleChanged();

                        if (state.ShouldClearOnRoleChange())
                        {
                            state.DisposeState();

                            ActiveStates.RemoveAt(i);
                        }
                    }
                }
            }
        }

        private static void OnDied(object[] args)
        {
            var hub = args[0].As<Player>().ReferenceHub;

            lock (ActiveStates)
            {
                for (int i = 0; i < ActiveStates.Count; i++)
                {
                    var state = ActiveStates.ElementAt(i);

                    if (state.Target == hub)
                    {
                        state.OnDied();

                        if (state.ShouldClearOnDeath())
                        {
                            state.DisposeState();

                            ActiveStates.Remove(state);
                        }
                    }
                }
            }
        }

        private static void OnUpdate()
        {
            if (!EnableUpdate)
                return;

            lock (ActiveStates)
            {
                foreach (var state in ActiveStates)
                {
                    if (state.CanUpdateState() && state.IsActive)
                        state.UpdateState();
                }
            }
        }
    }
}
