using HarmonyLib;
using MapGeneration.Distributors;

using PluginAPI.Enums;
using PluginAPI.Events;

using SlApi.Extensions;

namespace SlApi.Patches.Feature.RemoteCard
{
    [HarmonyPatch(typeof(Locker), nameof(Locker.ServerInteract))]
    public static class Locker_ServerInteract_Patch
    {
        public static bool Prefix(Locker __instance, ReferenceHub ply, byte colliderId)
        {
            if (colliderId >= __instance.Chambers.Length || !__instance.Chambers[colliderId].CanInteract)
                return false;

            var canOpen = 
                __instance.CheckPerms(__instance.Chambers[colliderId].RequiredPermissions, ply) 
                || ply.serverRoles.BypassMode
                || Features.RemoteKeycard.RemoteCard.CanOpenLocker(ply, __instance.Chambers[colliderId]);

            if (!EventManager.ExecuteEvent(ServerEventType.PlayerInteractLocker, ply, __instance, __instance.Chambers[colliderId], canOpen))
                return false;

            if (!canOpen)
            {
                __instance.RpcPlayDenied(colliderId);
                return false;
            }

            __instance.Chambers[colliderId].ToggleState(__instance);
            return false;
        }
    }
}
