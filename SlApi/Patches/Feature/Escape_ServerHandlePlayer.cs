using AzyWorks.Randomization.Weighted;

using HarmonyLib;

using InventorySystem.Disarming;

using PlayerRoles;

using Respawning;

using SlApi.Configs;
using SlApi.Features.CustomEscape;

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SlApi.Patches.Feature
{
    [HarmonyPatch(typeof(Escape), nameof(Escape.ServerHandlePlayer))]
    public static class Escape_ServerHandlePlayer
    {
        [Config("CustomEscape.ChancedEscapes", "List of chanced role conversions when escaping.")]
        public static HashSet<ChancedEscape> ChancedEscapes = new HashSet<ChancedEscape>()
        {
            new ChancedEscape()
            {
                EscapingAs = RoleTypeId.ChaosRepressor,
                CuffedByOpposingTeamOnly = true,
                ChangingTo = new Dictionary<RoleTypeId, int>()
                {
                    [RoleTypeId.NtfSergeant] = 60,
                    [RoleTypeId.NtfCaptain] = 5,
                    [RoleTypeId.NtfPrivate] = 35
                }
            }
        };

        [Config("CustomEscape.Enabled", "Whether or not to enable custom escapes.")]
        public static bool Enabled = true;

        private static bool ShouldOverride(ReferenceHub hub, out RoleTypeId changeTo)
        {
            if (ChancedEscapes.Count <= 0 || !Enabled)
            {
                changeTo = RoleTypeId.None;
                return true;
            }

            if (!(hub.roleManager.CurrentRole is HumanRole role))
            {
                changeTo = RoleTypeId.None;
                return false;
            }

            if ((role.FpcModule.Position - Escape.WorldPos).sqrMagnitude > 156.5f)
            {
                changeTo = RoleTypeId.None;
                return false;
            }

            if (role.ActiveTime < 10f)
            {
                changeTo = RoleTypeId.None;
                return false;
            }

            var escape = ChancedEscapes.FirstOrDefault(x => EscapePicker(hub, x));

            if (escape != null)
            {
                changeTo = WeightPicker.Pick(escape.ChangingTo, x => x.Value).Key;
                return true;
            }

            changeTo = RoleTypeId.None;
            return true;
        }

        private static Team[] GetOpposingTeams(Team team)
        {
            if (team is Team.ChaosInsurgency || team is Team.ClassD)
                return new Team[] { Team.FoundationForces, Team.Scientists, Team.OtherAlive };
            else if (team is Team.FoundationForces)
                return new Team[] { Team.ClassD, Team.ChaosInsurgency, Team.OtherAlive };
            else if (team is Team.OtherAlive)
                return new Team[] { Team.FoundationForces, Team.Scientists, Team.OtherAlive, Team.ChaosInsurgency, Team.ClassD };

            return null;
        }

        private static bool EscapePicker(ReferenceHub hub, ChancedEscape chancedEscape)
        {
            if (hub.roleManager.CurrentRole.RoleTypeId != chancedEscape.EscapingAs)
                return false;

            if (chancedEscape.CuffedByOpposingTeamOnly)
            {
                if (hub.inventory.IsDisarmed())
                {
                    var disarmerId = DisarmedPlayers.Entries.FirstOrDefault(x => x.DisarmedPlayer == hub.netId).Disarmer;

                    if (ReferenceHub.TryGetHubNetID(disarmerId, out var disarmer))
                    {
                        var opposingTeams = GetOpposingTeams(hub.roleManager.CurrentRole.Team);

                        if (!opposingTeams.Contains(disarmer.roleManager.CurrentRole.Team))
                            return false;
                        else
                            return true;
                    }

                    return false;
                }

                return false;
            }

            return true;
        }

        public static bool Prefix(ReferenceHub hub)
        {
            if (!ShouldOverride(hub, out var changeTo))
                return false;

            if (changeTo is RoleTypeId.None)
                return true;

            var escapeScenarioType = Escape.ServerGetScenario(hub);

            switch (escapeScenarioType)
            {
                case Escape.EscapeScenarioType.ClassD:
                case Escape.EscapeScenarioType.CuffedScientist:
                    RespawnTokensManager.GrantTokens(SpawnableTeamType.ChaosInsurgency, 4f);
                    break;
                case Escape.EscapeScenarioType.CuffedClassD:
                    RespawnTokensManager.GrantTokens(SpawnableTeamType.NineTailedFox, 3f);
                    break;
                case Escape.EscapeScenarioType.Scientist:
                    RespawnTokensManager.GrantTokens(SpawnableTeamType.NineTailedFox, 3f);
                    break;
            }

            hub.connectionToClient.Send(new Escape.EscapeMessage
            {
                ScenarioId = (byte)escapeScenarioType,
                EscapeTime = (ushort)Mathf.CeilToInt(hub.roleManager.CurrentRole.ActiveTime)
            });

            hub.roleManager.ServerSetRole(changeTo, RoleChangeReason.Escaped, RoleSpawnFlags.All);
            return false;
        }
    }
}
