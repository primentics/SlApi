using AzyWorks.Randomization.Weighted;
using Footprinting;
using HarmonyLib;

using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;

using MapGeneration;
using Mono.Cecil.Cil;
using PlayerRoles.FirstPersonControl;
using PluginAPI.Core;
using PluginAPI.Enums;
using PluginAPI.Events;

using SlApi.Configs;
using SlApi.Configs.Objects;
using SlApi.Events;
using SlApi.Events.CustomHandlers;
using SlApi.Extensions;

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SlApi.Features.Scp1162
{
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.UserCode_CmdDropItem))]
    public static class Scp1162Controller
    {
        private static Vector3 _position = Vector3.zero;
        private static RoomIdentifier _room;

        [Config("Scp1162.Debug", "Debug toggle")]
        public static bool Debug = true;

        [Config("Scp1162.Position", "The position of SCP-1162 item exchange.")]
        public static Vector Position { get; set; } = Vector.Get(0f, 0f, 0f);

        [Config("Scp1162.Room", "If you want to spawn SCP-1162 in a room instead.")]
        public static RoomName Room { get; set; } = RoomName.Unnamed;

        [Config("Scp1162.Mode", "Sets the operating mode of SCP-1162 (Room/Position)")]
        public static Scp1162Mode Mode { get; set; } = Scp1162Mode.Position;

        [Config("Scp1162.Bounds", "Maximum distance from the position to be considered inside SCP-1162.")]
        public static float Bounds { get; set; } = 5f;

        [Config("Scp1162.Enabled", "Whether or not to enable SCP-1162.")]
        public static bool IsEnabled { get; set; } = true;

        [Config("Scp1162.Message", "The message to display when a player drops an item in SCP-1162.")]
        public static string DropMessage { get; set; } = "<i>You try to drop the item through <color=yellow>SCP-1162</color> to get another...</i>";

        [Config("Scp1162.Items", "All items that can be exchanged via SCP-1162.")]
        public static List<Scp1162Item> Items { get; set; } = new List<Scp1162Item>()
        {
            new Scp1162Item()
            {
                AcceptedItem = ItemType.SCP500,
                OutputItems = new Dictionary<ItemType, int>()
                {
                    [ItemType.Medkit] = 50,
                    [ItemType.Adrenaline] = 25,
                    [ItemType.ArmorLight] = 15,
                    [ItemType.Flashlight] = 10
                }
            }
        };

        static Scp1162Controller()
        {
            EntryPoint.OnLoaded += () =>
            {
                OnRoundStart(null);
            };

            EntryPoint.OnReloaded += () =>
            {
                OnRoundStart(null);
            };

            EventHandlers.RegisterEvent(new GenericHandler(ServerEventType.RoundStart, OnRoundStart));
        }

        public static bool Prefix(Inventory __instance, ushort itemSerial, bool tryThrow)
        {
            if (!IsEnabled)
                return true;

            if (!__instance.UserInventory.Items.TryGetValue(itemSerial, out var itemBase))
                return false;

            if (!itemBase.CanHolster())
                return false;

            if (!EventManager.ExecuteEvent(ServerEventType.PlayerDropItem, __instance._hub, itemBase))
                return false;

            if (Scp1162TryProcessDrop(__instance, itemBase))
                return false;

            var pickup = DropItem(itemBase);

            __instance.SendItemsNextFrame = true;

            if (!tryThrow || pickup is null || !pickup.TryGetComponent(out Rigidbody rigidbody))
                return false;

            if (!EventManager.ExecuteEvent(ServerEventType.PlayerThrowItem, __instance._hub, itemBase, rigidbody))
                return false;

            var velocity = __instance._hub.GetVelocity();
            var vector = velocity / 3f + __instance._hub.PlayerCameraReference.forward * 6f * (Mathf.Clamp01(Mathf.InverseLerp(7f, 0.1f, rigidbody.mass)) + 0.3f);

            vector.x = Mathf.Max(Mathf.Abs(velocity.x), Mathf.Abs(vector.x)) * ((vector.x < 0f) ? -1 : 1);
            vector.y = Mathf.Max(Mathf.Abs(velocity.y), Mathf.Abs(vector.y)) * ((vector.y < 0f) ? -1 : 1);
            vector.z = Mathf.Max(Mathf.Abs(velocity.z), Mathf.Abs(vector.z)) * ((vector.z < 0f) ? -1 : 1);

            rigidbody.position = __instance._hub.PlayerCameraReference.position;
            rigidbody.velocity = vector;
            rigidbody.angularVelocity = Vector3.Lerp(itemBase.ThrowSettings.RandomTorqueA, itemBase.ThrowSettings.RandomTorqueB, Random.value);

            if (rigidbody.angularVelocity.magnitude > rigidbody.maxAngularVelocity)
                rigidbody.maxAngularVelocity = rigidbody.angularVelocity.magnitude;

            return false;
        }

        private static bool Scp1162TryProcessDrop(Inventory inventory, ItemBase item)
        {
            if (!IsInRange(inventory._hub.GetRealPosition()))
                return false;

            var outputItem = Items.FirstOrDefault(x => x.AcceptedItem == item.ItemTypeId);
            if (outputItem is null)
                return false;

            var outItem = WeightPicker.Pick(outputItem.OutputItems, x => x.Value).Key;
            if (outItem is ItemType.None)
                return false;

            inventory.ServerRemoveItem(item.ItemSerial, item.PickupDropModel);

            var newItem = inventory.ServerAddItem(outItem, ItemSerialGenerator.GenerateNext());
            if (newItem is null)
                return false;

            if (!string.IsNullOrWhiteSpace(DropMessage))
                inventory._hub.PersonalHint(DropMessage, 5f);

            DropItem(newItem);
            return true;
        }

        private static ItemPickupBase DropItem(ItemBase item)
        {
            if (item.PickupDropModel == null)
            {
                Log.Debug($"Missing drop model", Debug, "SL API::Scp1162");
                return null;
            }

            var syncInfo = new PickupSyncInfo(item.ItemTypeId, item.Owner.transform.position, 
                                                                Quaternion.identity, item.Weight, item.ItemSerial);

            var pickup = item.OwnerInventory.ServerCreatePickup(item, syncInfo, true);

            item.OwnerInventory.ServerRemoveItem(syncInfo.Serial, pickup);
            pickup.PreviousOwner = new Footprint(item.Owner);

            return pickup;
        }

        private static bool IsInRange(Vector3 pos)
        {
            if (Mode is Scp1162Mode.Position)
            {
                if (_position != Vector3.zero)
                {
                    return Vector3.Distance(_position, pos) <= Bounds;
                }

                return false;
            }

            if (Mode is Scp1162Mode.Room)
            {
                if (_room != null)
                {
                    var plyRoom = RoomIdUtils.RoomAtPosition(pos);
                    if (plyRoom != null)
                    {
                        return plyRoom.Name == _room.Name;
                    }
                }

                return false;
            }

            return false;
        }

        private static void OnRoundStart(object[] args)
        {
            if (!IsEnabled)
            {
                _position = Vector3.zero;
                _room = null;

                return;
            }

            if (Mode is Scp1162Mode.Position)
            {
                _position = Vector.FromVector(Position);
                return;
            }

            if (Mode is Scp1162Mode.Room)
            {
                if (Room != RoomName.Unnamed)
                {
                    var room = RoomIdentifier.AllRoomIdentifiers.FirstOrDefault(x => x.Name == Room);
                    if (room != null)
                    {
                        _room = room;
                        return;
                    }
                }
            }
        }
    }
}
