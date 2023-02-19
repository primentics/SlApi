using CommandSystem;

using SlApi.Extensions;
using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.InvisibleStates;

using System;

namespace SlApi.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TargetGhostCommand : ICommand
    {
        public string Command { get; } = "targetghost";
        public string[] Aliases { get; } = new string[] { "tg" };
        public string Description { get; } = "Ghosts you or another player to .. another player.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 2)
            {
                response = "Missing arguments! targetghost <player> <target>";
                return false;
            }

            var player = HubExtensions.GetHub(arguments.At(0));
            var target = HubExtensions.GetHub(arguments.At(1));

            if (player is null)
            {
                response = $"Player does not exist.";
                return false;
            }

            if (target is null)
            {
                response = "Target not found.";
                return false;
            }

            if (!player.TryGetState<InvisibilityState>(out var invisibilityState))
                player.TryAddState((invisibilityState = new InvisibilityState(player)));

            if (invisibilityState.IsVisibleTo(target))
            {
                invisibilityState.MakeInvisibleToObserver(target);

                response = $"{player.nicknameSync.MyNick} can't be seen by {target.nicknameSync.MyNick} anymore.";
                return true;
            }
            else
            {
                invisibilityState.MakeVisibleToObserver(target);

                response = $"{player.nicknameSync.MyNick} can now be seen by {target.nicknameSync.MyNick}.";
                return true;
            }
        }
    }
}
