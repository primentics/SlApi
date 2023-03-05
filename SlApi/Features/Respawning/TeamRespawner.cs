using AzyWorks.Extensions;
using AzyWorks.System.Weights;
using AzyWorks.Utilities;

using HarmonyLib;

using PlayerRoles;

using PluginAPI.Enums;
using PluginAPI.Events;

using Respawning;
using Respawning.NamingRules;

using SlApi.Configs;
using SlApi.Events;
using SlApi.Events.CustomHandlers;
using SlApi.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;

using Log = PluginAPI.Core.Log;

namespace SlApi.Features.Respawning {
    [HarmonyPatch(typeof(RespawnManager), nameof(RespawnManager.Update))]
    public static class TeamRespawner {

        private static bool _configValidated;

        private static DateTime m_LastCheck = DateTime.MinValue;
        private static DateTime m_LastRespawn = DateTime.MinValue;

        private static readonly Dictionary<SpawnableTeamType, int> m_SpawnCount = new Dictionary<SpawnableTeamType, int>() {
            [SpawnableTeamType.NineTailedFox] = 0,
            [SpawnableTeamType.ChaosInsurgency] = 0
        };

        [Config("Respawn.Enabled", "Whether or not to enable the custom team respawner.")]
        public static bool IsEnabled { get; set; } = true;

        [Config("Respawn.Chances", "A list of teams and their spawn chance.")]
        public static Dictionary<SpawnableTeamType, int> SpawnChances { get; set; } = new Dictionary<SpawnableTeamType, int>() {
            [SpawnableTeamType.ChaosInsurgency] = 30,
            [SpawnableTeamType.NineTailedFox] = 70
        };

        [Config("Respawn.Amount", "A list of teams and a number of maximum players per spawn wave.")]
        public static Dictionary<SpawnableTeamType, int> MaxSpawnAmount { get; set; } = new Dictionary<SpawnableTeamType, int>() {
            [SpawnableTeamType.ChaosInsurgency] = 15,
            [SpawnableTeamType.NineTailedFox] = 15
        };

        [Config("Respawn.Time", "When should a team respawn occur.")]
        public static TeamRespawnTime RespawnTime { get; set; } = new TeamRespawnTime() { MaximalTime = 300, MinimalTime = 300 };

        [Config("Respawn.Conditions", "A list of teams and the conditions they must meet to respawn.")]
        public static Dictionary<SpawnableTeamType, TeamRespawnConditions> Conditions { get; set; } = new Dictionary<SpawnableTeamType, TeamRespawnConditions>() {
            [SpawnableTeamType.ChaosInsurgency] = new TeamRespawnConditions(),
            [SpawnableTeamType.NineTailedFox] = new TeamRespawnConditions()
        };

        public static SpawnableTeamType? NextTeam = null;

        public static readonly HashSet<SpawnableTeamType> DisabledTeams = new HashSet<SpawnableTeamType>();
        public static readonly IReadOnlyCollection<SpawnableTeamType> SpawnableTeams = new SpawnableTeamType[] { SpawnableTeamType.NineTailedFox, SpawnableTeamType.ChaosInsurgency };

        public static bool RespawningDisabled;
        public static bool ConfigValidationFailed;

        static TeamRespawner() {
            EventHandlers.RegisterEvent(new GenericHandler(ServerEventType.RoundRestart, OnRoundRestart));
        }

        public static bool Prefix(RespawnManager __instance) {
            if (RespawningDisabled)
                return false;

            if (!Enabled())
                return true;

            if (!_configValidated) {
                ValidateConfigs();
                return false;
            }

            if (!!__instance.ReadyToCommence()) {
                return false;
            }

            if (__instance._stopwatch.Elapsed.TotalSeconds > __instance._timeForNextSequence) {
                __instance._curSequence += 1;
            }

            if (__instance._curSequence == RespawnManager.RespawnSequencePhase.SelectingTeam) {
                if ((DateTime.Now - m_LastCheck).TotalSeconds < 1) {
                    return false;
                }

                if (!TryDecideNextTeam(out var nextTeam)) {
                    __instance.RestartSequence();
                    return false;
                }
                
                if (!RespawnManager.SpawnableTeams.TryGetValue(nextTeam, out var spawnableTeamHandlerBase)) {
                    throw new NotImplementedException(string.Format("{0} was returned as dominating team despite not being implemented.", nextTeam));
                }

                if (!EventManager.ExecuteEvent(ServerEventType.TeamRespawnSelected, new object[]
                {
                    nextTeam
                })) {
                    __instance.RestartSequence();
                    return false;
                }

                Log.Info($"Selected next team: {nextTeam}", "SL API::TeamRespawner");

                __instance.NextKnownTeam = nextTeam;
                __instance._curSequence = RespawnManager.RespawnSequencePhase.PlayingEntryAnimations;
                __instance._stopwatch.Restart();
                __instance._timeForNextSequence = spawnableTeamHandlerBase.EffectTime;

                RespawnEffectsController.ExecuteAllEffects(RespawnEffectsController.EffectType.Selection, nextTeam);
            }

            if (__instance._curSequence == RespawnManager.RespawnSequencePhase.SpawningSelectedTeam) {
                m_SpawnCount[__instance.NextKnownTeam]++;
                m_LastRespawn = DateTime.Now;

                Log.Info($"Spawning team: {__instance.NextKnownTeam}", "SL API::TeamRespawner");

                __instance.Spawn();
                __instance.RestartSequence();
            }

            return false;
        }

