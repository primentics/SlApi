using PlayerRoles;

using PluginAPI.Core;

using SlApi.Events;
using SlApi.Events.CustomHandlers;

namespace SlApi.Features.PlayerStates.FakeRoleStates
{
    public class FakeRoleState : PlayerStateBase
    {
        private GenericHandler _onPlayerJoined;

        public FakeRoleState(ReferenceHub target) : base(target) { }

        public RoleTypeId FakedRole { get; set; } = RoleTypeId.None;
        public RoleTypeId Role { get => Target.roleManager.CurrentRole.RoleTypeId; }

        public override bool CanUpdateState()
            => false;

        public override void OnDied()
            => ResetRole();

        public override void OnRoleChanged()
            => ResetRole();

        public override void OnAdded()
        {
            _onPlayerJoined = new GenericHandler(PluginAPI.Enums.ServerEventType.PlayerJoined, OnPlayerJoined);

            EventHandlers.RegisterEvent(_onPlayerJoined);
        }

        public override void DisposeState()
        {
            if (_onPlayerJoined != null)
                EventHandlers.UnregisterEvent(_onPlayerJoined);

            _onPlayerJoined = null;
        }

        public void FakeRole(RoleTypeId fakedRole)
        {
            FakedRole = fakedRole;
            SendRole(fakedRole);
        }

        public void ResetRole()
        {
            FakedRole = RoleTypeId.None;
            SendRole(Role);
        }

        public void SendRole(RoleTypeId role)
        {
            foreach (var hub in ReferenceHub.AllHubs)
                SendRole(hub, role);
        }

        public void SendRole(ReferenceHub hub, RoleTypeId role)
        {
            if (hub.Mode != ClientInstanceMode.ReadyClient)
                return;

            if (hub.netId == Target.netId)
                return;

            hub.connectionToClient.Send(new RoleSyncInfo(Target, role, hub));
        }

        private void OnPlayerJoined(object[] args)
        {
            if (FakedRole is RoleTypeId.None || FakedRole == Role)
                return;

            SendRole((args[0] as Player).ReferenceHub, FakedRole);
        }
    }
}
