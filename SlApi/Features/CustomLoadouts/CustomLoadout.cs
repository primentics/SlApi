using PlayerRoles;

using System.Collections.Generic;

namespace SlApi.Features.CustomLoadouts
{
    public class CustomLoadout
    {
        public string Name { get; set; } = "example";

        public CustomLoadoutRestriction Restriction { get; set; } = new CustomLoadoutRestriction();
        public CustomLoadoutChance Chance { get; set; } = new CustomLoadoutChance();

        public HashSet<CustomLoadoutItem> Items { get; set; } = new HashSet<CustomLoadoutItem>() { new CustomLoadoutItem(), new CustomLoadoutItem() };
        public HashSet<CustomLoadoutCharacterModifier> Modifiers { get; set; } = new HashSet<CustomLoadoutCharacterModifier>() { new CustomLoadoutCharacterModifier(), new CustomLoadoutCharacterModifier() };

        public Dictionary<RoleTypeId, CustomLoadoutChance> Roles { get; set; } = new Dictionary<RoleTypeId, CustomLoadoutChance>()
        {
            [RoleTypeId.None] = new CustomLoadoutChance(),
            [RoleTypeId.Tutorial] = new CustomLoadoutChance()
        };

        public CustomLoadoutInventoryBehaviour InventoryBehaviour { get; set; } = CustomLoadoutInventoryBehaviour.AddItemsDropExcessive;
    }
}