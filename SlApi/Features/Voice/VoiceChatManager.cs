using HarmonyLib;

using Mirror;

using PlayerRoles.Voice;
using PluginAPI.Core;
using SlApi.Configs;
using SlApi.Features.PlayerStates;
using SlApi.Features.Voice.AdminVoice;
using SlApi.Features.Voice.Custom;
using System;


using VoiceChat;
using VoiceChat.Networking;

namespace SlApi.Voice
{
    [HarmonyPatch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))]
    public static class VoiceChatManager
    {
        [Config("VoiceChat.DisabledChannels", "A list of disabled voice chat channels.")]
        public static VoiceChatChannel[] DisabledChannels { get; set; } = Array.Empty<VoiceChatChannel>();

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

            if (msg.Channel is VoiceChatChannel.Mimicry 
                && msg.Speaker.roleManager.CurrentRole.RoleTypeId == PlayerRoles.RoleTypeId.Scp939)
                return true;

            if (AdminVoiceProcessor.IsGloballyActive)
            {
                if (AdminVoiceProcessor.IsAllowed(msg.Speaker))
                {
                    msg.Channel = VoiceChatChannel.Intercom;
                    vcRole.VoiceModule.CurrentChannel = VoiceChatChannel.Intercom;

                    foreach (var hub in ReferenceHub.AllHubs)
                    {
                        if (hub.Mode != ClientInstanceMode.ReadyClient) continue;
                        if (hub.netId == msg.Speaker.netId) continue;

                        hub.connectionToClient.Send(msg);
                    }
                }

                return false;
            }

            if (AdminVoiceProcessor.TryGetChannelId(msg.Speaker, out byte cChannelId)
                && AdminVoiceProcessor.TryGetChannel(cChannelId, out var vcChannel))
            {
                msg.Channel = vcChannel.VoiceChannel;
                vcRole.VoiceModule.CurrentChannel = vcChannel.VoiceChannel;

                foreach (var hub in ReferenceHub.AllHubs)
                {
                    if (hub.Mode != ClientInstanceMode.ReadyClient) continue;
                    if (hub.netId == msg.Speaker.netId) continue;
                    if (!AdminVoiceProcessor.IsAllowed(hub, cChannelId)) continue;

                    hub.connectionToClient.Send(msg);
                }

                return false;
            }

            if (msg.Speaker.TryGetState<CustomVoiceState>(out var voice) 
                && voice.IsEnabled())
            {
                vcRole.VoiceModule.CurrentChannel = voice.VcChannel;
                msg.Channel = voice.VcChannel;

                foreach (var hub in ReferenceHub.AllHubs)
                {
                    if (!voice.CanBeHeardBy(hub)) continue;

                    hub.connectionToClient.Send(msg);
                }

                return false;
            }

            VoiceChatChannel voiceChatChannel = vcRole.VoiceModule.ValidateSend(msg.Channel);

            if (voiceChatChannel == VoiceChatChannel.None)
                return false;

            vcRole.VoiceModule.CurrentChannel = voiceChatChannel;

            foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
            {
                IVoiceRole voiceRole2;
                if ((voiceRole2 = (referenceHub.roleManager.CurrentRole as IVoiceRole)) != null)
                {
                    VoiceChatChannel voiceChatChannel2 = voiceRole2.VoiceModule.ValidateReceive(msg.Speaker, voiceChatChannel);
                    if (voiceChatChannel2 != VoiceChatChannel.None)
                    {
                        msg.Channel = voiceChatChannel2;
                        referenceHub.connectionToClient.Send(msg, 0);
                    }
                }
            }

            return false;
        }
    }
}