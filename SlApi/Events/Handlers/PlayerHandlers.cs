using System;

using PluginAPI.Enums;
using PluginAPI.Core.Attributes;

using SlApi.Audio;
using SlApi.Dummies;

using PluginAPI.Core;

namespace SlApi.Events.Handlers
{
    public class PlayerHandlers
    {
        public static PlayerHandlers Instance;

        public PlayerHandlers()
        {
            if (Instance != null)
                throw new InvalidOperationException("PlayerHandlers are already active!");

            Instance = this;
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        public void OnPlayerLeft(Player player)
        {
            EventHandlers.TriggerEvents(ServerEventType.PlayerLeft, player);

            try
            {
                var audioPlayer = AudioPlayer.GetPlayer(player.ReferenceHub);

                if (audioPlayer != null)
                {
                    AudioPlayer.Destroy(audioPlayer);
                }
            }
            catch { }

            try
            {
                if (DummyPlayer.TryGetDummyByOwner(player.ReferenceHub, out var dummy))
                {
                    dummy.Destroy();
                }
            }
            catch { }
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        public void OnPlayerJoined(Player player)
        {
            EventHandlers.TriggerEvents(ServerEventType.PlayerJoined, player);
        }
    }
}