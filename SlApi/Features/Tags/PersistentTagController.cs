using PluginAPI.Core;

using SlApi.Extensions;

using System.Collections.Generic;

namespace SlApi.Features.Tags
{
    public static class PersistentTagController
    {
        public static HashSet<string> PersistentTags = new HashSet<string>();

        private static void OnPlayerJoined(object[] args)
        {
            var hub = (args[0] as Player).ReferenceHub;

            if (PersistentTags.Contains(hub.characterClassManager.UserId))
            {
                hub.ShowTag();
                hub.ConsoleMessage("[Persistent Tags] Your tag was automatically shown. Use hidetag to disable.");
            }
        }
    }
}