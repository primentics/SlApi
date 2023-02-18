using PlayerRoles;
using PlayerRoles.Spectating;
using PlayerRoles.Voice;

using SlApi.Extensions;
using SlApi.Features.PlayerStates;

using UnityEngine;

using VoiceChat;

namespace SlApi.Features.Voice.Custom
{
    public class CustomVoiceState : PlayerStateBase
    {
        public CustomVoiceState(ReferenceHub target) : base(target)
        {

        }

        public CustomVoiceChannel Channel { get; set; }
        public VoiceChatChannel VcChannel { get; set; }

        public byte KeyState { get; set; } = 0;
        public bool DefaultState { get; set; } = false;

        public bool CanBeHeardBy(ReferenceHub receiver)
        {
            if (receiver.netId == Target.netId
                && !Channel.Flags.Contains(CustomVoiceFlags.CanHearSelf))
            {
                return false;
            }

            if (VcChannel is VoiceChatChannel.ScpChat && !receiver.IsSCP())
            {
                return false;
            }

            if (receiver.roleManager.CurrentRole.RoleTypeId is RoleTypeId.Spectator && Target.IsSpectatedBy(receiver))
            {
                return true;
            }

            if (Target.IsSCP() && !receiver.IsSCP() && VcChannel is VoiceChatChannel.Proximity)
            {
                if (Channel.Flags.Contains(CustomVoiceFlags.PerformScpProximityCheck))
                {
                    if (Vector3.Distance(Target.GetRealPosition(), receiver.GetRealPosition()) > Channel.MaxScpProximity)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool IsEnabled()
            => 
               Channel != null 
            && VcChannel != VoiceChatChannel.None
            && !DefaultState;

        public void OnKeyUsed()
        {
            if (Channel is null)
                return;

            if (!Channel.Flags.Contains(CustomVoiceFlags.AllowSwitchByKey))
                return;

            KeyState++;

            if (KeyState >= Channel.Channels.Length)
                KeyState = 0;

            VcChannel = Channel.Channels[KeyState];

            Target.ConsoleMessage($"[CustomVoice] Switched channel: {VcChannel} ({KeyState})");
        }

        public override void OnAdded()
            => OnRoleChanged();

        public override void OnRoleChanged()
        {
            if (!(Target.roleManager.CurrentRole is IVoiceRole))
            {
                Channel = null;
                DefaultState = true;
                KeyState = 0;
                Target.ConsoleMessage($"[CustomVoice] Switched to default state - not a voice role.");
                return;
            }

            if (CustomVoiceProcessor.PairedChannels.TryGetValue(Target.roleManager.CurrentRole.RoleTypeId, out var channelId) 
                && CustomVoiceProcessor.TryGetChannelById(channelId, out var channel))
            {
                Channel = channel;
                KeyState = 0;
                VcChannel = channel.Channels[KeyState];
                DefaultState = false;
                Target.ConsoleMessage($"[CustomVoice] Switched to channel: {VcChannel}");
            }
            else
            {
                Channel = null;
                DefaultState = true;
                KeyState = 0;
                Target.ConsoleMessage($"[CustomVoice] Switched to default state - no custom channels.");
            }
        }
    }
}