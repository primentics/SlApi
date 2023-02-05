using VoiceChat;

namespace SlApi.Features.Voice.AdminVoice
{
    public class AdminVoiceChannel
    {
        public byte Id { get; set; } = 0;

        public string Name { get; set; } = "Example";

        public VoiceChatChannel VoiceChannel { get; set; } = VoiceChatChannel.Intercom;
    }
}