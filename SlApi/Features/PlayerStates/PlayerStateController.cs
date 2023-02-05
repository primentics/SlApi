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

        private static void OnUpdate()
        {
            if (!EnableUpdate)
                return;

            for (int i = 0; i < ActiveStates.Count; i++)
            {
                if (ActiveStates[i].CanUpdateState() && ActiveStates[i].IsActive)
                    ActiveStates[i].UpdateState();
            }
        }
    }
}
