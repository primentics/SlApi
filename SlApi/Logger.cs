using PluginAPI.Core;

using SlApi.Configs;

namespace SlApi
{
    public static class Logger
    {
        [Config("Log.ShowDebug", "Whether or not to log debug messages.")]
        public static bool ShowDebug = true;

        public static void Info(object message)
            => Log.Info(message.ToString(), "Sl Api");

        public static void Error(object message)
            => Log.Error(message.ToString(), "Sl Api");

        public static void Warn(object message)
            => Log.Warning(message.ToString(), "Sl Api");

        public static void Debug(object message)
        {
            if (!ShowDebug)
                return;

            Log.Debug(message.ToString(), "Sl Api");
        }
    }
}