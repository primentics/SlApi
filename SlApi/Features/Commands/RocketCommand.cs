using CommandSystem;

using SlApi.Extensions;
using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.RocketStates;

using System;

namespace SlApi.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class RocketCommand : ICommand
    {
        public string Command { get; } = "rocket";
        public string[] Aliases { get; } = Array.Empty<string>();
        public string Description { get; } = "Sends a player to space.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = "Missing arguments! rocket <target>";
                return false;
            }

            var player = HubExtensions.GetHub(string.Join(" ", arguments));

            if (player is null)
            {
                response = $"Player does not exist.";
                return false;
            }

            if (!player.TryGetState<RocketState>(out var rocketState))
            {
                rocketState = new RocketState(player);

                player.TryAddState(rocketState);

                response = $"Sent {rocketState.Target.nicknameSync.MyNick} into space.";
                return true;
            }
            else
            {
                if (!rocketState.IsActive)
                {
                    player.SetActive<RocketState>(true);
                    response = $"Resumed rocket of {rocketState.Target.nicknameSync.MyNick}.";
                    return true;
                }
                else
                {
                    player.SetActive<RocketState>(false);
                    response = $"Stopped rocket of {rocketState.Target.nicknameSync.MyNick}.";
                    return true;
                }
            }
        }
    }
}