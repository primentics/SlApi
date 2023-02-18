namespace SlApi.Features.CustomLoadouts
{
    public class CustomLoadoutRestriction
    {
        public CustomLoadoutRestrictionType Type { get; set; } = CustomLoadoutRestrictionType.None;
        public string[] Items { get; set; } = new string[] { "None", "None" };
    }
}
