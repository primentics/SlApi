using CommandSystem;

using PluginAPI.Core;

using SlApi.Extensions;
using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.SpectateStates;

using System;

namespace SlApi.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class AdminSpectateCommand : ICommand
    {
        public string Command { get; } = "adminspectate";
        public string[] Aliases { get; } = new string[] { "ast" };
        public string Description { get; } = "Disallows player other than administrators from spectating you/target.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = arguments.Count > 0 ? HubExtensions.GetHub(string.Join(" ", arguments)) : Player.Get(sender)?.ReferenceHub;

            if (player is null)
            {
                response = $"Player does not exist.";
                return false;
            }

            if (!player.TryGetState<SpectateState>(out var spectateState))
                player.TryAddState((spectateState = new SpectateState(player)));

            if (spectateState.Flags is SpectateFlags.ByStaff)
            {
                spectateState.Flags = SpectateFlags.ByStaff;
                response = $"Only staff members can spectate you now.";
                return true;
            }
            else
            {
                spectateState.Flags = SpectateFlags.ByAnyone;
                response = $"Everyone can spectate you now.";
                return true;
            }
        }
    }
}