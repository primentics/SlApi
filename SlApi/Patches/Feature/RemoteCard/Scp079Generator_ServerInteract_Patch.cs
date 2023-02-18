using Footprinting;
using HarmonyLib;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Keycards;

using MapGeneration.Distributors;

using PlayerRoles;

using PluginAPI.Enums;
using PluginAPI.Events;

using SlApi.Extensions;

namespace SlApi.Patches.Feature.RemoteCard
{
    [HarmonyPatch(typeof(Scp079Generator), nameof(Scp079Generator.ServerInteract))]
    public static class Scp079Generator_ServerInteract_Patch
    {
        public static bool Prefix(Scp079Generator __instance, ReferenceHub ply, byte colliderId)
        {
            if (__instance._cooldownStopwatch.IsRunning 
                && __instance._cooldownStopwatch.Elapsed.TotalSeconds < __instance._targetCooldown)
                return false;

            if (colliderId != 0 && !__instance.HasFlag(__instance._flags, Scp079Generator.GeneratorFlags.Open))
                return false;

            __instance._cooldownStopwatch.Stop();

            if (!EventManager.ExecuteEvent(ServerEventType.PlayerInteractGenerator, ply, __instance, (Scp079Generator.GeneratorColliderId)colliderId))
            {
                __instance._cooldownStopwatch.Restart();
                return false;
            }

            switch (colliderId)
            {
                case 0:
                    {
                        if (__instance.IsUnlocked())
                        {
                            if (__instance.IsOpen())
                            {
                                if (!EventManager.ExecuteEvent(ServerEventType.PlayerCloseGenerator, ply, __instance))
                                    break;
                            }
                            else if (!EventManager.ExecuteEvent(ServerEventType.PlayerOpenGenerator, ply, __instance))
                                break;

                            if (__instance.IsOpen())
                                __instance.Close();
                            else
                                __instance.Open();

                            __instance._targetCooldown = __instance._doorToggleCooldownTime;
                        }
                        else if (ply.serverRoles.BypassMode
                            || (ply.inventory.CurInstance != null 
                                && ply.inventory.CurInstance is KeycardItem keycardItem 
                                && keycardItem.Permissions.HasFlagFast(__instance._requiredPermission))
                            || Features.RemoteKeycard.RemoteCard.CanOpenGenerator(ply, __instance))
                        {
                            __instance.Unlock();
                            __instance.ServerGrantTicketsConditionally(new Footprint(ply), 0.5f);
                        }
                        else
                        {
                            __instance._targetCooldown = __instance._unlockCooldownTime;
                            __instance.RpcDenied();
                        }

                        break;
                    }
                case 1:
                    if ((ply.IsHuman() || __instance.Activating) && !__instance.Engaged)
                    {
                        if (!!__instance.Activating)
                        {
                            if (!EventManager.ExecuteEvent(ServerEventType.PlayerActivateGenerator, ply, __instance))
                                break;
                        }
                        else if (!EventManager.ExecuteEvent(ServerEventType.PlayerDeactivatedGenerator, ply, __instance))
                            break;

                        __instance.Activating = !__instance.Activating;

                        if (__instance.Activating)
                        {
                            __instance._leverStopwatch.Restart();
                            __instance._lastActivator = new Footprint(ply);
                        }
                        else
                        {
                            __instance._lastActivator = default;
                        }

                        __instance._targetCooldown = __instance._doorToggleCooldownTime;
                    }

                    break;
                case 2:
                    if (__instance.Activating 
                        && !__instance.Engaged
                        && EventManager.ExecuteEvent(ServerEventType.PlayerDeactivatedGenerator, ply, __instance))
                    {
                        __instance.ServerSetFlag(Scp079Generator.GeneratorFlags.Activating, false);
                        __instance._targetCooldown = __instance._unlockCooldownTime;
                        __instance._lastActivator = default;
                    }

                    break;
                default:
                    __instance._targetCooldown = 1f;
                    break;
            }

            __instance._cooldownStopwatch.Restart();
            return false;
        }
    }
}
