using RoundRestarting;
using SlApi.Events.CustomHandlers;
using SlApi.Events;

using System.Collections.Generic;

using AzyWorks.Randomization.Weighted;

using UnityEngine;

using NorthwoodLib.Pools;

using PluginAPI.Core;

using PlayerRoles;

using SlApi.Extensions;

using System.Linq;

namespace SlApi.Features.CustomSinkholes
{
    public static class CustomSinkholeController
    {
        private static bool _pauseUpdate;
        private static float _timeScalar = 0f;

        private static HashSet<CustomSinkholeBase> _spawnedSinkholes = new HashSet<CustomSinkholeBase>();

        public static HashSet<CustomSinkholeBase> AllSinkholes { get; } = new HashSet<CustomSinkholeBase>()
        {

        };

        static CustomSinkholeController()
        {
            StaticUnityMethods.OnUpdate += OnUpdate;

            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.WaitingForPlayers, OnWaitingForPlayers));
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.RoundRestart, OnRoundRestart));
        }

        public static void SpawnSinkhole(CustomSinkholeBase sinkhole)
        {
            var copy = sinkhole.DoCopy();

            sinkhole.SpawnedInstances++;
            sinkhole.SpawnedThisRound++;

            copy.Spawn();

            _spawnedSinkholes.Add(sinkhole);
        }

        public static void CheckSpawnableSinkholes()
        {
            var chosenSinkholes = HashSetPool<CustomSinkholeBase>.Shared.Rent();

            foreach (var sinkhole in AllSinkholes)
            {
                if (!sinkhole.CheckSpawnInterval())
                    continue;

                if (!sinkhole.CheckRoundState())
                    continue;

                if (sinkhole.MaxAmount != -1 && sinkhole.SpawnedInstances >= sinkhole.MaxAmount)
                    continue;

                chosenSinkholes.Add(sinkhole);
            }

            var chosenSinkhole = WeightPicker.Pick(chosenSinkholes, x =>
                                            x.SpawnedThisRound > 0
                                                ? Mathf.CeilToInt(x.Chance / (x.SpawnedThisRound / 2f))
                                                : x.Chance);

            if (chosenSinkhole != null)
                SpawnSinkhole(chosenSinkhole);

            HashSetPool<CustomSinkholeBase>.Shared.Return(chosenSinkholes);
        }

        private static void OnUpdate()
        {
            if (!Round.IsRoundStarted || _pauseUpdate)
                return;

            _timeScalar += Time.deltaTime;

            if (_timeScalar >= 1f)
            {
                CheckSpawnableSinkholes();

                for (int i = 0; i < _spawnedSinkholes.Count; i++)
                {
                    var sinkhole = _spawnedSinkholes.ElementAt(i);

                    if (sinkhole.MaxDuration != -1)
                    {
                        sinkhole.IncrementSpawnDuration();

                        if (sinkhole.CurDuration >= sinkhole.MaxDuration)
                        {
                            sinkhole.Despawn();
                            sinkhole.Prefab.SpawnedInstances--;

                            _spawnedSinkholes.Remove(sinkhole);
                        }
                    }
                }

                _timeScalar = 0f;
            }

            foreach (var sinkhole in _spawnedSinkholes)
            {
                foreach (var hub in ReferenceHub.AllHubs)
                {
                    if (hub.Mode != ClientInstanceMode.ReadyClient)
                        continue;

                    if (!hub.IsAlive())
                        continue;

                    if (Vector3.Distance(hub.GetRealPosition(), sinkhole.Position) > sinkhole.Bounds)
                    {
                        sinkhole.SteppedPlayers.Remove(hub);
                        continue;
                    }

                    if (sinkhole.SteppedPlayers.Add(hub))
                        sinkhole.OnPlayer(hub);
                }
            }
        }

        private static void OnRoundRestart(object[] args)
        {
            _pauseUpdate = true;
            _spawnedSinkholes.Clear();

            foreach (var prefab in AllSinkholes)
            {
                prefab.SpawnedInstances = 0;
                prefab.SpawnedThisRound = 0;
            }
        }

        private static void OnWaitingForPlayers(object[] args)
        {
            _pauseUpdate = false;
            _timeScalar = 0f;
        }
    }
}
