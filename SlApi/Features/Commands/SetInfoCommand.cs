using CommandSystem;

using SlApi.Extensions;

using System;
using System.Linq;

namespace SlApi.Features.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class SetInfoCommand : ICommand
    {
        public string Command { get; } = "setinfo";
        public string Description { get; } = "setinfo <target> <info>";

        public string[] Aliases { get; } = Array.Empty<string>();

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 2)
            {
                response = "setinfo <target> <info>";
                return false;
            }

            if (!HubExtensions.TryGetHub(arguments.At(0), out var hub))
            {
                response = $"Failed to find \"{arguments.At(0)}\".";
                return false;
            }

            var info = string.Join(" ", arguments.Skip(1));

            hub.nicknameSync.Network_customPlayerInfoString = info;

            response = $"Set custom info string of {hub.nicknameSync.MyNick} to {info}";
            return true;
        }
    }
}