        public static bool Enabled()
            => IsEnabled && !ConfigValidationFailed;

        public static bool TryDecideNextTeam(out SpawnableTeamType spawnableTeamType) {
            spawnableTeamType = SpawnableTeamType.None;

            if (RespawningDisabled)
                return false;

            if (m_SpawnCount.All(x => x.Value is 0) 
                && RespawnManager.Singleton._stopwatch.Elapsed.TotalSeconds < RoundHelper.ActiveSeconds)
                return false;

            var currentlySpawnableTeams = GetSpawnableTeams();
            if (currentlySpawnableTeams.Length < 1)
                return false;

            if (currentlySpawnableTeams.Length == 1) {
                spawnableTeamType = currentlySpawnableTeams.Single();
                return true;
            }

            if (!WeightPick.TryPick(currentlySpawnableTeams, x => {
                if (!SpawnChances.TryGetValue(x, out var chance))
                    return 0;

                return chance;
            }, out spawnableTeamType))
                return false;

            return true;
        }

        public static bool MeetsRequirements(SpawnableTeamType spawnableTeamType) {
            if (DisabledTeams.Contains(spawnableTeamType)) 
                return false;

            if (RespawningDisabled)
                return false;

            if (!Conditions.TryGetValue(spawnableTeamType, out var conditions))
                return true;

            if (conditions.MaxSpawns != -1 && conditions.MaxSpawns >= m_SpawnCount[spawnableTeamType])
                return false;

            var players = GetSpawnablePlayers();
            if (players.Count < 1 || players.Count < conditions.MinWaveSize)
                return false;

            if (conditions.TargetedRoleAlive) {
                if (spawnableTeamType is SpawnableTeamType.ChaosInsurgency) {
                    if (!ReferenceHub.AllHubs.WherePlayers(RoleTypeId.ClassD).Any())
                        return false;
                }
                else if (spawnableTeamType is SpawnableTeamType.NineTailedFox) {
                    if (!ReferenceHub.AllHubs.WherePlayers(RoleTypeId.Scientist).Any())
                        return false;
                }
            }

            return true;
        }

        public static SpawnableTeamType[] GetSpawnableTeams() {
            var teams = new HashSet<SpawnableTeamType>();

            foreach (var team in SpawnableTeams) {
                if (!MeetsRequirements(team))
                    continue;

                teams.Add(team);
            }

            return teams.ToArray();
        }

        public static HashSet<ReferenceHub> GetSpawnablePlayers() {
            var set = new HashSet<ReferenceHub>();

            ReferenceHub.AllHubs.ForEachPlayer(
                x => x.roleManager.CurrentRole.RoleTypeId is RoleTypeId.Spectator, 
                y => set.Add(y));

            return set;
        }

