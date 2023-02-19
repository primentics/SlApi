using AngleSharp.Io;
using Hints;

using Interactables.Interobjects.DoorUtils;

using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Keycards;

using MapGeneration.Distributors;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;

using PlayerStatsSystem;

using RelativePositioning;
using SlApi.Dummies;
using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.ResizeStates;

using System;
using System.Linq;

using UnityEngine;

namespace SlApi.Extensions
{
    public static class HubExtensions
    {
        public static AhpStat.AhpProcess[] GetActiveAhpProcesses(this ReferenceHub hub)
        {
            if (!hub.playerStats.TryGetModule<AhpStat>(out var module))
            {
                return Array.Empty<AhpStat.AhpProcess>();
            }

            return module._activeProcesses.ToArray();
        }

        public static void RemoveItem(this ReferenceHub hub, ItemBase item)
            => hub.inventory.ServerRemoveItem(item.ItemSerial, item.PickupDropModel);

        public static void RemoveItems(this ReferenceHub hub)
        {
            for (int i = 0; i < hub.inventory.UserInventory.Items.Count; i++)
                hub.RemoveItem(hub.inventory.UserInventory.Items.ElementAt(i).Value);

            hub.inventory.CurInstance = null;
            hub.inventory.UserInventory.Items.Clear();
            hub.inventory.UserInventory.ReserveAmmo.Clear();
            hub.inventory.SendItemsNextFrame = true;
            hub.inventory.SendAmmoNextFrame = true;
        }

        public static ItemBase[] GetItems(this ReferenceHub hub)
        {
            if (!hub.inventory.UserInventory.Items.Any())
                return Array.Empty<ItemBase>();

            return hub.inventory.UserInventory.Items.Values.ToArray();
        }

        public static ushort GetAmmo(this ReferenceHub hub, ItemType item)
        {
            if (hub.inventory.UserInventory.ReserveAmmo.TryGetValue(item, out var ammo))
                return ammo;

            return 0;
        }

        public static void SetAmmo(this ReferenceHub hub, ItemType ammo, ushort amount)
        {
            hub.inventory.UserInventory.ReserveAmmo[ammo] = amount;
            hub.inventory.SendAmmoNextFrame = true;
        }

        public static bool IsTagHidden(this ReferenceHub hub)
            => !string.IsNullOrEmpty(hub.serverRoles.HiddenBadge);

        public static void ShowTag(this ReferenceHub hub)
        {
            hub.serverRoles.HiddenBadge = null;
            hub.serverRoles.GlobalHidden = false;
            hub.serverRoles.RpcResetFixed();
            hub.serverRoles.RefreshPermissions(true);
        }

        public static void HideTag(this ReferenceHub hub)
        {
            if (!hub.serverRoles.BypassStaff)
            {
                if (!string.IsNullOrEmpty(hub.serverRoles.HiddenBadge))
                    return;

                if (string.IsNullOrEmpty(hub.serverRoles.MyText))
                    return;
            }

            hub.serverRoles.GlobalHidden = hub.serverRoles.GlobalSet;
            hub.serverRoles.HiddenBadge = hub.serverRoles.MyText;
            hub.serverRoles.NetworkGlobalBadge = null;
            hub.serverRoles.SetText(null);
            hub.serverRoles.SetColor(null);
            hub.serverRoles.RefreshHiddenTag();
        }

        public static bool TryGetRoleKey(this ReferenceHub hub, out string roleKey)
        {
            if (ServerStatic.PermissionsHandler is null)
            {
                roleKey = null;
                return false;
            }

            if (ServerStatic.PermissionsHandler._members.TryGetValue(hub.characterClassManager.UserId, out roleKey))
            {
                return true; 
            }
            else if (!string.IsNullOrWhiteSpace(hub.characterClassManager.UserId2))
            {
                if (ServerStatic.PermissionsHandler._members.TryGetValue(hub.characterClassManager.UserId2, out roleKey))
                {
                    return true;
                }
            }

            roleKey = null;
            return false;
        }

        public static bool CanOpenLocker(this ReferenceHub hub, LockerChamber locker)
        {
            if (hub.playerEffectsController.GetEffect<CustomPlayerEffects.AmnesiaItems>().IsEnabled)
                return false;

            foreach (var item in hub.inventory.UserInventory.Items.Values)
            {
                if (!(item is KeycardItem keycardItem))
                    continue;

                if (keycardItem.Permissions.HasFlagFast(locker.RequiredPermissions))
                    return true;
            }

            return false;
        }

        public static bool CanOpenGenerator(this ReferenceHub hub, Scp079Generator generator)
        {
            if (hub.playerEffectsController.GetEffect<CustomPlayerEffects.AmnesiaItems>().IsEnabled)
                return false;

            foreach (var item in hub.inventory.UserInventory.Items.Values)
            {
                if (!(item is KeycardItem keycardItem))
                    continue;

                if (keycardItem.Permissions.HasFlagFast(generator._requiredPermission))
                    return true;
            }

            return false;
        }

