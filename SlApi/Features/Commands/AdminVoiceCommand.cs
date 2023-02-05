using CommandSystem;

using SlApi.Features.Voice.AdminVoice;

using System;

namespace SlApi.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class AdminVoiceCommand : ICommand
    {
        public string Command { get; } = "adminvoice";
        public string[] Aliases { get; } = new string[] { "avc" };
        public string Description { get; } = "Toggles the admin voice.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (AdminVoiceProcessor.IsGloballyActive)
            {
                AdminVoiceProcessor.IsGloballyActive = false;
                response = $"Disabled admin-only voice chat.";
                return true;
            }
            else
            {
                AdminVoiceProcessor.IsGloballyActive = true;
                response = $"Enabled admin-only voice chat.";
                return true;
            }
        }
    }
}