        public static void ValidateConfigs() {
            ConfigValidationFailed = false;

            foreach (var team in SpawnableTeams) { 
                if (!SpawnChances.TryGetValue(team, out var value)) {
                    Log.Warning($"Missing spawn chance for team: {team}", "SL API::TeamRespawner");
                    IsEnabled = false;
                    ConfigValidationFailed = true;
                    continue;
                }

                if (value < 0) {
                    Log.Warning($"Invalid spawn chance for team: {team} - value cannot be negative.", "SL API::TeamRespawner");
                    IsEnabled = false;
                    ConfigValidationFailed = true;
                    continue;
                }

                if (value > 100) {
                    Log.Warning($"Invalid spawn chance for team: {team} - value cannot be higher than hundred.", "SL API::TeamRespawner");
                    IsEnabled = false;
                    ConfigValidationFailed = true;
                    continue;
                }

                if (!Conditions.TryGetValue(team, out _)) {
                    Log.Warning($"Missing spawn conditions for team: {team}", "SL API::TeamRespawner");
                    IsEnabled = false;
                    ConfigValidationFailed = true;
                    continue;
                }

                if (!MaxSpawnAmount.TryGetValue(team, out _)) {
                    Log.Warning($"Missing max spawn amount for team: {team}", "SL API::TeamRespawner");
                    IsEnabled = false;
                    ConfigValidationFailed = true;
                    continue;
                }
            }

            if (SpawnChances.Keys.Match(SpawnableTeams)) {
                var totalChance = SpawnChances.Sum(x => x.Value);
                if (totalChance > 100) {
                    Log.Warning($"Total spawn chance is more than hundred.", "SL API::TeamRespawner");
                    IsEnabled = false;
                    ConfigValidationFailed = true;
                }
                else if (totalChance < 0) {
                    Log.Warning($"Total spawn chance is less than zero.", "SL API::TeamRespawner");
                    IsEnabled = false;
                    ConfigValidationFailed = true;
                }
            }

            if (ConfigValidationFailed) {
                Log.Error($"Failed to validate configs. Custom respawns will not work.", "SL API::TeamRespawner");
                return;
            }

            Log.Info($"Configuration succesfully validated.", "SL API::TeamRespawner");

            _configValidated = true;
        }

        public static int PickTime()
            => AzyWorks.System.RandomGenerator.Int32(RespawnTime.MaximalTime, RespawnTime.MinimalTime);

        private static void OnRoundRestart(object[] args) {
            m_SpawnCount.Clear();
            m_SpawnCount[SpawnableTeamType.ChaosInsurgency] = 0;
            m_SpawnCount[SpawnableTeamType.NineTailedFox] = 0;

            DisabledTeams.Clear();
            RespawningDisabled = false;

            ValidateConfigs();
        }
    }

    [HarmonyPatch(typeof(RespawnManager), nameof(RespawnManager.RestartSequence))]
    public static class RestartSequencePatch {
        public static bool Prefix(RespawnManager __instance) {
            if (!TeamRespawner.Enabled())
                return true;

            __instance._timeForNextSequence = TeamRespawner.PickTime();
            __instance._curSequence = RespawnManager.RespawnSequencePhase.RespawnCooldown;

            if (__instance._stopwatch.IsRunning) {
                __instance._stopwatch.Restart();
                return false;
            }

            __instance._stopwatch.Start();
            return false;
        }
    }

    [HarmonyPatch(typeof(RespawnManager), nameof(RespawnManager.Spawn))]
    public static class SpawnPatch {
        public static bool Prefix(RespawnManager __instance) {
            if (!TeamRespawner.Enabled())
                return true;

            if (!RespawnManager.SpawnableTeams.TryGetValue(__instance.NextKnownTeam, out var teamHandler)
                || __instance.NextKnownTeam == SpawnableTeamType.None) {
                return false;
            }

            if (!EventManager.ExecuteEvent(ServerEventType.TeamRespawn, __instance.NextKnownTeam)) {
                RespawnEffectsController.ExecuteAllEffects(RespawnEffectsController.EffectType.UponRespawn, __instance.NextKnownTeam);
                __instance.NextKnownTeam = SpawnableTeamType.None;
                return false;
            }

            var players = TeamRespawner.GetSpawnablePlayers().ToList();
            var maxSize = TeamRespawner.Conditions[__instance.NextKnownTeam].MaxWaveSize;
            var size = players.Count;

            Log.Info($"Spawning {players.Count} players with {maxSize} max wave size.", "SL API::TeamRespawner");

            if (maxSize != -1) {
                if (size > maxSize) {
                    players.RemoveRange(maxSize, size - maxSize);
                    size = maxSize;
                }
            }

            if (size > 0 && UnitNamingRule.TryGetNamingRule(__instance.NextKnownTeam, out var namingRule)) {
                UnitNameMessageHandler.SendNew(__instance.NextKnownTeam, namingRule);
            }

            var queue = new Queue<RoleTypeId>();

            teamHandler.GenerateQueue(queue, size);

            foreach (var hub in players) {
                if (!queue.TryDequeue(out var plyRole))
                    continue;

                hub.roleManager.ServerSetRole(plyRole, RoleChangeReason.Respawn, RoleSpawnFlags.All);
            }

            if (!HumanTerminationTokens.NumberOfRespawns.TryGetValue(__instance.NextKnownTeam, out var tokens))
                tokens = 0;

            HumanTerminationTokens.NumberOfRespawns[__instance.NextKnownTeam] = tokens + 1;

            __instance.NextKnownTeam = SpawnableTeamType.None;
            return false;
        }
    }
}