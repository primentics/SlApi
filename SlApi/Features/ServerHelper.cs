using SlApi.Extensions;

namespace SlApi.Features
{
    public static class ServerHelper
    {
        public static void DoGlobalBroadcast(object message, ushort time)
        {
            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                hub.PersonalBroadcast(message, time);
            }
        }

        public static void DoGlobalHint(object message, ushort time)
        {
            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                hub.PersonalHint(message, time);
            }
        }

        public static void DoGlobalConsoleMessage(object message, string color = "green")
        {
            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                hub.ConsoleMessage(message, color);
            }
        }
    }
}