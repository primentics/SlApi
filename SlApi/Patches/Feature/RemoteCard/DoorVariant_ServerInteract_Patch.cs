using HarmonyLib;

using Interactables.Interobjects.DoorUtils;

using PlayerRoles;

using PluginAPI.Enums;
using PluginAPI.Events;

using SlApi.Extensions;

namespace SlApi.Patches.Feature.RemoteCard
{
    [HarmonyPatch(typeof(DoorVariant), nameof(DoorVariant.ServerInteract))]
    public static class DoorVariant_ServerInteract_Patch
    {
        public static bool Prefix(DoorVariant __instance, ReferenceHub ply, byte colliderId)
        {
            if (__instance.ActiveLocks > 0 && !ply.serverRoles.BypassMode)
            {
                DoorLockMode mode = DoorLockUtils.GetMode((DoorLockReason)__instance.ActiveLocks);

                if ((!mode.HasFlagFast(DoorLockMode.CanClose) 
                    || !mode.HasFlagFast(DoorLockMode.CanOpen)) && (!mode.HasFlagFast(DoorLockMode.ScpOverride) 
                    || !ply.IsSCP(true)) && (mode == DoorLockMode.FullLock 
                    || (__instance.TargetState && !mode.HasFlagFast(DoorLockMode.CanClose)) 
                    || (!__instance.TargetState && !mode.HasFlagFast(DoorLockMode.CanOpen))))
                {
                    __instance.LockBypassDenied(ply, colliderId);
                    return false;
                }
            }

            if (!__instance.AllowInteracting(ply, colliderId))
                return false;

            if (__instance.RequiredPermissions.RequiredPermissions is KeycardPermissions.None 
                || ply.serverRoles.BypassMode
                || (ply.IsSCP() && __instance.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.ScpOverride))
                || __instance.RequiredPermissions.CheckPermissions(ply.inventory.CurInstance, ply)
                || ply.GetRoleId() == RoleTypeId.Scp079
                || Features.RemoteKeycard.RemoteCard.CanOpenDoor(ply, __instance))
            {
                if (!EventManager.ExecuteEvent(ServerEventType.PlayerInteractDoor, ply, __instance, true))
                {
                    __instance.PermissionsDenied(ply, colliderId);
                    DoorEvents.TriggerAction(__instance, DoorAction.AccessDenied, ply);
                    return false;
                }

                __instance.ToggleState();
                __instance._triggerPlayer = ply;
                return false;
            }  

            __instance.PermissionsDenied(ply, colliderId);
            DoorEvents.TriggerAction(__instance, DoorAction.AccessDenied, ply);
            return false;
        }
    }
}
