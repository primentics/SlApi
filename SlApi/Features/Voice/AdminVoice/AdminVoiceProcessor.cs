using SlApi.Configs;
using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.AdminVoiceStates;

using System.Collections.Generic;
using System.Linq;

namespace SlApi.Features.Voice.AdminVoice
{
    public static class AdminVoiceProcessor
    {
        public static bool IsGloballyActive { get; set; }

        [Config("AdminVoice.IncludeNorthwoodStaff", "Whether or not to count Northwood staff as normal server staff.")]
        public static bool IncludeNwStaff { get; set; } = true;

        [Config("AdminVoice.KeepChannels", "Whether or not to keep custom made admin voice channels on round restart.")]
        public static bool KeepChannels { get; set; } = true;

        [Config("AdminVoice.PredefinedChannels", "A list of predefined admin-only voice channels.")]
        public static AdminVoiceChannel[] PredefinedChannels { get; set; } = new AdminVoiceChannel[]
        {
            new AdminVoiceChannel()
        };

        public static HashSet<AdminVoiceChannel> CustomChannels { get; } = new HashSet<AdminVoiceChannel>();

        public static void ToggleGlobalState()
        {
            IsGloballyActive = !IsGloballyActive;
        }

        public static bool IsAllowed(ReferenceHub hub)
        {
            if (hub.TryGetState<AdminVoiceState>(out var adminVoiceState) && adminVoiceState.IsAllowed())
                return true;
            else
                return hub.serverRoles.RemoteAdmin || ((hub.serverRoles.RaEverywhere || hub.serverRoles.Staff) && IncludeNwStaff);
        }

        public static bool IsAllowed(ReferenceHub hub, byte channelId)
        {
            if (hub.TryGetState<AdminVoiceState>(out var adminVoiceState) && adminVoiceState.IsAllowed(channelId))
                return true;

            return false;
        }

        public static bool TryGetChannel(byte channelId, out AdminVoiceChannel channel)
        {
            channel = PredefinedChannels.FirstOrDefault(x => x.Id == channelId);

            if (channel != null)
                return true;

            channel = CustomChannels.FirstOrDefault(x => x.Id == channelId);

            return channel != null;
        }

        public static bool TryGetChannelId(ReferenceHub hub, out byte channelId)
        {
            if (!hub.TryGetState<AdminVoiceState>(out var adminVoiceState))
            {
                channelId = 0;
                return false;
            }

            if (adminVoiceState.CurrentChannel.HasValue)
            {
                channelId = adminVoiceState.CurrentChannel.Value;
                return true;
            }

            channelId = 0;
            return false;
        }
    }
}