        public static bool HasPermissions(this ReferenceHub hub, KeycardPermissions permissions, bool requiresAllPermissions = false)
        {
            if (hub.playerEffectsController.TryGetEffect<CustomPlayerEffects.AmnesiaItems>(out var effect) && effect.IsEnabled)
                return false;

            if (hub.inventory.CurInstance != null && hub.inventory.CurInstance is KeycardItem keycard)
                return requiresAllPermissions ? keycard.Permissions.HasFlag(permissions) : (keycard.Permissions & permissions) != 0;

            foreach (var item in hub.inventory.UserInventory.Items.Values)
            {
                if (item is KeycardItem card)
                {
                    if ((requiresAllPermissions ? card.Permissions.HasFlag(permissions) : (card.Permissions & permissions) != 0))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool HasItems(this ReferenceHub hub)
            => hub.inventory.UserInventory.Items.Any();

        public static void SetPosition(this ReferenceHub hub, Vector3 position)
        {
            hub.TryOverridePosition(position, Vector3.zero);
        }

        public static void SetRotation(this ReferenceHub hub, Quaternion rotation)
        {
            if (hub.roleManager.CurrentRole is IFpcRole fpcRole && fpcRole.FpcModule != null && fpcRole.FpcModule.MouseLook != null)
            {
                var module = fpcRole.FpcModule.MouseLook;

                module.CurrentHorizontal = WaypointBase.GetWorldRotation(module._fpmm.Motor.ReceivedPosition.WaypointId, rotation).eulerAngles.y;
                module.CurrentVertical = module._syncVertical;
            }

            hub.transform.rotation = rotation;
            hub.PlayerCameraReference.rotation = rotation;
        }

        public static Vector3 GetRealPosition(this ReferenceHub hub)
        {
            if (hub.roleManager.CurrentRole is IFpcRole fpcRole && fpcRole.FpcModule != null)
                return fpcRole.FpcModule.Position;

            return hub.PlayerCameraReference.position;
        }

        public static Quaternion GetRealRotation(this ReferenceHub hub)
        {
            if (hub.roleManager.CurrentRole is IFpcRole fpcRole && fpcRole.FpcModule != null && fpcRole.FpcModule.MouseLook != null)
                return fpcRole.FpcModule.MouseLook.TargetCamRotation;

            return hub.PlayerCameraReference.rotation;
        }

        public static void Scale(this ReferenceHub hub, float scale)
            => Resize(hub, hub.transform.localScale * scale);

        public static void Resize(this ReferenceHub hub, Vector3 size)
        {
            if (!hub.TryGetState<ResizeState>(out var state))
                hub.TryAddState((state = new ResizeState(hub)));

            state.SetScale(size);
        }

        public static void UseDisruptor(this ReferenceHub hub)
        {
            if (DummyPlayer.IsDummy(hub))
                return;

            if (!hub.IsAlive())
                return;

            hub.characterClassManager.GodMode = false;
            hub.playerStats.KillPlayer(new DisruptorDamageHandler(new Footprinting.Footprint(hub), float.MaxValue));
        }

        public static void ConsoleMessage(this ReferenceHub hub, object message, string color = "green")
        {
            if (DummyPlayer.IsDummy(hub))
                return;

            hub.characterClassManager.ConsolePrint(message.ToString(), color);
        }

        public static void PersonalHint(this ReferenceHub hub, object message, float time)
        {
            if (DummyPlayer.IsDummy(hub))
                return;

            HintParameter[] parameters =
            {
                new StringHintParameter(message.ToString())
            };

            hub.networkIdentity.connectionToClient.Send(new HintMessage(new TextHint(message.ToString(), parameters, durationScalar: time)));
        }

        public static void PersonalBroadcast(this ReferenceHub hub, object message, ushort time)
        {
            if (DummyPlayer.IsDummy(hub))
                return;

            Broadcast.Singleton?.TargetClearElements(hub.connectionToClient);
            Broadcast.Singleton?.TargetAddElement(hub.connectionToClient, message.ToString(), time, Broadcast.BroadcastFlags.Normal);
        }

        public static bool TryGetHub(string value, out ReferenceHub hub)
        {
            hub = GetHub(value);
            return hub != null;
        }

        public static ReferenceHub GetHub(string value)
        {
            if (value.StartsWith("dummy:") && byte.TryParse(value.Replace("dummy:", ""), out var id) && DummyPlayer.TryGetDummy(id, out var dummy) && dummy._hub != null)
                return dummy._hub;
            else if (int.TryParse(value, out var pId))
                return ReferenceHub.GetHub(pId);
            else
                return ReferenceHub.AllHubs.FirstOrDefault(x => x.nicknameSync.MyNick.ToLower().Contains(value.ToLower()) || x.characterClassManager.UserId.Contains(value));
        }
    }
}