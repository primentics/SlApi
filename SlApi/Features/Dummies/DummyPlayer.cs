using MEC;

using Mirror;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace SlApi.Dummies
{
    public class DummyPlayer
    {
        public static int IdIndex = 500;
        public static HashSet<DummyPlayer> Dummies = new HashSet<DummyPlayer>();

        public static GameObject PlayerPrefab { get => NetworkManager.singleton.playerPrefab; }

        private Vector3 _lastPos;
        private Vector3 _lastScale;
        private Quaternion _lastRot;

        public ReferenceHub Owner { get; set; }
        public ReferenceHub Hub { get; }
        public NetworkConnection Connection { get; }
        public GameObject Player { get; }

        public string Nick { get => Hub.nicknameSync.Network_myNickSync; set => Hub.nicknameSync.SetNick(value); }

        public bool InWorld { get; private set; }
        public ReferenceHub Follow { get; set; }

        public RoleTypeId RoleId
        {
            get => Hub.roleManager.CurrentRole?.RoleTypeId ?? RoleTypeId.None;
            set
            {
                if (value == RoleTypeId.None)
                    Despawn();
                else
                {
                    if (value == RoleId)
                        return;

                    Hub.roleManager.ServerSetRole(value, RoleChangeReason.RemoteAdmin);
                }
            }
        }

        public PlayerRoleBase Role
        {
            get => Hub.roleManager.CurrentRole;
            set
            {
                if (value == null)
                    Despawn();
                else
                    RoleId = value.RoleTypeId;
            }
        }

        public Vector3 Position
        {
            get => InWorld ? (Hub.roleManager.CurrentRole is IFpcRole fpcRole ? fpcRole.FpcModule.Position : Hub.transform.position) : _lastPos;
            set
            {
                if (!(Role is IFpcRole fpcRole))
                    return;

                fpcRole.FpcModule.ServerOverridePosition(Position, Rotation.eulerAngles);
            }
        }

        public Vector3 Scale
        {
            get => InWorld ? (Hub.roleManager.CurrentRole is IFpcRole fpcRole ? fpcRole.FpcModule.CharacterModelInstance.transform.localScale : Hub.transform.localScale) : _lastScale;
            set
            {
                _lastScale = value;
            }
        }

        public Quaternion Rotation
        {
            get => InWorld ? (Hub.roleManager.CurrentRole is IFpcRole fpcRole ? fpcRole.FpcModule.MouseLook.TargetHubRotation : Hub.transform.rotation) : _lastRot;
            set
            {
                if (!(Role is IFpcRole fpcRole))
                    return;

                fpcRole.FpcModule.MouseLook.CurrentHorizontal = value.x;
                fpcRole.FpcModule.MouseLook.CurrentVertical = value.y;
                fpcRole.FpcModule.MouseLook.UpdateRotation();
            }
        }

        public DummyPlayer(RoleTypeId role)
        {
            Player = Object.Instantiate(PlayerPrefab);
            Connection = new DummyNetworkConnecton(IdIndex++);
            Hub = Player.GetComponent<ReferenceHub>();

            NetworkServer.AddPlayerForConnection(Connection, Player);

            Hub.characterClassManager._privUserId =  $"dummy-{Hub.GetInstanceID()}-{IdIndex}";
            Hub.characterClassManager.NetworkSyncedUserId = "ID_Host";
            Hub.Network_playerId = new RecyclablePlayerId(IdIndex);

            Timing.CallDelayed(0.5f, () =>
            {
                RoleId = role;
            });

            Dummies.Add(this);

            StaticUnityMethods.OnFixedUpdate += OnFixedUpdate;
        }

        public void Destroy()
        {
            StaticUnityMethods.OnFixedUpdate -= OnFixedUpdate;

            NetworkServer.Destroy(Player);

            Dummies.Remove(this);
        }

        public void Spawn(Vector3 pos, Vector3 scale, Quaternion rot)
        {
            _lastPos = pos;
            _lastRot = rot;
            _lastScale = scale;

            InWorld = true;

            Player.transform.rotation = rot;
            Player.transform.position = pos;
            Player.transform.localPosition = pos;
            Player.transform.localRotation = rot;
            Player.transform.localScale = scale;

            NetworkServer.Spawn(Player);

            Position = pos;
            Rotation = rot;
        }

        public void Despawn()
        {
            _lastRot = Rotation;
            _lastPos = Position;
            _lastScale = Scale;

            InWorld = false;

            NetworkServer.UnSpawn(Player);
        }

        private void OnFixedUpdate()
        {
            if (Follow != null && Follow.IsAlive() && (Follow.roleManager.CurrentRole is IFpcRole fpcRole))
            {
                Position = Follow.transform.TransformPoint(new Vector3(0f, 0.5f, -5f));
                
                var rot = Quaternion.LookRotation(Hub.transform.forward, Hub.transform.up);

                rot.SetLookRotation(fpcRole.FpcModule.Position);

                Rotation = rot;
            }
        }

        public static bool TryGetDummyByOwner(ReferenceHub owner, out DummyPlayer dummy)
        {
            dummy = GetDummyByOwner(owner);

            return dummy != null;
        }

        public static DummyPlayer GetDummyByOwner(ReferenceHub owner)
        {
            return Dummies.FirstOrDefault(x => x.Owner != null && x.Owner.netId == owner.netId);
        }

        public static DummyPlayer GetDummy(ReferenceHub hub)
        {
            var dummy = Dummies.FirstOrDefault(x => x.Hub.GetInstanceID() == hub.GetInstanceID());

            return dummy;
        }

        public static DummyPlayer GetOrCreateDummy(ReferenceHub hub, bool spawn = false)
        {
            if (!TryGetDummy(hub, out var dummy))
                dummy = new DummyPlayer(hub.GetRoleId());

            if (spawn)
            {
                Timing.CallDelayed(0.5f, () =>
                {
                    if (hub.roleManager.CurrentRole is IFpcRole fpcRole)
                        dummy.Spawn(
                            fpcRole.FpcModule.Position,
                            fpcRole.FpcModule.CharacterModelInstance.transform.localScale,
                            fpcRole.FpcModule.MouseLook.TargetHubRotation);
                    else
                        dummy.Spawn(
                            hub.PlayerCameraReference.position, 
                            hub.transform.localScale, 
                            hub.PlayerCameraReference.rotation);
                });
            }

            return dummy;
        }

        public static bool TryGetDummy(ReferenceHub hub, out DummyPlayer dummy)
        {
            dummy = GetDummy(hub);

            return dummy != null;
        }

        public static void DestroyAll()
        {
            IdIndex = 500;

            foreach (var dummy in Dummies)
                dummy.Destroy();
        }
    }
}
