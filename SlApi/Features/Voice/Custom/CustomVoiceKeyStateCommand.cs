using CommandSystem;
using PluginAPI.Core;

using SlApi.Features.PlayerStates;

using System;

namespace SlApi.Features.Voice.Custom
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class CustomVoiceKeyStateCommand : ICommand
    {
        public string Command { get; } = "popvoice";

        public string[] Aliases { get; } = new string[] { "pvoice", "switchvoice", "svoice" };

        public string Description { get; } = "Switches to the next channel, if you're using a custom voice system.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender)?.ReferenceHub;

            if (player is null)
            {
                response = "Player object not found.";
                return false;
            }

            if (!player.TryGetState<CustomVoiceState>(out var voice))
            {
                response = $"You are not currently using a custom voice system.";
                return false;
            }

            voice.OnKeyUsed();

            response = "";
            return true;
        }
    }
}