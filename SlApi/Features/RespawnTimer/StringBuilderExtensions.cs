using GameCore;

using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles;

using PluginAPI.Core;

using Respawning;

using System;
using System.Globalization;
using System.Linq;
using System.Text;

using UnityEngine;

namespace SlApi.Features.RespawnTimer
{
    public static class StringBuilderExtensions
    {
        public static StringBuilder SetAllProperties(this StringBuilder builder, int? spectatorCount = null) => builder
            .SetRoundTime()
            .SetMinutesAndSeconds()
            .SetSpawnableTeam()
            .SetSpectatorCountAndTickets(spectatorCount)
            .SetGeneratorCount()
            .SetTpsAndTickrate()
            .SetHint();

        private static StringBuilder SetRoundTime(this StringBuilder builder)
        {
            int minutes = RoundStart.RoundLength.Minutes;
            builder.Replace("{round_minutes}", $"{(RespawnTimerController.LeadingZeros && minutes < 10 ? "0" : string.Empty)}{minutes}");

            int seconds = RoundStart.RoundLength.Seconds;
            builder.Replace("{round_seconds}", $"{(RespawnTimerController.LeadingZeros && seconds < 10 ? "0" : string.Empty)}{seconds}");

            return builder;
        }

        private static StringBuilder SetMinutesAndSeconds(this StringBuilder builder)
        {
            TimeSpan time = TimeSpan.FromSeconds(RespawnManager.Singleton._timeForNextSequence - RespawnManager.Singleton._stopwatch.Elapsed.TotalSeconds);

            if ((RespawnManager.Singleton._curSequence is RespawnManager.RespawnSequencePhase.PlayingEntryAnimations 
                || RespawnManager.Singleton._curSequence is RespawnManager.RespawnSequencePhase.SpawningSelectedTeam) 
                || !RespawnTimerController.TimerOffset)
            {
                int minutes = (int)time.TotalSeconds / 60;
                builder.Replace("{minutes}", $"{(RespawnTimerController.LeadingZeros && minutes < 10 ? "0" : string.Empty)}{minutes}");

                int seconds = (int)Math.Round(time.TotalSeconds % 60);
                builder.Replace("{seconds}", $"{(RespawnTimerController.LeadingZeros && seconds < 10 ? "0" : string.Empty)}{seconds}");
            }
            else
            {
                int offset = RespawnTokensManager.Counters[1].Amount >= 50 ? 18 : 14;

                int minutes = (int)(time.TotalSeconds + offset) / 60;
                builder.Replace("{minutes}", $"{(RespawnTimerController.LeadingZeros && minutes < 10 ? "0" : string.Empty)}{minutes}");

                int seconds = (int)Math.Round((time.TotalSeconds + offset) % 60);
                builder.Replace("{seconds}", $"{(RespawnTimerController.LeadingZeros && seconds < 10 ? "0" : string.Empty)}{seconds}");
            }

            return builder;
        }

        private static StringBuilder SetSpawnableTeam(this StringBuilder builder)
        {
            switch (Respawn.NextKnownTeam)
            {
                case SpawnableTeamType.None:
                    return builder;

                case SpawnableTeamType.NineTailedFox:
                    builder.Replace("{team}", RespawnTimerController.NtfString);
                    break;

                case SpawnableTeamType.ChaosInsurgency:
                    builder.Replace("{team}", RespawnTimerController.CiString);
                    break;
            }

            return builder;
        }

        private static StringBuilder SetSpectatorCountAndTickets(this StringBuilder builder, int? spectatorCount = null)
        {
            builder.Replace("{spectators_num}", spectatorCount?.ToString() ?? Player.GetPlayers().Count(x => x.Role == RoleTypeId.Spectator && !x.IsOverwatchEnabled).ToString());
            builder.Replace("{ntf_tickets_num}", Mathf.Round(RespawnTokensManager.Counters[1].Amount).ToString());
            builder.Replace("{ci_tickets_num}", Mathf.Round(RespawnTokensManager.Counters[0].Amount).ToString());

            return builder;
        }

        private static StringBuilder SetGeneratorCount(this StringBuilder builder)
        {
            builder.Replace("{generator_engaged}", Scp079Recontainer.AllGenerators.Count(x => x.Engaged).ToString());
            builder.Replace("{generator_count}", "3");
            return builder;
        }

        private static StringBuilder SetTpsAndTickrate(this StringBuilder builder)
        {
            builder.Replace("{tps}", Math.Round(1.0 / Time.smoothDeltaTime).ToString(CultureInfo.InvariantCulture));
            builder.Replace("{tickrate}", Application.targetFrameRate.ToString());

            return builder;
        }

        private static StringBuilder SetHint(this StringBuilder builder)
        {
            if (!RespawnTimerController.Hints.Any())
                return builder;

            builder.Replace("{hint}", RespawnTimerController.Hints[RespawnTimerController.CurHintIndex]);

            return builder;
        }
    }
}
