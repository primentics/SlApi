using SlApi.Extensions;

using UnityEngine;

namespace SlApi.Features.PlayerStates.FreezeStates
{
    public class PlayerFreezeState : PlayerStateBase
    {
        private Vector3 _freezePos;
        private PlayerFreezeStateReason _reason;

        public PlayerFreezeState(ReferenceHub hub, PlayerFreezeStateReason reason) : base(hub) 
        { 
            _reason = reason;
        }

        public override bool CanUpdateState()
            => true;

        public override bool IsFinished()
            => false;

        public override bool ShouldClearOnDeath()
            => true;

        public override bool ShouldClearOnRoleChange()
            => _reason != PlayerFreezeStateReason.ByRemoteAdmin;

        public override void OnAdded()
        {
            _freezePos = Target.transform.position;
        }

        public override void UpdateState()
        {
            Target.SetPosition(_freezePos);
        }
    }
}