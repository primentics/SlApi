using PluginAPI.Core;

namespace SlApi.Features {
    public static class RoundHelper {
        public static int ActiveSeconds { get => Round.Duration.Seconds; }
    }
}
