using MapGeneration.Distributors;

namespace SlApi.Extensions
{
    public static class GeneratorExtensions
    {
        public static bool IsOpen(this Scp079Generator gen)
            => gen.HasFlag(gen._flags, Scp079Generator.GeneratorFlags.Open);

        public static bool IsUnlocked(this Scp079Generator gen)
            => gen.HasFlag(gen._flags, Scp079Generator.GeneratorFlags.Unlocked);

        public static void Open(this Scp079Generator gen)
            => gen.ServerSetFlag(Scp079Generator.GeneratorFlags.Open, true);

        public static void Close(this Scp079Generator gen)
            => gen.ServerSetFlag(Scp079Generator.GeneratorFlags.Open, false);

        public static void Unlock(this Scp079Generator gen)
            => gen.ServerSetFlag(Scp079Generator.GeneratorFlags.Unlocked, true);

        public static void Lock(this Scp079Generator gen)
            => gen.ServerSetFlag(Scp079Generator.GeneratorFlags.Unlocked, false);
    }
}