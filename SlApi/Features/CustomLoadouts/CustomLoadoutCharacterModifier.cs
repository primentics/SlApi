namespace SlApi.Features.CustomLoadouts
{
    public class CustomLoadoutCharacterModifier
    {
        public CustomLoadoutCharacterModifierType Type { get; set; } = CustomLoadoutCharacterModifierType.Health;
        public CustomLoadoutCharacterModifierDuration Duration { get; set; } = CustomLoadoutCharacterModifierDuration.Instant;
        public CustomLoadoutCharacterModifierValueType ValueType { get; set; } = CustomLoadoutCharacterModifierValueType.Permanent;

        public CustomLoadoutChance Chance { get; set; } = new CustomLoadoutChance();

        public string Value { get; set; } = "120";

        public int DurationValue { get; set; } = 0;
        public int ValueInterval { get; set; } = 5;
    }
}