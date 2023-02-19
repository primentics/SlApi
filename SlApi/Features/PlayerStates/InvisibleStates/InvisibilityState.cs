using NorthwoodLib.Pools;

using SlApi.Configs;

using System.Collections.Generic;

namespace SlApi.Features.PlayerStates.InvisibleStates
{
    public class InvisibilityState : PlayerStateBase
    {
        private HashSet<uint> _invisibleTo;

        [Config("InvisibilityState.ClearOnRoleChange", "Whether or not to clear invisibility on role change.")]
        public static bool ClearOnRoleChange { get; set; } = true;

        [Config("InvisibilityState.ClearOnDeath", "Whether or not to clear invisibility on death.")]
        public static bool ClearOnDeath { get; set; } = true;

        [Config("InvisibilityState.IgnoreNorthwoodStaff", "Whether or not to allow Northwood's staff members to bypass invisibility.")]
        public static bool IgnoreNwStaff { get; set; } = true;

        public bool ToEveryone;
        public bool ToNonStaff;

        public InvisibilityState(ReferenceHub target) : base(target)
        {

        }

        public override void OnAdded()
        {
            _invisibleTo = HashSetPool<uint>.Shared.Rent();
        }

        public override void DisposeState()
        {
            HashSetPool<uint>.Shared.Return(_invisibleTo);
        }

        public override bool ShouldClearOnDeath()
            => ClearOnDeath;

        public override bool ShouldClearOnRoleChange()
            => ClearOnRoleChange;

        public void MakeInvisibleToObserver(ReferenceHub observer)
        {
            if (!_invisibleTo.Contains(observer.netId))
                _invisibleTo.Add(observer.netId);
        }

        public void MakeVisibleToObserver(ReferenceHub observer)
        {
            if (_invisibleTo.Contains(observer.netId))
                _invisibleTo.Remove(observer.netId);
        }

        public void MakeInvisibleToEveryone()
        {
            ToEveryone = true;
        }

        public void MakeVisibleToEveryone()
        {
            ToEveryone = false;
        }

        public void MakeInvisibleToNonStaff()
        {
            ToNonStaff = true;
        }

        public void MakeVisibleToNonStaff()
        {
            ToNonStaff = false;
        }

        public bool IsVisibleTo(ReferenceHub observer)
        {
            if ((observer.serverRoles.RaEverywhere || observer.serverRoles.Staff) && IgnoreNwStaff)
                return true;
            else if (observer.serverRoles.BypassMode)
                return true;
            else if (ToEveryone)
                return false;
            else if (ToNonStaff && !observer.serverRoles.RemoteAdmin)
                return false;
            else if (_invisibleTo.Contains(observer.netId))
                return false;
            else
                return true;
        }
    }
}
