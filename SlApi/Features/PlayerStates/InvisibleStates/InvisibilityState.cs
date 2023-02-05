using NorthwoodLib.Pools;

using SlApi.Configs;

using System.Collections.Generic;

namespace SlApi.Features.PlayerStates.InvisibleStates
{
    public class InvisibilityState : PlayerStateBase
    {
        private HashSet<uint> _invisibleTo;

        public InvisibilityFlags Flags { get; set; }

        [Config("InvisibilityState.ClearOnRoleChange", "Whether or not to clear invisibility on role change.")]
        public static bool ClearOnRoleChange { get; set; } = true;

        [Config("InvisibilityState.ClearOnDeath", "Whether or not to clear invisibility on death.")]
        public static bool ClearOnDeath { get; set; } = true;

        [Config("InvisibilityState.IgnoreNorthwoodStaff", "Whether or not to allow Northwood's staff members to bypass invisibility.")]
        public static bool IgnoreNwStaff { get; set; } = true;

        public InvisibilityState(ReferenceHub target) : base(target)
        {
            Flags = InvisibilityFlags.Visible;
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
            if (!Flags.HasFlag(InvisibilityFlags.InvisibleToTargets))
                Flags |= InvisibilityFlags.InvisibleToTargets;

            if (!_invisibleTo.Contains(observer.netId))
                _invisibleTo.Add(observer.netId);
        }

        public void MakeVisibleToObserver(ReferenceHub observer)
        {
            if (!Flags.HasFlag(InvisibilityFlags.InvisibleToTargets))
                Flags |= InvisibilityFlags.InvisibleToTargets;

            if (_invisibleTo.Contains(observer.netId))
                _invisibleTo.Remove(observer.netId);
        }

        public void MakeInvisibleToEveryone()
        {
            Flags |= InvisibilityFlags.InvisibleToAll;
        }

        public void MakeVisibleToEveryone()
        {
            Flags -= InvisibilityFlags.InvisibleToAll;
            Flags -= InvisibilityFlags.InvisibleToNonStaff;
        }

        public void MakeInvisibleToNonStaff()
        {
            Flags |= InvisibilityFlags.InvisibleToNonStaff;
        }

        public bool IsVisibleTo(ReferenceHub observer)
        {
            if ((observer.serverRoles.RaEverywhere || observer.serverRoles.Staff) && IgnoreNwStaff)
                return true;
            else if (observer.serverRoles.BypassMode)
                return true;
            else if (Flags.HasFlag(InvisibilityFlags.Visible))
                return true;
            else if (Flags.HasFlag(InvisibilityFlags.InvisibleToAll))
                return false;
            else if (Flags.HasFlag(InvisibilityFlags.InvisibleToNonStaff) && !observer.serverRoles.RemoteAdmin)
                return false;
            else if (Flags.HasFlag(InvisibilityFlags.InvisibleToTargets))
                return !_invisibleTo.Contains(observer.netId);
            else
                return true;
        }
    }
}
