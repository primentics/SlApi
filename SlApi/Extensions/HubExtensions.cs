using Hints;

using Interactables.Interobjects.DoorUtils;

using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Keycards;

using MapGeneration;
using MapGeneration.Distributors;
using Mirror;

using NorthwoodLib.Pools;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerStatsSystem;

using RelativePositioning;
using Respawning;

using SlApi.Dummies;
using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.ResizeStates;

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SlApi.Extensions
{
    public static class HubExtensions
    {
        public static NetworkIdentity HostIdentity { get => ReferenceHub.HostHub?.networkIdentity ?? null; }

        public static string UserId(this ReferenceHub hub)
            => hub.characterClassManager.UserId;

        public static string Nick(this ReferenceHub hub, string newValue = null) {
            if (!string.IsNullOrWhiteSpace(newValue)) {
                hub.nicknameSync.SetNick(newValue);
                return hub.nicknameSync.Network_myNickSync;
            }

            return hub.nicknameSync.Network_myNickSync;
        }

        public static bool GodMode(this ReferenceHub hub, bool? newValue = null) {
            if (newValue.HasValue) {
                hub.characterClassManager.GodMode = newValue.Value;
                return newValue.Value;
            }

            return hub.characterClassManager.GodMode;
        }

        public static HashSet<ReferenceHub> Where(this IEnumerable<ReferenceHub> hubs, Func<ReferenceHub, bool> condition) {
            var set = new HashSet<ReferenceHub>();

            foreach (var hub in hubs) {
                if (hub is null)
                    continue;

                if (!condition(hub))
                    continue;

                set.Add(hub);
            }

            return set;
        }

        public static HashSet<ReferenceHub> WherePlayers(this IEnumerable<ReferenceHub> hubs, Func<ReferenceHub, bool> condition) {
            var set = new HashSet<ReferenceHub>();

            foreach (var hub in hubs) {
                if (hub is null)
                    continue;

                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (hub.connectionToClient is null)
                    continue;

                if (DummyPlayer.IsDummy(hub))
                    continue;

                if (!condition(hub))
                    continue;

                set.Add(hub);
            }

            return set;
        }

        public static HashSet<ReferenceHub> WherePlayers(this IEnumerable<ReferenceHub> hubs, RoleTypeId role)
            => hubs.WherePlayers(x => x.roleManager.CurrentRole != null && x.roleManager.CurrentRole.RoleTypeId == role);

        public static void ForEach(this IEnumerable<ReferenceHub> hubs, Func<ReferenceHub, bool> condition, Action<ReferenceHub> action) {
            foreach (var hub in hubs) {
                if (hub is null)
                    continue;

                if (!condition(hub))
                    continue;

                action(hub);
            }
        }

        public static void ForEachPlayer(this IEnumerable<ReferenceHub> hubs, Func<ReferenceHub, bool> condition, Action<ReferenceHub> action) {
            foreach (var hub in hubs) {
                if (hub is null)
                    continue;

                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (hub.connectionToClient is null)
                    continue;

                if (DummyPlayer.IsDummy(hub))
                    continue;

                if (!condition(hub))
                    continue;

                action(hub);
            }
        }

        public static void ForEachPlayer(this IEnumerable<ReferenceHub> hubs,  Action<ReferenceHub> action) {
            foreach (var hub in hubs) {
                if (hub is null)
                    continue;

                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (hub.connectionToClient is null)
                    continue;

                if (DummyPlayer.IsDummy(hub))
                    continue;

                action(hub);
            }
        }

        public static void PositionMessage(this ReferenceHub target, Vector3 position, float rotation = 0f) {
            target?.connectionToClient?.Send(new FpcOverrideMessage(position, rotation));
        }

        public static void FakeRoleMessage(this ReferenceHub receiver, ReferenceHub target, RoleTypeId fakeRole) {
            receiver?.connectionToClient?.Send(new RoleSyncInfo(target, fakeRole, receiver));
        }

        public static void DisruptorHit(this ReferenceHub target, Vector3 position, Quaternion rotation) {
            target.connectionToClient?.Send(new DisruptorHitreg.DisruptorHitMessage {
                Position = position,
                Rotation = new LowPrecisionQuaternion(rotation)
            });
        }

        public static void BeepSound(this ReferenceHub target) {
            target.FakeRpc(HostIdentity, typeof(AmbientSoundPlayer), nameof(AmbientSoundPlayer.RpcPlaySound), 7);
        }

        public static void GunSound(this ReferenceHub target, Vector3 position, ItemType gunType, byte volume, byte clipId = 0) {
            target.connectionToClient.Send(new GunAudioMessage {
                AudioClipId = clipId,
                MaxDistance = volume,
                ShooterHub = target,
                ShooterPosition = new RelativePosition(position),
                Weapon = gunType
            });
        }

        public static void TargetRoomColor(this ReferenceHub hub, RoomIdentifier room, Color color) {
            var lightController = FlickerableLightController.Instances.FirstOrDefault(x => x.Room != null && x.Room.GetInstanceID() == room.GetInstanceID());
            if (lightController is null)
                return;

            hub.FakeSyncVar(lightController.netIdentity, typeof(FlickerableLightController), nameof(FlickerableLightController.Network_warheadLightOverride), true);
            hub.FakeSyncVar(lightController.netIdentity, typeof(FlickerableLightController), nameof(FlickerableLightController.Network_warheadLightColor), color);
        }

        public static void TargetCassie(this ReferenceHub hub, string cassieMessage, bool makeHold = false, bool makeNoise = true, bool isSubtitles = false) {
            foreach (var controller in RespawnEffectsController.AllControllers) {
                if (controller is null)
                    continue;

                hub.FakeRpc(controller.netIdentity, typeof(RespawnEffectsController), nameof(RespawnEffectsController.RpcCassieAnnouncement), cassieMessage, makeHold, makeNoise, isSubtitles);
            }
        }

        public static void TargetCassieTranslation(this ReferenceHub hub, string cassieMessage, string translation, bool makeHold = false, bool makeNoise = true, bool isSubtitles = true) {
            var annoucement = StringBuilderPool.Shared.Rent();
            var cassies = cassieMessage.Split('\n');
            var translations = translation.Split('\n');

            for (int i = 0; i < cassies.Length; i++)
                annoucement.Append($"{translations[i]}<size=0> {cassies[i].Replace(' ', ' ')} </size><split>");

            cassieMessage = annoucement.ToString();

            StringBuilderPool.Shared.Return(annoucement);

            foreach (var controller in RespawnEffectsController.AllControllers) {
                if (controller is null)
                    continue;

                hub.FakeRpc(controller.netIdentity, typeof(RespawnEffectsController), nameof(RespawnEffectsController.RpcCassieAnnouncement), cassieMessage, translation, makeHold, makeNoise, isSubtitles);
            }
        }

        public static void TargetCustomInfoString(this ReferenceHub player, ReferenceHub target, string infoString) {
            player.FakeSyncVar(target.networkIdentity, typeof(NicknameSync), nameof(NicknameSync.Network_customPlayerInfoString), infoString);
        }

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