using PlayerRoles;

using SlApi.Configs;

namespace SlApi.Features.PlayerStates.SpectateStates
{
    public class SpectateState : PlayerStateBase
    {
        [Config("SpectateState.IgnoreNorthwoodStaff", "Whether or not to allow Northwood's staff members to bypass a spectate block.")]
        public static bool IgnoreNwStaff { get; set; } = true;

        public SpectateState(ReferenceHub target) : base(target) { }

        public SpectateFlags Flags { get; set; } = SpectateFlags.ByAnyone;

        public override void OnRoleChanged()
        {
            if (!Target.IsAlive())
                return;

            if (Flags is SpectateFlags.ByAnyone)
                return;

            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (hub.netId == Target.netId)
                    continue;

                if (hub.roleManager.CurrentRole.RoleTypeId != RoleTypeId.Spectator)
                    continue;

                if (CanBeSpectatedBy(hub))
                    continue;

                hub.connectionToClient.Send(new RoleSyncInfo(Target, RoleTypeId.Spectator, hub));
            }
        }

        public override void DisposeState()
        {
            if (!Target.IsAlive())
                return;

            if (Flags is SpectateFlags.ByAnyone)
                return;

            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (hub.netId == Target.netId)
                    continue;

                if (hub.roleManager.CurrentRole.RoleTypeId != RoleTypeId.Spectator)
                    continue;

                if (CanBeSpectatedBy(hub))
                    continue;

                hub.connectionToClient.Send(new RoleSyncInfo(Target, Target.roleManager.CurrentRole.RoleTypeId, hub));
            }
        }

        public bool CanBeSpectatedBy(ReferenceHub spectator)
        {
            if ((spectator.serverRoles.RaEverywhere || spectator.serverRoles.Staff))
                return true;

            if (spectator.serverRoles.BypassMode)
                return true;

            if (Flags is SpectateFlags.ByAnyone)
                return true;

            if (Flags is SpectateFlags.ByNoOne)
                return false;

            return spectator.serverRoles.RemoteAdmin;
        }
    }
}