namespace SlApi.Features.CustomLoadouts
{
    public class CustomLoadoutItem
    {
        public ItemType Item { get; set; } = ItemType.None;

        public int Amount { get; set; } = 1;

        public CustomLoadoutChance Chance { get; set; } = new CustomLoadoutChance();
    }
}