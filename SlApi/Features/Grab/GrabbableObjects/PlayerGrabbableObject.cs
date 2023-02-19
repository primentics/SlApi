using SlApi.Extensions;
using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.FakeRoleStates;

using UnityEngine;

namespace SlApi.Features.Grab.GrabbableObjects
{
    public class PlayerGrabbableObject : PlayerGrabbableObjectBase
    {
        public override bool ShouldSpawnLight { get; } = false;

        public ReferenceHub GrabbedPlayer { get; set; }
        public FakeRoleState GrabbedRole { get; set; }

        public override void Grab(GameObject target, ReferenceHub grabbedBy)
        {
            base.Grab(target, grabbedBy);

            GrabbedPlayer = ReferenceHub.GetHub(target);

            if (!GrabbedPlayer.TryGetState(out FakeRoleState state))
                GrabbedPlayer.TryAddState(state = new FakeRoleState(GrabbedPlayer));

            GrabbedRole = state;
            GrabbedRole.FakeRole(PlayerRoles.RoleTypeId.Tutorial);
        }

        public override void Stop()
        {
            GrabbedRole.ResetRole();
            GrabbedRole = null;
            GrabbedPlayer = null;

            base.Stop();
        }

        public override void SetTargetRotation()
        {
            GrabbedPlayer.SetRotation(TargetRotation);
        }

        public override void SetTargetPosition()
        {
            GrabbedPlayer.SetPosition(TargetPosition);
        }
    }
}
