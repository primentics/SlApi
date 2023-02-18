using NorthwoodLib.Pools;

using PlayerRoles;

using PluginAPI.Core;

using SlApi.Configs;
using SlApi.Events;
using SlApi.Events.CustomHandlers;
using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.AdminVoiceStates;
using SlApi.Features.Voice.Custom;

using System.Collections.Generic;
using System.Linq;

namespace SlApi.Features.Voice
{
    public static class CustomVoiceProcessor
    {
        static CustomVoiceProcessor()
        {
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.PlayerJoined, OnPlayerJoined));
        }

        [Config("CustomVoice.PairedChannels", "A list of roles paired to their channel IDs.")]
        public static Dictionary<RoleTypeId, byte> PairedChannels { get; set; } = new Dictionary<RoleTypeId, byte>()
        {
            [RoleTypeId.Scp049] = 0
        };

        [Config("CustomVoice.PredefinedChannels", "A list of predefined custom voice chat channels.")]
        public static HashSet<CustomVoiceChannel> PredefinedChannels { get; set; } = new HashSet<CustomVoiceChannel>() { new CustomVoiceChannel() };

        public static bool TryGetChannelById(byte channelId, out CustomVoiceChannel channel)
        {
            channel = PredefinedChannels.FirstOrDefault(x => x.Id == channelId);

            return channel != null;
        }

        public static bool ValidateConfigs()
        {
            HashSet<int> toRemove = HashSetPool<int>.Shared.Rent();

            for (int i = 0; i < PredefinedChannels.Count; i++) 
            {
                var element = PredefinedChannels.ElementAt(i);

                if (PredefinedChannels.Count(x => x.Id == element.Id) > 1 
                    && (PredefinedChannels.Count - toRemove.Count(x => PredefinedChannels.ElementAt(x).Id == element.Id)) > 0)
                {
                    toRemove.Add(i);
                    Logger.Warn($"[Custom Voice Processor] Detected a channel with duplicate ID: {element.Id}");
                }
            }

            if (toRemove.Count > 0)
            {
                foreach (var index in toRemove)
                    PredefinedChannels.Remove(PredefinedChannels.ElementAt(index));

                toRemove.Clear();

                for (int i = 0; i < PairedChannels.Count; i++)
                {
                    var pair = PairedChannels.ElementAt(i);

                    if (!TryGetChannelById(pair.Value, out _))
                    {
                        Logger.Warn($"[Custom Voice Processor] Missing voice channel with ID {pair.Value} ({pair.Key})");

                        toRemove.Add(i);
                    }
                }

                foreach (var index in toRemove)
                    PairedChannels.Remove(PairedChannels.ElementAt(index).Key);

                return false;
            }

            HashSetPool<int>.Shared.Return(toRemove);

            return true;
        }

        private static void OnPlayerJoined(object[] args)
        {
            var hub = (args[0] as Player).ReferenceHub;

            hub.TryAddState(new CustomVoiceState(hub));
            hub.TryAddState(new AdminVoiceState(hub));
        }
    }
}