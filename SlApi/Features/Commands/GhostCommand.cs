using CommandSystem;

using PluginAPI.Core;

using SlApi.Extensions;
using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.InvisibleStates;

using System;

namespace SlApi.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class GhostCommand : ICommand
    {
        public string Command { get; } = "ghost";
        public string[] Aliases { get; } = Array.Empty<string>();
        public string Description { get; } = "Makes you or other player invisible.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = arguments.Count > 0 ? HubExtensions.GetHub(string.Join(" ", arguments)) : Player.Get(sender)?.ReferenceHub;

            if (player is null)
            {
                response = $"Player does not exist.";
                return false;
            }

            if (!player.TryGetState<InvisibilityState>(out var invisibilityState))
                player.TryAddState((invisibilityState = new InvisibilityState(player)));

            if (invisibilityState.ToEveryone)
            {
                invisibilityState.MakeVisibleToEveryone();
                response = $"{player.nicknameSync.MyNick} now visible to everyone.";
                return true;
            }
            else
            {
                invisibilityState.MakeInvisibleToEveryone();
                response = $"{player.nicknameSync.MyNick} is now invisible to everyone.";
                return true;
            }
        }
    }
}