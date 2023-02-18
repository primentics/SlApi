using PlayerRoles;

using System.Collections.Generic;

namespace SlApi.Features.CustomEscape
{
    public class ChancedEscape
    {
        public RoleTypeId EscapingAs { get; set; }

        public bool CuffedByOpposingTeamOnly { get; set; }

        public Dictionary<RoleTypeId, int> ChangingTo { get; set; } 
    }
}