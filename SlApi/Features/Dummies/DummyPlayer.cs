using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using MapGeneration;
using Mirror;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PluginAPI.Core;
using SlApi.Extensions;

using System.Collections.Generic;
using System.Linq;
using System;

using UnityEngine;

using Utils.NonAllocLINQ;

using VoiceChat;
using VoiceChat.Networking;

namespace SlApi.Dummies
{
    public class DummyPlayer
    {
        internal static byte Ids = 0;

        public static HashSet<DummyPlayer> SpawnedDummies { get; } = new HashSet<DummyPlayer>();

        internal static HashSet<uint> MutedGlobal = new HashSet<uint>();

        internal ReferenceHub _hub;
        private ReferenceHub _owner;
        private ReferenceHub _followedPlayer;

        private HashSet<uint> _invisibleTo = new HashSet<uint>();
        private HashSet<uint> _mutedTo = new HashSet<uint>();

        public PlayerRoleBase Role { get => _hub.roleManager.CurrentRole; }

        public byte DummyId { get; }
        public bool DestroyDoors { get; set; }
        public bool IsInZero { get => _hub.transform.position != Vector3.zero; }

        public float WalkDistance { get; set; } = 5f;
        public float RunDistance { get; set; } = 8f;
        public float TeleportDistance { get; set; } = 30f;

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
                return _hub.gameObject.transform.position;
            }
            set
            {
                _hub.TryOverridePosition(value, Vector3.zero);
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

        public ItemType CurItem
        {
            get
            {
                return _hub.inventory.CurInstance?.ItemTypeId ?? ItemType.None;
            }
            set
            {
                if (value is ItemType.None)
                {
                    _hub.inventory.NetworkCurItem = new InventorySystem.Items.ItemIdentifier(ItemType.None, 0);
                    return;
                }
               
                if (!_hub.inventory.UserInventory.Items.Any(x => x.Value.ItemTypeId == value))
                {
                    foreach (var item in _hub.inventory.UserInventory.Items)
                    {
                        _hub.inventory.ServerRemoveItem(item.Key, item.Value.PickupDropModel);
                    }

                    _hub.inventory.ServerAddItem(value);
                }

                _hub.inventory.ServerSelectItem(_hub.inventory.UserInventory.Items.First(x => x.Value.ItemTypeId == value).Key);
            }
        }

        public bool IsInvisible { get; set; }

        public VoiceChatChannel VoiceChannel { get; set; } = VoiceChatChannel.None;

        public DummyPlayer(ReferenceHub owner = null)
        {
            _hub = InstantiatePlayer();
            _owner = owner;

            NetworkServer.AddPlayerForConnection(InstantiateConnection(_hub.PlayerId), _hub.gameObject);

            Ids++;

            DummyId = Ids;

            StaticUnityMethods.OnUpdate += OnUpdate;

            SpawnedDummies.Add(this);

            RoleId = _owner.GetRoleId();
            Position = _owner.GetRealPosition();
            Rotation = _owner.GetRealRotation();

            try { Scale = _owner.transform.localScale; } catch { }

            UserId = $"Dummy {DummyId}";
            NickName = $"Dummy ({DummyId})";
        }

        public void Destroy()
        {
            StaticUnityMethods.OnUpdate -= OnUpdate;

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

        private void OnUpdate()
        {
            if (_followedPlayer is null || !_followedPlayer.IsAlive())
                return;

            if (RoleId is RoleTypeId.Scp079)
            {
                UpdateCamera();
                return;
            }

            if (!(_hub.roleManager.CurrentRole is IFpcRole fpcRole) || fpcRole.FpcModule is null)
                return;

            var pos = _followedPlayer.gameObject.transform.position - Position;
            var mouseLook = fpcRole.FpcModule.MouseLook;
            var rot = Quaternion.LookRotation(pos, Vector3.up / 2f);

            mouseLook.CurrentHorizontal = rot.eulerAngles.y;
            mouseLook.CurrentVertical = rot.eulerAngles.x;

            _hub.PlayerCameraReference.rotation = rot;

            var distance = Vector3.Distance(_followedPlayer.gameObject.transform.position, Position);
            if (distance >= 9f)
            {
                var dir = (_followedPlayer.gameObject.transform.position - Position).normalized;
                var velocity = dir * fpcRole.FpcModule.SprintSpeed;

                fpcRole.FpcModule.CharController.Move(velocity * Time.deltaTime);
                fpcRole.FpcModule.CurrentMovementState = PlayerMovementState.Sprinting;
                fpcRole.FpcModule.SyncMovementState = PlayerMovementState.Sprinting;
                fpcRole.FpcModule.UpdateMovement();
                fpcRole.FpcModule.StateProcessor?.UpdateMovementState(PlayerMovementState.Sprinting);
            }
            else if (distance >= 6f)
            {
                var dir = (_followedPlayer.gameObject.transform.position - Position).normalized;
                var velocity = dir * fpcRole.FpcModule.WalkSpeed;

                fpcRole.FpcModule.CharController.Move(velocity * Time.deltaTime);
                fpcRole.FpcModule.CurrentMovementState = PlayerMovementState.Walking;
                fpcRole.FpcModule.SyncMovementState = PlayerMovementState.Walking;
                fpcRole.FpcModule.UpdateMovement();
                fpcRole.FpcModule.StateProcessor?.UpdateMovementState(PlayerMovementState.Walking);
            }
            else if (distance >= TeleportDistance)
            {
                Position = _followedPlayer.GetRealPosition();
            }
            else
            {
                fpcRole.FpcModule.CurrentMovementState = PlayerMovementState.Sneaking;
                fpcRole.FpcModule.SyncMovementState = PlayerMovementState.Sneaking;
                fpcRole.FpcModule.UpdateMovement();
                fpcRole.FpcModule.StateProcessor?.UpdateMovementState(PlayerMovementState.Sneaking);
            }

            UpdateMove();
        }

        private void UpdateCamera()
        {
            if (!(_hub.roleManager.CurrentRole is Scp079Role scp079))
                return;

            var targetRoom = RoomIdUtils.RoomAtPosition(_followedPlayer.transform.gameObject.transform.position);

            if (targetRoom is null)
                return;

            var cams = Scp079InteractableBase.AllInstances.Where(x => x is Scp079Camera &&
                        x.Room != null &&
                        x.Room.GetInstanceID() == targetRoom.GetInstanceID());

            if (!cams.Any())
                return;

            Scp079InteractableBase targetCam = null;

            if (cams.Count() > 1)
                targetCam = cams.OrderBy(x => Vector3.Distance(x.Position, Position)).First();
            else
                targetCam = cams.First();

            if (targetCam is null)
                return;

            var cam = targetCam as Scp079Camera;
            if (ReferenceHub.AllHubs.Any(x => x.Mode is ClientInstanceMode.ReadyClient
                && x.netId != _hub.netId
                && x.roleManager.CurrentRole is Scp079Role ply
                && ply.CurrentCamera != null
                && ply.CurrentCamera.SyncId == targetCam.SyncId))
                return;

            try
            {
                var pos = _followedPlayer.gameObject.transform.position - cam.CameraPosition;
                var rot = Quaternion.LookRotation(pos, cam.transform.up);

                if (scp079.CurrentCamera is null || scp079.CurrentCamera.Label != cam.Label
                    || scp079.CurrentCamera.Room.GetInstanceID() != targetCam.Room.GetInstanceID())
                {
                    float num = scp079._curCamSync.GetSwitchCost(cam);
                    if (num > scp079._curCamSync._auxManager.CurrentAux)
                    {
                        scp079._curCamSync._errorCode = Scp079HudTranslation.NotEnoughAux;
                        scp079._curCamSync.ServerSendRpc(true);
                        return;
                    }

                    if (scp079._curCamSync._lostSignalHandler.Lost)
                    {
                        scp079._curCamSync._errorCode = Scp079HudTranslation.SignalLost;
                        scp079._curCamSync.ServerSendRpc(true);
                        return;
                    }

                    scp079._curCamSync._auxManager.CurrentAux -= num;
                    scp079._curCamSync.CurrentCamera = cam;
                    scp079._curCamSync.ServerSendRpc(true);
                }

                cam._cameraAnchor.rotation = rot;
                cam.VerticalAxis.TargetValue = rot.eulerAngles.x;
                cam.HorizontalAxis.TargetValue = rot.eulerAngles.y;
                cam.RollRotation = rot.eulerAngles.z;
                cam.VerticalAxis.OnValueChanged(rot.eulerAngles.x, cam);
                cam.HorizontalAxis.OnValueChanged(rot.eulerAngles.y, cam);
                cam.HorizontalAxis.Update(cam);
                cam.VerticalAxis.Update(cam);
                scp079._curCamSync.ServerSendRpc(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        private void UpdateMove()
        {
            if (!Physics.Raycast(_hub.PlayerCameraReference.position, _hub.PlayerCameraReference.forward, 
                out var hit, 10f, Physics.AllLayers))
                return;

            var door = hit.transform.GetComponentInParent<DoorVariant>();

            if (door != null && !door.NetworkTargetState)
            {
                if (DestroyDoors && door is BreakableDoor breakable)
                {
                    breakable.Network_destroyed = true;
                    return;
                }

                door.ServerInteract(_hub, 0);
            }
        }

        public static ReferenceHub InstantiatePlayer()
            => UnityEngine.Object.Instantiate(NetworkManager.singleton.playerPrefab).GetComponent<ReferenceHub>();

        public static DummyConnection InstantiateConnection(int id)
            => new DummyConnection(id);

        public static bool IsDummy(ReferenceHub hub)
            => TryGetDummy(hub, out _);

        public static bool TryGetDummy(ReferenceHub hub, out DummyPlayer dummy)
        {
            dummy = SpawnedDummies.FirstOrDefault(x => x._hub == hub);
            return dummy != null;
        }

        public static bool TryGetDummy(byte id, out DummyPlayer dummy)
        {
            dummy = SpawnedDummies.FirstOrDefault(x => x.DummyId == id);
            return dummy != null;
        }

        public static bool TryGetDummies(ReferenceHub owner, out HashSet<DummyPlayer> dummies)
        {
            dummies = new HashSet<DummyPlayer>();

            foreach (var dummy in SpawnedDummies)
            {
                if (dummy._owner != null && dummy._owner == owner)
                    dummies.Add(dummy);
            }

            return dummies.Count > 0;
        }

        public static void DestroyAll()
        {
            foreach (var dummy in SpawnedDummies)
                dummy.Destroy();

            SpawnedDummies.Clear();
        }
    }
}
