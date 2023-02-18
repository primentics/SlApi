using MEC;

using Mirror;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using SlApi.Extensions;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Utils.NonAllocLINQ;
using VoiceChat;
using VoiceChat.Networking;

namespace SlApi.Dummies
{
    public class DummyPlayer
    {
        public static HashSet<DummyPlayer> SpawnedDummies { get; } = new HashSet<DummyPlayer>();

        internal static HashSet<uint> MutedGlobal = new HashSet<uint>();

        private ReferenceHub _hub;
        private ReferenceHub _owner;
        private ReferenceHub _followedPlayer;

        private HashSet<uint> _invisibleTo = new HashSet<uint>();
        private HashSet<uint> _mutedTo = new HashSet<uint>();

        public PlayerRoleBase Role { get => _hub.roleManager.CurrentRole; }

        public bool IsInZero { get => _hub.transform.position != Vector3.zero; }

        public RoleTypeId RoleId
        {
            get
            {
                return Role?.RoleTypeId ?? RoleTypeId.None;
            }
            set
            {
                if (value is RoleTypeId.None)
                {
                    Destroy();
                    return;
                }

                _hub.roleManager.ServerSetRole(value, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.AssignInventory);
            }
        }

        public Vector3 Position
        {
            get
            {
                return _hub.GetRealPosition();
            }
            set
            {
                _hub.SetPosition(value);
            }
        }

        public Vector3 Scale
        {
            get
            {
                return _hub.transform.localScale;
            }
            set
            {
                _hub.Resize(value);
            }
        }

        public Quaternion Rotation
        {
            get
            {
                return _hub.GetRealRotation();
            }
            set
            {
                _hub.SetRotation(value);
            }
        }

        public string NickName
        {
            get
            {
                return _hub.nicknameSync.Network_myNickSync;
            }
            set
            {
                _hub.nicknameSync.Network_myNickSync = value;
            }
        }

        public string UserId
        {
            get
            {
                return _hub.characterClassManager.UserId2;
            }
            set
            {
                _hub.characterClassManager.UserId2 = value;
            }
        }

        public int Id
        {
            get
            {
                return _hub.PlayerId;
            }
            set
            {
                _hub.Network_playerId = new RecyclablePlayerId(value);
            }
        }

        public bool IsInvisible { get; set; }

        public VoiceChatChannel VoiceChannel { get; set; } = VoiceChatChannel.None;

        public DummyPlayer(ReferenceHub owner = null)
        {
            _hub = InstantiatePlayer();
            _owner = owner;

            NetworkServer.AddPlayerForConnection(InstantiateConnection(_hub.PlayerId), _hub.gameObject);
        }

        public void Destroy()
        {
            NetworkServer.Destroy(_hub.gameObject);

            _hub = null;
            _followedPlayer = null;
            _owner = null;

            SpawnedDummies.Remove(this);
        }

        public bool IsInvisibleTo(uint plyId)
            => _invisibleTo.Contains(plyId);

        public bool IsFollowing(out ReferenceHub followed)
        {
            followed = _followedPlayer;
            return followed != null;
        }

        public bool IsMutedTo(ReferenceHub hub)
            => _mutedTo.Contains(hub.netId);

        public void Mute(ReferenceHub hub)
            => _mutedTo.Add(hub.netId);

        public void Unmute(ReferenceHub hub)
            => _mutedTo.Remove(hub.netId);

        public static bool IsMutedToGlobal(ReferenceHub hub)
            => MutedGlobal.Contains(hub.netId);

        public static void MuteGlobal(ReferenceHub hub)
            => MutedGlobal.Add(hub.netId);

        public static void UnmuteGlobal(ReferenceHub hub)
            => MutedGlobal.Remove(hub.netId);

        public void Follow(ReferenceHub hub)
            => _followedPlayer = hub;

        public void Speak(byte[] data, int length)
            => Speak(new VoiceMessage(_hub, VoiceChannel, data, length, false));

        public void Speak(VoiceMessage voiceMessage)
        {
            if (VoiceChannel is VoiceChatChannel.None)
                return;

            if (voiceMessage.Speaker != _hub)
                voiceMessage.Speaker = _hub;

            if (voiceMessage.Channel != VoiceChannel)
                voiceMessage.Channel = VoiceChannel;

            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub == _hub)
                    continue;

                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (_owner != null && _owner.netId != hub.netId)
                {
                    if (_mutedTo.Contains(hub.netId))
                        continue;

                    if (MutedGlobal.Contains(hub.netId))
                        continue;
                }

                hub.connectionToClient.Send(voiceMessage);
            }
        }

        public void SpeakTo(ReferenceHub hub, VoiceMessage voiceMessage)
        {
            if (((_owner != null && _owner.netId != hub.netId) && 
                (_mutedTo.Contains(hub.netId) || MutedGlobal.Contains(hub.netId)
                    || VoiceChannel is VoiceChatChannel.None)))
                return;

            if (voiceMessage.Speaker != _hub)
                voiceMessage.Speaker = _hub;

            if (voiceMessage.Channel != VoiceChannel)
                voiceMessage.Channel = VoiceChannel;

            hub.connectionToClient.Send(voiceMessage);
        }

        public void SpeakTo(ReferenceHub hub, byte[] data, int length)
            => SpeakTo(hub, new VoiceMessage(hub, VoiceChannel, data, length, false));

        public void MoveToZero()
            => Position = Vector3.zero;

        public static ReferenceHub InstantiatePlayer()
            => Object.Instantiate(NetworkManager.singleton.playerPrefab).GetComponent<ReferenceHub>();

        public static DummyConnection InstantiateConnection(int id)
            => new DummyConnection(id);

        public static bool TryGetDummy(ReferenceHub hub, out DummyPlayer dummy)
        {
            dummy = SpawnedDummies.FirstOrDefault(x => x._hub == hub);

            return dummy != null;
        }

        public static bool TryGetDummies(ReferenceHub owner, out HashSet<DummyPlayer> dummies)
        {
            dummies = new HashSet<DummyPlayer>();

            foreach (var dummy in SpawnedDummies)
            {
                if (dummy._owner != null && dummy._owner != owner)
                    dummies.Add(dummy);
            }

            return dummies.Count > 0;
        }

        public static void DestroyAll()
        {
            foreach (var dummy in SpawnedDummies)
            {
                dummy.Destroy();
            }

            SpawnedDummies.Clear();
        }
    }
}
