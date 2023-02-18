using System.Collections.Generic;

namespace SlApi.Features.CustomLoadouts
{
    public class CustomLoadoutChance
    {
        public Dictionary<string, int> PerRoleChances { get; set; } = new Dictionary<string, int>()
        {
            ["*"] = 50
        };
    }
}