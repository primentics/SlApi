using SlApi.Configs;

namespace SlApi.Features.RandomEvents.Events
{
    public class Scp575Event : RandomEventBase
    {
        [Config("Scp575Event.Chance", "The chance of SCP-575 spawning.")]
        public static int Scp575Chance = 10;
    }
}