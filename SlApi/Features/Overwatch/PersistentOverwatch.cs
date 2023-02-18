using PlayerRoles;
using PlayerRoles.Spectating;

using PluginAPI.Core;

using SlApi.Events;
using SlApi.Events.CustomHandlers;
using SlApi.Extensions;

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

        internal static void Init() { }

        public static HashSet<string> TrackedPlayers { get; set; } = new HashSet<string>();

        private static void OnPlayerJoined(object[] args)
        {
            var hub = (args[0] as Player).ReferenceHub;

            if (TrackedPlayers.Contains(hub.characterClassManager.UserId))
            {
                hub.roleManager.ServerSetRole(RoleTypeId.Overwatch, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.All);
                hub.PersonalHint($"Byla ti udělena role <color=#19D0E9>Overwatch</color>.", 10f);
            }
        }

        private static void OnRoleChanged(object[] args)
        {
            var hub = (args[0] as Player).ReferenceHub;
            var oldRole = args[1] as PlayerRoleBase;
            var newRole = (RoleTypeId)args[2];
            var reason = (RoleChangeReason)args[3];

            if (TrackedPlayers.Contains(hub.characterClassManager.UserId) && oldRole is OverwatchRole && newRole != RoleTypeId.Overwatch)
            {
                hub.PersonalHint($"<color=#19D0E9>Overwatch</color> ti již <color=#FF0000>nebude</color> zůstávat.", 10f);
                
                TrackedPlayers.Remove(hub.characterClassManager.UserId);
            }
            else if (newRole is RoleTypeId.Overwatch && !TrackedPlayers.Contains(hub.characterClassManager.UserId))
            {
                hub.PersonalHint($"<color=#19D0E9>Overwatch</color> ti nyní <color=#FF0000>bude</color> zůstávat.", 10f);
                TrackedPlayers.Add(hub.characterClassManager.UserId);
            }
        }
    }
}
