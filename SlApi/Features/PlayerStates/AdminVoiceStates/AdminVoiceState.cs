using SlApi.Extensions;
using SlApi.Features.Voice.AdminVoice;

namespace SlApi.Features.PlayerStates.AdminVoiceStates
{
    public class AdminVoiceState : PlayerStateBase
    {
        public AdminVoiceFlags Flags { get; set; }

        public byte? CurrentChannel;

        public AdminVoiceState(ReferenceHub target) : base(target)
        {
            Flags = (target.serverRoles.RemoteAdmin 
                || ((target.serverRoles.RaEverywhere || target.serverRoles.Staff) && AdminVoiceProcessor.IncludeNwStaff)) 
                ? AdminVoiceFlags.GlobalAllowed 
                : AdminVoiceFlags.None;

            CurrentChannel = null;
        }

        public void AllowToChannel(byte channelId)
        {
            CurrentChannel = channelId;

            if (AdminVoiceProcessor.TryGetChannel(channelId, out var channel))
                Target.ConsoleMessage($"[AdminVoice] Joined channel {channel.Name} ({channel.Id}).");
            else
                Target.ConsoleMessage($"[AdminVoice] Joined channel with ID {channelId}", "red");
        }

        public void DeclineFromChannel(byte channelId)
        {
            CurrentChannel = null;

            if (AdminVoiceProcessor.TryGetChannel(channelId, out var channel))
                Target.ConsoleMessage($"[AdminVoice] Disconnected from channel {channel.Name} ({channel.Id}).");
            else
                Target.ConsoleMessage($"[AdminVoice] Disconnected from channel with ID {channelId}", "red");
        }

        public bool IsAllowed()
            => Flags == AdminVoiceFlags.GlobalAllowed;

        public bool IsAllowed(byte channelId)
            => CurrentChannel.HasValue && CurrentChannel.Value == channelId;
    }
}