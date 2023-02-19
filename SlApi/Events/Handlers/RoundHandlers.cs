using System;

using Mirror;

using PluginAPI.Enums;
using PluginAPI.Core.Attributes;

using SlApi.Commands;
using SlApi.Dummies;

using PluginAPI.Core;
using SlApi.Features.Audio;

namespace SlApi.Events.Handlers
{
    public class RoundHandlers
    {
        public static RoundHandlers Instance;

        public RoundHandlers()
        {
            if (Instance != null)
                throw new InvalidOperationException("RoundHandlers are already active!");

            Instance = this;
        }

        [PluginEvent(ServerEventType.WarheadStop)]
        public void OnWarheadStopped(Player player)
        {
            EventHandlers.TriggerEvents(ServerEventType.WarheadStop, player);
        }

        [PluginEvent(ServerEventType.RoundStart)]
        public void OnRoundStarted()
        {
            EventHandlers.TriggerEvents(ServerEventType.RoundStart);
        }

        [PluginEvent(ServerEventType.RoundEnd)]
        public void OnRoundEnded(RoundSummary.LeadingTeam leadingTeam)
        {
            EventHandlers.TriggerEvents(ServerEventType.RoundEnd, leadingTeam);
        }

        [PluginEvent(ServerEventType.RoundRestart)]
        public void OnRoundRestarting()
        {
            EventHandlers.TriggerEvents(ServerEventType.RoundRestart);

            DestroySpawnables();

            try { DummyPlayer.DestroyAll(); } catch { }
        }

        [PluginEvent(ServerEventType.WaitingForPlayers)]
        public void OnRoundWaitingForPlayers()
        {
            EventHandlers.TriggerEvents(ServerEventType.WaitingForPlayers);
        }

        private void DestroySpawnables()
        {
            if (SpawnableCommand.Spawnables.Count <= 0)
                return;

            foreach (var obj in SpawnableCommand.Spawnables)
            {
                NetworkServer.Destroy(obj);
            }

            SpawnableCommand.Spawnables.Clear();
        }
    }
}
