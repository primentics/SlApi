using Mirror;

using PlayerRoles;

using SlApi.Configs;
using SlApi.Extensions;
using UnityEngine;

namespace SlApi.Features.PlayerStates.ResizeStates
{
    public class ResizeState : PlayerStateBase
    {
        private Vector3 _originalScale;
        private Vector3 _savedScale;

        [Config("ResizeState.ClearOnRoleChange", "Whether or not to keep the scale on role change.")]
        public static bool ClearOnRoleChange = false;

        [Config("ResizeState.ClearOnDeath", "Whether or not to keep the scale on death.")]
        public static bool ClearOnDeath = false;

        public ResizeState(ReferenceHub target) : base(target)
        {

        }

        public void SetScale(Vector3 scale)
        {
            _savedScale = scale;

            if (Target is null || !Target.IsAlive())
                return;

            Target.transform.localScale = scale;

            foreach (var ply in ReferenceHub.AllHubs)
            {
                if (ply.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (ply.netId == Target.netId)
                    continue;

                ply.connectionToClient.Send(new ObjectDestroyMessage
                {
                    netId = Target.netId
                });

                NetworkServer.SendSpawnMessage(Target.netIdentity, ply.connectionToClient);
            }

            Target.roleManager.ServerSetRole(Target.roleManager.CurrentRole.RoleTypeId, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);
        }

        public void ResetScale()
            => SetScale(_originalScale);

        public override void DisposeState()
            => ResetScale();

        public override bool ShouldClearOnDeath()
            => ClearOnDeath;

        public override bool ShouldClearOnRoleChange()
            => ClearOnRoleChange;

        public override void OnAdded()
        {
            _originalScale = Target.transform.localScale;
            _savedScale = Target.transform.localScale;
        }

        public override void OnRoleChanged()
        {
            if (Target.IsAlive())
                SetScale(_savedScale);
        }
    }
}