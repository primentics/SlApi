using VoiceChat;

namespace SlApi.Features.Voice.Custom
{
    public class CustomVoiceChannel
    {
        public byte Id { get; set; } = 0;

        public VoiceChatChannel[] Channels { get; set; } = new VoiceChatChannel[] { VoiceChatChannel.ScpChat, VoiceChatChannel.Proximity };
        public CustomVoiceFlags[] Flags { get; set; } = new CustomVoiceFlags[] { CustomVoiceFlags.CanHearSelf, CustomVoiceFlags.PerformScpProximityCheck };

        public float MaxScpProximity { get; set; } = 20f;
    }
}