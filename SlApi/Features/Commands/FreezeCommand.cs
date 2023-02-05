using CommandSystem;

using SlApi.Extensions;
using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.FreezeStates;

using System;

namespace SlApi.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class FreezeCommand : ICommand
    {
        public string Command { get; } = "freeze";
        public string[] Aliases { get; } = Array.Empty<string>();
        public string Description { get; } = "Freezes a player.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = "Missing arguments! freeze <target>";
                return false;
            }

            var player = HubExtensions.GetHub(string.Join(" ", arguments));

            if (player is null)
            {
                response = $"Player does not exist.";
                return false;
            }

            if (!player.TryGetState<PlayerFreezeState>(out var playerFreezeState))
            {
                playerFreezeState = new PlayerFreezeState(player, PlayerFreezeStateReason.ByRemoteAdmin);
                player.TryAddState(playerFreezeState);

                response = $"Froze {playerFreezeState.Target.nicknameSync.MyNick}.";
                return true;
            }
            else
            {
                if (playerFreezeState.IsActive)
                {
                    player.SetActive<PlayerFreezeState>(false);

                    response = $"Unfroze {playerFreezeState.Target.nicknameSync.MyNick}.";
                    return true;
                }
                else
                {
                    player.SetActive<PlayerFreezeState>(true);

                    response = $"Froze {playerFreezeState.Target.nicknameSync.MyNick}.";
                    return true;
                }
            }
        }
    }
}
