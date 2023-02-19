using CommandSystem;

using System;

namespace SlApi.Features.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class SlReloadCommand : ICommand
    {
        public string Command { get; } = "slreload";
        public string Description { get; } = "Reloads the SL API plugin.";

        public string[] Aliases { get; } = Array.Empty<string>();

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            EntryPoint.Instance.Reload();
            response = "Reloaded.";
            return true;
        }
    }
}
