using CommandSystem;

using PluginAPI.Core;

using SlApi.Features.RainbowWarhead;

using System;

namespace SlApi.Features.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class DisableLightsCommand : ICommand
    {
        public string Command { get; } = "disablelights";

        public string[] Aliases { get; } = Array.Empty<string>();

        public string Description { get; } = "Disables rainbow lights.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var hub = Player.Get(sender)?.ReferenceHub;

            if (hub is null)
            {
                response = "Failed to get your Reference Hub.";
                return false;
            }

            if (RainbowWarheadController.BlacklistedUsers.Contains(hub.characterClassManager.UserId))
            {
                RainbowWarheadController.BlacklistedUsers.Remove(hub.characterClassManager.UserId);

                response = "Enabled rainbow lights.";
                return true;
            }
            else
            {
                RainbowWarheadController.BlacklistedUsers.Add(hub.characterClassManager.UserId);

                response = "Disabled rainbow lights.";
                return true;
            }
        }
    }
}