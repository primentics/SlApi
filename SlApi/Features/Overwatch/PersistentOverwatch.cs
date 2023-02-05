using SlApi.Events;
using SlApi.Events.CustomHandlers;

using System.Collections.Generic;

namespace SlApi.Features.Overwatch
{
    public static class PersistentOverwatch
    {
        static PersistentOverwatch()
        {
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.PlayerJoined, OnPlayerJoined));
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.PlayerChangeRole, OnRoleChanged));
        }

        public static HashSet<string> TrackedPlayers { get; set; } = new HashSet<string>();

        private static void OnPlayerJoined(object[] args)
        {

        }

        private static void OnRoleChanged(object[] args)
        {

        }
    }
}
