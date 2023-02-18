using HarmonyLib;

using NorthwoodLib.Pools;

using PlayerRoles;

using PluginAPI.Core;

using SlApi.Configs;
using SlApi.Extensions;

using System.Linq;

using UnityEngine;

namespace SlApi.Features.CustomTesla
{
    [HarmonyPatch(typeof(TeslaGateController), nameof(TeslaGateController.FixedUpdate))]
    public static class CustomTeslaController
    {
        [Config("CustomTesla.DisabledForRoles", "A list of roles the tesla gate's won't trigger for.")]
        public static RoleTypeId[] DisabledForRoles = new RoleTypeId[]
        {
            RoleTypeId.Tutorial,
            RoleTypeId.FacilityGuard,
            RoleTypeId.NtfSpecialist,
            RoleTypeId.NtfCaptain,
            RoleTypeId.NtfPrivate,
            RoleTypeId.NtfSergeant,
            RoleTypeId.Scientist
        };

        [Config("CustomTesla.DisabledForUsers", "A list of roles/IDs of users that have disabled teslas.")]
        public static string[] DisabledForUsers = new string[]
        {
            "none"
        };

        public static bool Prefix(TeslaGateController __instance)
        {
            for (int i = 0; i < __instance.TeslaGates.Count; i++)
            {
                var tesla = __instance.TeslaGates[i];

                if (tesla != null && tesla.gameObject != null && tesla.netIdentity != null)
                {
                    if (tesla.isActiveAndEnabled)
                    {
                        if (tesla.InactiveTime > 0f)
                        {
                            tesla.NetworkInactiveTime = Mathf.Max(0f, tesla.InactiveTime - Time.fixedDeltaTime);
                        }
                        else
                        {
                            bool anyIdle = false;
                            bool anyRange = false;

                            foreach (var hub in ReferenceHub.AllHubs)
                            {
                                if (hub.Mode != ClientInstanceMode.ReadyClient)
                                    continue;

                                if (!hub.IsAlive())
                                    continue;

                                if (DisabledForRoles.Contains(hub.roleManager.CurrentRole.RoleTypeId))
                                    continue;

                                if (DisabledForUsers.Contains(hub.characterClassManager.UserId))
                                    continue;

                                if (hub.TryGetRoleKey(out var role) && DisabledForUsers.Contains(role))
                                    continue;

                                if (!anyIdle)
                                    anyIdle = tesla.PlayerInIdleRange(hub);

                                if (!anyRange && !tesla.InProgress && tesla.PlayerInRange(hub))
                                    anyRange = true;
                            }

                            if (anyRange)
                                tesla.ServerSideCode();

                            if (anyIdle != tesla.isIdling)
                                tesla.ServerSideIdle(anyIdle);
                        }
                    }
                }
            }

            return false;
        }
    }
}