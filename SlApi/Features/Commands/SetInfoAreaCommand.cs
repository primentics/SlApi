using AzyWorks.Utilities;

using CommandSystem;

using SlApi.Extensions;

using System;
using System.Linq;

namespace SlApi.Features.Commands
{
    public class SetInfoAreaCommand : ICommand
    {
        public string Command { get; } = "setarea";
        public string Description { get; } = "setarea <target> <area>";

        public string[] Aliases { get; } = Array.Empty<string>();

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 2)
            {
                response = $"setarea <target> <area>";
                return false;
            }

            if (!HubExtensions.TryGetHub(arguments.At(0), out var hub))
            {
                response = $"Player \"{arguments.At(0)}\" has not been found.";
                return false;
            }

            var str = string.Join(" ", arguments.Skip(1));

            if (!Enum.TryParse(str, out PlayerInfoArea playerInfoArea))
            {
                response = "Invalid PlayerInfoArea value.";
                return false;
            }

            var enumBuilder = new EnumBuilder<PlayerInfoArea>(hub.nicknameSync.Network_playerInfoToShow);

            if (enumBuilder.HasValue(playerInfoArea))
            {
                enumBuilder.WithoutValue(playerInfoArea);

                hub.nicknameSync.Network_playerInfoToShow = enumBuilder.GetValue();

                response = $"Removed area: {playerInfoArea} from {hub.nicknameSync.MyNick}";
                return true;
            }
            else
            {
                enumBuilder.WithValue(playerInfoArea);

                hub.nicknameSync.Network_playerInfoToShow = enumBuilder.GetValue();

                response = $"Added area: {playerInfoArea} to {hub.nicknameSync.MyNick}";
                return true;
            }
        }
    }
}
