using AzyWorks.Utilities;
using Interactables.Interobjects.DoorUtils;

using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Pickups;

using MapGeneration;

using MEC;

using Mirror;

using SlApi.Extensions;

using System.Collections.Generic;
using System.Linq;

namespace SlApi.Features
{
    public static class ServerHelper
    {
        private struct BlackoutSession
        {
            public string Id;

            public Dictionary<DoorVariant, bool> States;

            public BlackoutSession(string id, Dictionary<DoorVariant, bool> states)
            {
                Id = id;
                States = states;
            }

            public void SaveState(DoorVariant doorVariant)
            {
                States[doorVariant] = doorVariant.NetworkTargetState;

                doorVariant.NetworkTargetState = false;
                doorVariant.ServerChangeLock(DoorLockReason.AdminCommand, true);
            }

            public void RestoreStates()
            {
                foreach (var door in States)
                {
                    door.Key.NetworkTargetState = door.Value;
                    door.Key.ServerChangeLock(DoorLockReason.AdminCommand, false);
                }

                States.Clear();
            }
        }

        public static void DoBlackout(float time, bool lockDoors = true, FacilityZone[] filter = null)
        {
            foreach (var room in RoomIdentifier.AllRoomIdentifiers)
            {
                if (filter != null)
                {
                    if (!filter.Contains(room.Zone))
                    {
                        continue;
                    }
                }

                DoBlackout(room, time, lockDoors);
            }
        }

        public static void DoBlackout(float time, bool lockDoors = true, RoomName[] filter = null)
        {
            foreach (var room in RoomIdentifier.AllRoomIdentifiers)
            {
                if (filter != null)
                {
                    if (!filter.Contains(room.Name))
                    {
                        continue;
                    }
                }

                DoBlackout(room, time, lockDoors);
            }
        }

        public static void DoBlackout(RoomIdentifier room, float time, bool lockDoors = true)
        {
            foreach (var flicker in FlickerableLightController.Instances)
            {
                if (flicker.Room is null || flicker.Room.GetInstanceID() != room.GetInstanceID())
                    continue;

                flicker.ServerFlickerLights(time);
            }

            if (lockDoors)
            {
                string id = StaticRandom.RandomTicket(20);

                var session = new BlackoutSession();

                session.Id = id;
                session.States = new Dictionary<DoorVariant, bool>();

                foreach (var door in DoorVariant.AllDoors)
                {
                    if (door.Rooms.Any(x => x != null && x.GetInstanceID() == room.GetInstanceID()))
                    {
                        session.SaveState(door);
                    }
                }

                Timing.CallDelayed(time, session.RestoreStates);
            }
        }

        public static void SetupFirearm(Firearm firearm, ReferenceHub owner)
        {
            if (AttachmentsServerHandler.PlayerPreferences.TryGetValue(owner, out var preferences))
            {
                if (preferences.TryGetValue(firearm.ItemTypeId, out var prefCode))
                {
                    firearm.ApplyAttachmentsCode(prefCode, true);
                }
            }

            var baseFlags = FirearmStatusFlags.MagazineInserted;

            if (firearm.HasAdvantageFlag(AttachmentDescriptiveAdvantages.Flashlight))
                baseFlags |= FirearmStatusFlags.FlashlightEnabled;

            firearm.Status = new FirearmStatus(
                firearm.AmmoManagerModule.MaxAmmo,
                baseFlags,
                firearm.GetCurrentAttachmentsCode());
        }

        public static ItemBase GiveItem(ReferenceHub hub, ItemType type)
        {
            var item = hub.inventory.ServerAddItem(type, ItemSerialGenerator.GenerateNext());

            if (item is null)
                return null;

            if (item is Firearm firearm)
                SetupFirearm(firearm, hub);

            return item;
        }

        public static ItemPickupBase SpawnItem(ReferenceHub owner, ItemType type, bool spawn = true)
        {
            var item = owner.inventory.CreateItemInstance(new ItemIdentifier(type, ItemSerialGenerator.GenerateNext()), owner.isLocalPlayer);

            if (item is null) 
                return null;

            var pickupInfo = new PickupSyncInfo(
                type, 
                owner.GetRealPosition(), 
                owner.GetRealRotation(), 
                item.Weight, 
                item.ItemSerial);

            var pickup = UnityEngine.Object.Instantiate<ItemPickupBase>(
                item.PickupDropModel,
                pickupInfo.Position,
                pickupInfo.Rotation);

            pickup.NetworkInfo = pickupInfo;

            if (spawn)
                NetworkServer.Spawn(pickup.gameObject);

            pickup.InfoReceived(default, pickupInfo);

            return pickup;
        }

        public static void DoGlobalBroadcast(object message, ushort time)
        {
            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                hub.PersonalBroadcast(message, time);
            }
        }

        public static void DoGlobalHint(object message, ushort time)
        {
            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                hub.PersonalHint(message, time);
            }
        }

        public static void DoGlobalConsoleMessage(object message, string color = "green")
        {
            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                hub.ConsoleMessage(message, color);
            }
        }
    }
}