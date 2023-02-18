using MEC;

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

namespace SlApi.Features.RespawnTimer
{
    public static class RespawnTimerController
    {
        private static CoroutineHandle _timerCoroutine;

        static RespawnTimerController()
        {
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.WaitingForPlayers, OnWaitingForPlayers));
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.RoundStart, OnRoundStart));
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

        [Config("RespawnTimer.NtfString", "The Nine-Tailed Fox display name.")]
        public static string NtfString { get; set; } = "<color=blue>Nine-Tailed Fox</color>";

        [Config("RespawnTimer.CiString", "The Chaos Insurgency display name.")]
        public static string CiString { get; set; } = "<color=green>Chaos Insurgency</color>";

        public static string BeforeSpawn { get; set; }
        public static string DuringSpawn { get; set; } 


        [Config("RespawnTimer.Hints", "A list of hints to show.")]
        public static string[] Hints { get; set; } = new string[]
        {
            "You <b>will</b> die in this game many times.",
            "Don't throw grenades into elevators. It's not funny at all."
        };

        public static HashSet<string> TimerHiddenFor { get; } = new HashSet<string>();

        public static int CurHintIndex;
        public static int CurHintInterval;

        public static readonly StringBuilder StringBuilder = new StringBuilder(1024);

        public static string GetText(int? spectators = null)
        {
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
                CurHintIndex++;

                if (CurHintIndex >= Hints.Length)
                    CurHintIndex = 0;
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
                if (!Enabled)
                    continue;

                yield return Timing.WaitForSeconds(1f);

                var spectators = ReferenceHub.AllHubs.Where(x => x.Mode is ClientInstanceMode.ReadyClient
                                                    && !x.isLocalPlayer
                                                    && !x.IsAlive());

                string text = GetText(spectators.Count());

                foreach (var spectator in spectators)
                {
                    if (spectator.roleManager.CurrentRole.RoleTypeId is RoleTypeId.Overwatch && !HideTimerForOverwatch)
                        continue;

                    if (TimerHiddenFor.Contains(spectator.characterClassManager.UserId))
                        continue;

                    spectator.PersonalHint(text, 1.25f);
                }

            } while (!RoundSummary.singleton._roundEnded);
        }
    }
}
