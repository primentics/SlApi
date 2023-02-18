using HarmonyLib;
using MEC;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.NetworkMessages;

using PluginAPI.Core;

using SlApi.Configs;
using SlApi.Events;
using SlApi.Events.CustomHandlers;
using SlApi.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SlApi.Features.Spawnpoints
{
    public static class SpawnpointManager
    {
        private static Dictionary<string, SpawnpointDefinition> _spawnpoints = new Dictionary<string, SpawnpointDefinition>();

        [Config("Spawnpoints.Debug", "Debug toggle")]
        public static bool Debug = true;

        [Config("Spawnpoints.SyncMode", "Sets the sync mode of player occupations.")]
        public static SpawnpointSyncMode SyncMode = SpawnpointSyncMode.CheckOccupation;

        [Config("Spawnpoints.Points", "A list of spawnpoints.")]
        public static HashSet<SpawnpointBase> AllSpawnpoints { get; set; } = new HashSet<SpawnpointBase>()
        {
            new SpawnpointBase()
        };

        public static event Action<SpawnpointBase, ReferenceHub> OnPlayerSpawnedOnPoint;
        public static event Action<SpawnpointBase, ReferenceHub> OnPlayerLeftPoint;
        public static event Action<SpawnpointBase, ReferenceHub> OnPlayerEnteredPoint;

        public static event Action<ReferenceHub, FirstPersonMovementModule, Vector3, Vector3> OnPositionOverriden;

        static SpawnpointManager()
        {
            OnPositionOverriden += OnPlayerPositionOverriden;

            StaticUnityMethods.OnUpdate += OnUpdate;

            EntryPoint.OnReloaded += ReloadSpawnpoints;
            EntryPoint.OnLoaded += ReloadSpawnpoints;

            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.PlayerSpawn, OnPlayerSpawned));
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.RoundRestart, OnRoundRestart));
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.PlayerLeft, OnPlayerLeft));
        }

        [HarmonyPatch(typeof(FirstPersonMovementModule), nameof(FirstPersonMovementModule.ServerOverridePosition))]
        public static bool Prefix(FirstPersonMovementModule __instance, Vector3 position, Vector3 deltaRotation)
        {
            var oldPos = __instance.Position;

            __instance.Position = position;
            __instance.Hub.connectionToClient.Send(new FpcOverrideMessage(position, deltaRotation.y), 0);
            __instance.OnServerPositionOverwritten();

            OnPositionOverriden?.Invoke(__instance.Hub, __instance, oldPos, position);

            return false;
        }

        public static void ReloadSpawnpoints()
        {
            foreach (var spawn in _spawnpoints)
                spawn.Value.RemoveSpawnpoint();

            _spawnpoints.Clear();

            foreach (var point in AllSpawnpoints)
            {
                var spawn = new SpawnpointDefinition(point);
                spawn.AddSpawnpoint();
                _spawnpoints[point.Name] = spawn;
            }
        }

        public static bool TryGetSpawnpointAtPosition(Vector3 pos, out SpawnpointDefinition spawnpoint)
        {
            foreach (var spawn in _spawnpoints)
            {
                if (IsInRange(pos, spawn.Value))
                {
                    spawnpoint = spawn.Value;
                    return true;
                }
            }

            spawnpoint = null;
            return false;
        }

        public static bool TryGetSpawnpoint(ReferenceHub hub, out SpawnpointDefinition spawnpoint)
        {
            foreach (var point in _spawnpoints)
            {
                if (point.Value.CanSpawn(hub))
                {
                    spawnpoint = point.Value;
                    return true;
                }
            }

            spawnpoint = null;
            return false;
        }

        public static bool TryGetSpawnpoint(string name, out SpawnpointDefinition spawnpoint)
        {
            foreach (var point in _spawnpoints)
            {
                if (point.Key.ToLower() == name.ToLower())
                {
                    spawnpoint = point.Value;
                    return true;
                }
            }

            spawnpoint = null;
            return false;
        }

        public static SpawnpointBase GetSpawnpointBase(SpawnpointDefinition spawnpointDefinition)
            => AllSpawnpoints.FirstOrDefault(x => x.Name == spawnpointDefinition.Name);

        public static void RemovePlayer(ReferenceHub hub, SpawnpointDefinition definition)
        {
            definition.PlayersIn.RemoveWhere(x => x.netId == hub.netId);
            OnPlayerLeftPoint?.Invoke(GetSpawnpointBase(definition), hub);
        }

        public static void AddPlayer(ReferenceHub hub, SpawnpointDefinition definition)
        {
            definition.PlayersIn.Add(hub);
            OnPlayerEnteredPoint?.Invoke(GetSpawnpointBase(definition), hub);
        }

        public static bool IsInRange(Vector3 hubPos, SpawnpointDefinition spawnpointDefinition)
        {
            if (Vector3.Distance(hubPos, spawnpointDefinition.Position) <= spawnpointDefinition.Bounds)
            {
                if (CalculateYAxisDistance(hubPos, spawnpointDefinition.Position) <= 1.5f)
                {
                    return true;
                }
            }

            return false;
        }

        public static float CalculateYAxisDistance(Vector3 pos, Vector3 otherPos)
        {
            if (pos.y > otherPos.y)
                return otherPos.y - pos.y;
            else
                return pos.y - otherPos.y;
        }

        private static void OnPlayerSpawned(object[] args)
        {
            Timing.CallDelayed(0.5f, () =>
            {
                var hub = (args[0] as Player).ReferenceHub;
                var pos = hub.GetRealPosition();

                if (TryGetSpawnpoint(hub, out var point))
                {
                    point.Spawn(hub);

                    OnPlayerSpawnedOnPoint?.Invoke(GetSpawnpointBase(point), hub);
                }
            });
        }

        private static void OnRoundRestart(object[] args)
        {
            foreach (var point in _spawnpoints)
                point.Value.PlayersIn.Clear();

            ReloadSpawnpoints();
        }

        private static void OnPlayerLeft(object[] args)
        {
            var hub = (args[0] as Player).ReferenceHub;

            foreach (var point in _spawnpoints)
                point.Value.PlayersIn.Remove(hub);
        }

        private static void OnPlayerPositionOverriden(ReferenceHub hub, FirstPersonMovementModule module, Vector3 oldPos, Vector3 newPos)
        {
            Log.Debug($"Position overriden: {hub.characterClassManager.UserId} / {hub.roleManager.CurrentRole.RoleTypeId} / " +
                $"{oldPos} / {newPos}", Debug, "SL API::Spawnpoint Manager");

            if (SyncMode != SpawnpointSyncMode.SpawnAndRoleChange)
                return;

            if (!hub.IsAlive())
                return;

            if (hub.Mode != ClientInstanceMode.ReadyClient)
                return;

            if (TryGetSpawnpointAtPosition(oldPos, out var oldPoint) 
                && oldPoint.PlayersIn.Contains(hub))
            {
                if (!TryGetSpawnpointAtPosition(newPos, out var newPoint))
                {
                    RemovePlayer(hub, oldPoint);
                }
                else
                {
                    if (newPoint.Roles.Contains(hub.roleManager.CurrentRole.RoleTypeId)
                        && !newPoint.PlayersIn.Contains(hub))
                    {
                        AddPlayer(hub, newPoint);
                    }
                }
            }
            else
            {
                if (TryGetSpawnpointAtPosition(newPos, out var newPoint)
                    && !newPoint.PlayersIn.Contains(hub)
                    && newPoint.Roles.Contains(hub.roleManager.CurrentRole.RoleTypeId))
                {
                    AddPlayer(hub, newPoint);
                }
            }
        }


        private static void OnUpdate()
        {
            if (SyncMode != SpawnpointSyncMode.CheckOccupation)
                return;

            foreach (var point in _spawnpoints)
            {
                for (int i = 0; i < point.Value.PlayersIn.Count; i++)
                {
                    var hub = point.Value.PlayersIn.ElementAt(i);

                    if (!hub.IsAlive())
                    {
                        RemovePlayer(hub, point.Value);
                        continue;
                    }

                    if (!point.Value.Roles.Contains(hub.roleManager.CurrentRole.RoleTypeId))
                    {
                        RemovePlayer(hub, point.Value);
                        continue;
                    }

                    if (!IsInRange(hub.GetRealPosition(), point.Value))
                    {
                        RemovePlayer(hub, point.Value);
                        continue;
                    }
                }
            }

            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (!hub.IsAlive())
                    continue;

                for (int i = 0; i < _spawnpoints.Count; i++)
                {
                    var point = _spawnpoints.ElementAt(i);

                    if (!point.Value.PlayersIn.Contains(hub))
                    {
                        if (point.Value.Roles.Contains(hub.roleManager.CurrentRole.RoleTypeId))
                        {
                            if (IsInRange(hub.GetRealPosition(), point.Value))
                            {
                                AddPlayer(hub, point.Value);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (!point.Value.Roles.Contains(hub.roleManager.CurrentRole.RoleTypeId)
                            || !IsInRange(hub.GetRealPosition(), point.Value))
                        {
                            RemovePlayer(hub, point.Value);
                            continue;
                        }
                    }
                }
            }
        }
    }
}