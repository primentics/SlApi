using SlApi.Configs;

using System;

using VoiceChat;

namespace SlApi.Voice
{
    public static class VoiceChatManager
    {
        [Config("VoiceChat.DisabledChannels", "A list of disabled voice chat channels.")]
        public static VoiceChatChannel[] DisabledChannels { get; set; } = Array.Empty<VoiceChatChannel>();
    }
}