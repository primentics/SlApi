using CommandSystem;

using PluginAPI.Core;

using SlApi.Features.Grab;
using SlApi.Features.PlayerStates;

using System;

namespace SlApi.Features.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class UnGrabCommand : ICommand
    {
        public string Command { get; } = "ungrab";
        public string Description { get; } = "ungrab";

        public string[] Aliases { get; } = Array.Empty<string>();

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender)?.ReferenceHub;

            if (player is null)
            {
                response = "Player invalid.";
                return false;
            }

            if (!player.TryGetState(out PlayerGrabState playerGrabState))
            {
                response = "You do not have an active grabber.";
                return false;
            }

            playerGrabState.UnGrab();

            response = "Grab cancelled.";
            return true;
        }
    }
}
