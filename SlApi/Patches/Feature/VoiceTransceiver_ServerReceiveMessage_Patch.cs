using HarmonyLib;

using Mirror;

using PlayerRoles.Voice;

using VoiceChat;
using VoiceChat.Networking;

using SlApi.Features.PlayerStates;
using SlApi.Features.Voice.Custom;
using SlApi.Features.Voice.AdminVoice;
using SlApi.Features.PlayerStates.AdminVoiceStates;

namespace SlApi.Patches.Feature
{
    [HarmonyPatch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))]
    public static class VoiceTransceiver_ServerReceiveMessage_Patch
    {
        public static bool Prefix(NetworkConnection conn, VoiceMessage msg)
        {
            if (msg.SpeakerNull || msg.Speaker.netId != conn.identity.netId)
                return false;

            if (!(msg.Speaker.roleManager.CurrentRole is IVoiceRole vcRole) || vcRole is null || vcRole.VoiceModule is null)
                return false;

            if (!vcRole.VoiceModule.CheckRateLimit())
                return false;

            var muteFlags = VoiceChatMutes.GetFlags(msg.Speaker);

            if (muteFlags is VcMuteFlags.GlobalRegular || muteFlags is VcMuteFlags.LocalRegular)
                return false;

            if (AdminVoiceProcessor.IsGloballyActive)
            {
                if (AdminVoiceProcessor.IsAllowed(msg.Speaker))
                {
                    msg.Channel = VoiceChatChannel.Intercom;

                    foreach (var hub in ReferenceHub.AllHubs)
                    {
                        if (hub.Mode != ClientInstanceMode.ReadyClient)
                            continue;

                        if (hub.netId == msg.Speaker.netId)
                            continue;

                        hub.connectionToClient.Send(msg);
                    }
                }

                return false;
            }

            if (AdminVoiceProcessor.TryGetChannelId(msg.Speaker, out byte cChannelId) && AdminVoiceProcessor.TryGetChannel(cChannelId, out var vcChannel))
            {
                msg.Channel = vcChannel.VoiceChannel;

                foreach (var hub in ReferenceHub.AllHubs)
                {
                    if (hub.Mode != ClientInstanceMode.ReadyClient)
                        continue;

                    if (hub.netId == msg.Speaker.netId)
                        continue;

                    if (!AdminVoiceProcessor.IsAllowed(hub, cChannelId))
                        continue;

                    hub.connectionToClient.Send(msg);
                }

                return false;
            }

            if (msg.Speaker.TryGetState<CustomVoiceState>(out var voice) && voice.IsEnabled())
            {
                vcRole.VoiceModule.CurrentChannel = voice.VcChannel;

                foreach (var hub in ReferenceHub.AllHubs)
                {
                    if (voice.CanBeHeardBy(hub))
                    {
                        msg.Channel = voice.VcChannel;

                        hub.connectionToClient.Send(msg);
                    }
                }
            }
            else
            {
                var channel = vcRole.VoiceModule.ValidateSend(msg.Channel);

                if (channel is VoiceChatChannel.None)
                    return false;

                vcRole.VoiceModule.CurrentChannel = channel;

                foreach (var hub in ReferenceHub.AllHubs)
                {
                    if (hub.Mode != ClientInstanceMode.ReadyClient)
                        continue;

                    if (!(hub.roleManager.CurrentRole is IVoiceRole vc) || vc is null || vc.VoiceModule is null)
                        return false;

                    channel = vc.VoiceModule.ValidateReceive(msg.Speaker, channel);

                    if (channel != VoiceChatChannel.None)
                    {
                        msg.Channel = channel;

                        hub.connectionToClient.Send(msg);
                    }
                }
            }

            return false;
        }
    }
}