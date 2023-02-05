using CommandSystem;

using SlApi.Extensions;
using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.ResizeStates;

using System;

namespace SlApi.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class ResizeCommand : ICommand
    {
        public string Command { get; } = "resize";

        public string[] Aliases { get; } = new string[] { "size", "scale" };

        public string Description { get; } = "Sets a player's scale.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count != 4)
            {
                response = "Missing arguments! resize <target> <x> <y> <z>";
                return false;
            }

            if (!HubExtensions.TryGetHub(arguments.At(0), out var hub))
            {
                response = "Failed to find that player.";
                return false;
            }

            if (!float.TryParse(arguments.At(1), out var x))
            {
                response = "Failed to parse the X axis!";
                return false;
            }

            if (!float.TryParse(arguments.At(2), out var y))
            {
                response = "Failed to parse the Y axis!";
                return false;
            }

            if (!float.TryParse(arguments.At(3), out var z))
            {
                response = "Failed to parse the Z axis!";
                return false;
            }

            if (!hub.TryGetState<ResizeState>(out var resizeState))
                hub.TryAddState((resizeState = new ResizeState(hub)));

            resizeState.SetScale(new UnityEngine.Vector3(x, y, z));

            response = $"Resized {resizeState.Target.nicknameSync.MyNick} to {x} {y} {z}.";
            return true;
        }
    }
}