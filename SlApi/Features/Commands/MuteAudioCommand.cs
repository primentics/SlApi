using CommandSystem;

using PluginAPI.Core;

using System;

using SlApi.Dummies;
using SlApi.Features.Audio;

namespace SlApi.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class MuteAudioCommand : ICommand
    {
        public string Command { get; } = "muteaudio";

        public string[] Aliases { get; } = new string[] { "mutea" };

        public string Description { get; } = "Mutes audio from all dummies on the server.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);

            if (player == null)
            {
                response = "Failed to retrieve your Player object.";
                return false;
            }

            if (DummyPlayer.IsMutedToGlobal(player.ReferenceHub) || AudioPlayer.Mutes.Contains(player.UserId))
            {
                DummyPlayer.UnmuteGlobal(player.ReferenceHub);
                AudioPlayer.Mutes.Remove(player.UserId);

                response = "Unmuted all audio.";
                return true;
            }
            else
            {
                DummyPlayer.MuteGlobal(player.ReferenceHub);
                AudioPlayer.Mutes.Add(player.UserId);

                response = "Muted all audio.";
                return true;
            }
        }
    }
}
