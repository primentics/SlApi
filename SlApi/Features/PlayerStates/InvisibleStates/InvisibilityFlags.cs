using System;

namespace SlApi.Features.PlayerStates.InvisibleStates
{
    [Flags]
    public enum InvisibilityFlags
    {
        InvisibleToAll,
        InvisibleToTargets,
        InvisibleToNonStaff,

        Visible
    }
}