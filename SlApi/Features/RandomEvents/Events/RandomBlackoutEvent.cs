using Interactables.Interobjects.DoorUtils;

using MEC;

using PluginAPI.Core;

using SlApi.Configs;
using SlApi.Events;
using SlApi.Events.CustomHandlers;

using System;
using System.Collections.Generic;

using MapGeneration;
using System.Linq;

namespace SlApi.Features.RandomEvents.Events
{
    public class RandomBlackoutEvent : RandomEventBase
    {
        private DateTime _lastSpawnCheck = DateTime.Now;

        private static int BlackoutsOccured = 0;
        private static Dictionary<uint, bool> DoorStateCache = new Dictionary<uint, bool>();

        [Config("RandomBlackout.Chance", "The chance of a random blackout occuring.")]
        public static int BlackoutChance = 10;

        [Config("RandomBlackout.Duration", "The duration of a random blackout.")]
        public static float BlackoutDuration = 15f;

        [Config("RandomBlackout.MaxBlackouts", "Gets the maximum amount of blackouts that can occur in a round.")]
        public static int MaxBlackouts = -1;

        [Config("RandomBlackout.LockDoors", "Whether or not to lock doors when a blackout occurs.")]
        public static bool LockDoors = true;

        [Config("RandomBlackout.Interval", "Gets the amount of seconds between each spawn check.")]
        public static int Interval = 50;

        [Config("RandomBlackout.MinTime", "Gets the minimum amount of seconds that need to pass since the round start for this event to happen.")]
        public static int MinSeconds = 300;

        [Config("RandomBlackout.MaxTime", "Gets the maximum amount of seconds that can pass.")]
        public static int MaxSeconds = 1200;

        [Config("RandomBlackout.ZoneFilter", "Gets a list of zones the blackout can ocur in.")]
        public static FacilityZone[] ZoneFilter = new FacilityZone[]
        {
            FacilityZone.LightContainment,
            FacilityZone.HeavyContainment,
            FacilityZone.Entrance,
            FacilityZone.Surface,
            FacilityZone.None,
            FacilityZone.Other
        };

        [Config("RandomBlackout.RoomFilter", "Gets a list of rooms the blackout can occur in.")]
        public static RoomName[] RoomFilter = new RoomName[]
        {
            RoomName.EzCollapsedTunnel,
            RoomName.EzEvacShelter,
            RoomName.EzGateA,
            RoomName.EzGateB,
            RoomName.EzIntercom,
            RoomName.EzOfficeLarge,
            RoomName.EzOfficeSmall,
            RoomName.EzOfficeStoried,
            RoomName.EzRedroom,

            RoomName.Hcz049,
            RoomName.Hcz079,
            RoomName.Hcz096,
            RoomName.Hcz106,
            RoomName.Hcz939,
            RoomName.HczArmory,
            RoomName.HczCheckpointA,
            RoomName.HczCheckpointB,
            RoomName.HczCheckpointToEntranceZone,
            RoomName.HczMicroHID,
            RoomName.HczServers,
            RoomName.HczTesla,
            RoomName.HczTestroom,
            RoomName.HczWarhead,

            RoomName.Lcz173,
            RoomName.Lcz330,
            RoomName.Lcz914,
            RoomName.LczAirlock,
            RoomName.LczArmory,
            RoomName.LczCheckpointA,
            RoomName.LczCheckpointB,
            RoomName.LczClassDSpawn,
            RoomName.LczComputerRoom,
            RoomName.LczGlassroom,
            RoomName.LczGreenhouse,
            RoomName.LczToilets,

            RoomName.Outside,
            RoomName.Pocket,
            RoomName.Unnamed
        };

        public override int Chance => BlackoutChance;
        public override string Id => "random_blackout";

        static RandomBlackoutEvent()
        {
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.RoundRestart, OnRoundRestart));
        }

        public override bool CanDoEvent()
        {
            if (MaxBlackouts != -1)
            {
                return BlackoutsOccured + 1 <= MaxBlackouts;
            }

            return true;
        }

        public override bool CheckSpawnInterval()
        { 
            if (Interval != -1)
            {
                if ((DateTime.Now - _lastSpawnCheck).Seconds < Interval)
                    return false;
            }

            _lastSpawnCheck = DateTime.Now;
            return true;
        }

        public override bool CheckRoundState()
        {
            if (MinSeconds != -1)
            {
                if (Round.Duration.Seconds < MinSeconds)
                    return false;
            }

            if (MaxSeconds != -1)
            {
                if (Round.Duration.Seconds > MaxSeconds)
                    return false;
            }

            return true;
        }

        public override void DoEvent()
        {
            DoBlackout(BlackoutDuration);
        }

        private static void OnRoundRestart(object[] args)
        {
            BlackoutsOccured = 0;
            DoorStateCache.Clear();
        }

        public static void DoBlackout(float duration)
        {
            foreach (FlickerableLightController controller in FlickerableLightController.Instances)
            {
                var cRoom = controller.Room;

                if (cRoom != null)
                {
                    if (ZoneFilter.Length > 0)
                    {
                        if (!ZoneFilter.Contains(cRoom.Zone))
                            continue;
                    }

                    if (RoomFilter.Length > 0)
                    {
                        if (!RoomFilter.Contains(cRoom.Name))
                            continue;
                    }
                }

                controller.ServerFlickerLights(duration);
            }

            if (LockDoors)
            {
                foreach (var door in DoorVariant.AllDoors)
                {
                    if (door.Rooms != null && door.Rooms.Length > 0)
                    {
                        if (ZoneFilter.Length > 0)
                        {
                            if (!door.Rooms.Any(x => ZoneFilter.Contains(x.Zone)))
                                continue;
                        }

                        if (RoomFilter.Length > 0)
                        {
                            if (!door.Rooms.Any(x => RoomFilter.Contains(x.Name)))
                                continue;
                        }
                    }

                    DoorStateCache[door.netId] = door.NetworkTargetState;

                    door.NetworkTargetState = false;
                    door.ServerChangeLock(DoorLockReason.AdminCommand, true);
                }

                Timing.CallDelayed(duration, () =>
                {
                    foreach (var door in DoorVariant.AllDoors)
                    {
                        if (door.Rooms != null && door.Rooms.Length > 0)
                        {
                            if (ZoneFilter.Length > 0)
                            {
                                if (!door.Rooms.Any(x => ZoneFilter.Contains(x.Zone)))
                                    continue;
                            }

                            if (RoomFilter.Length > 0)
                            {
                                if (!door.Rooms.Any(x => RoomFilter.Contains(x.Name)))
                                    continue;
                            }
                        }

                        door.ServerChangeLock(DoorLockReason.AdminCommand, false);

                        if (DoorStateCache.TryGetValue(door.netId, out var oldState))
                            door.NetworkTargetState = oldState;
                    }
                });

                DoorStateCache.Clear();
            }
        }
    }
}