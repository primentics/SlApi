using PlayerRoles;

using Respawning;

using SlApi.Configs;
using SlApi.Events;
using SlApi.Events.CustomHandlers;
using SlApi.Extensions;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using AzyWorks.System.Weights;

using MEC;

namespace SlApi.Features.RespawnTimer
{
    public static class RespawnTimerController
    {
        private static CoroutineHandle _timerCoroutine;

        static RespawnTimerController()
        {
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.WaitingForPlayers, OnWaitingForPlayers));
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.RoundStart, OnRoundStart));

            EntryPoint.OnReloaded += CompileLines;
        }

        [Config("RespawnTimer.Enabled", "Whether or not to show hints.")]
        public static bool Enabled { get; set; } = true;

        [Config("RespawnTimer.HideInOverwatch", "Whether the timer should be hidden for players in overwatch.")]
        public static bool HideTimerForOverwatch { get; set; } = true;

        [Config("RespawnTimer.LeadingZeros", "Whether the leading zeros should be added in minutes and seconds if number is less than 10.")]
        public static bool LeadingZeros { get; set; } = true;

        [Config("RespawnTimer.TimerOffset", "Whether the timer should add time offset depending on MTF/CI spawn.")]
        public static bool TimerOffset { get; set; } = true;

        [Config("RespawnTimer.HintInterval", "How often custom hints should be changed (in seconds).")]
        public static int HintInterval { get; set; } = 10;

        [Config("RespawnTimer.Translations")]
        public static Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>()
        {
            ["activated"] = "activated",
            ["deactivated"] = "deactivated",
            ["detonated"] = "detonated",
            ["to_detonation"] = "left until detonation",
            ["chaos_insurgency"] = "<color=green>Chaos Insurgency</color>",
            ["nine_tailed_fox"] = "<color=blue>Nine-Tailed Fox</color>"
        };

        public static string BeforeSpawn { get; set; }
        public static string DuringSpawn { get; set; }

        [Config("RespawnTimer.Hints", "A list of hints to show.")]
        public static List<RespawnTimerHint> Hints { get; set; } = new List<RespawnTimerHint>
        {
            new RespawnTimerHint(),
            new RespawnTimerHint()
        };

        public static HashSet<string> TimerHiddenFor { get; } = new HashSet<string>();
        public static List<ReferenceHub> Spectators = new List<ReferenceHub>();

        public static RespawnTimerHint CurHint;
        public static int CurHintInterval;

        public static readonly StringBuilder StringBuilder = new StringBuilder(1024);

        public static string GetText(int? spectators = null)
        {
            if (CurHint is null)
                CurHint = WeightPick.Pick(Hints, x => x.Chance);

            StringBuilder.Clear();
            StringBuilder.Append(
                RespawnManager.Singleton._curSequence != RespawnManager.RespawnSequencePhase.PlayingEntryAnimations
                && RespawnManager.Singleton._curSequence != RespawnManager.RespawnSequencePhase.SpawningSelectedTeam
                    ? BeforeSpawn
                    : DuringSpawn);

            StringBuilder.SetAllProperties(spectators);
            StringBuilder.Replace("{RANDOM_COLOR}", $"#{UnityEngine.Random.Range(0x0, 0xFFFFFF):X6}");
            StringBuilder.Replace('{', '[').Replace('}', ']');

            CurHintInterval++;
            if (CurHintInterval >= HintInterval)
            {
                CurHintInterval = 0;
                CurHint = WeightPick.Pick(Hints, x => x.Chance);
            }

            return StringBuilder.ToString();
        }

        private static void CompileLines()
        {
            if (File.Exists($"{CustomConfigManager.TopPath}/respawn_timer_before.txt"))
                BeforeSpawn = File.ReadAllText($"{CustomConfigManager.TopPath}/respawn_timer_before.txt");

            if (File.Exists($"{CustomConfigManager.TopPath}/respawn_timer_during.txt"))
                DuringSpawn = File.ReadAllText($"{CustomConfigManager.TopPath}/respawn_timer_during.txt");
        }

        private static void OnWaitingForPlayers(object[] args)
        {
            if (string.IsNullOrWhiteSpace(BeforeSpawn) || string.IsNullOrWhiteSpace(DuringSpawn))
                CompileLines();

            if (_timerCoroutine.IsRunning)
                Timing.KillCoroutines(_timerCoroutine);
        }

        private static void OnRoundStart(object[] args)
        {
            _timerCoroutine = Timing.RunCoroutine(TimerCoroutine());
        }

        private static IEnumerator<float> TimerCoroutine()
        {
            do
            {
                yield return Timing.WaitForSeconds(1f);

                Spectators.Clear();
                Spectators.AddRange(ReferenceHub.AllHubs.Where(x => x.Mode is ClientInstanceMode.ReadyClient && !x.IsAlive()));

                string text = GetText(Spectators.Count);

                foreach (ReferenceHub hub in Spectators)
                {
                    if (hub.GetRoleId() == RoleTypeId.Overwatch 
                        && HideTimerForOverwatch || TimerHiddenFor.Contains(hub.characterClassManager.UserId))
                        continue;

                    hub.PersonalHint(text, 1.25f);
                }

            } while (!RoundSummary.singleton._roundEnded);
        }
    }
}