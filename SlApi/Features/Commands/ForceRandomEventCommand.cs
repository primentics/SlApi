using CommandSystem;

using SlApi.Features.RandomEvents;

using System;
using System.Linq;

namespace SlApi.Features.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class ForceRandomEventCommand : ICommand
    {
        public string Command { get; } = "forcere";
        public string Description { get; } = "Forces a random event to occur.";

        public string[] Aliases { get; } = Array.Empty<string>();

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = $"forcere <event>";
                return false;
            }

            var eventName = string.Join(" ", arguments);

            if (string.IsNullOrWhiteSpace(eventName))
            {
                response = "Empty/whitespace event name!";
                return false;
            }

            var eventId = RandomEventManager.AllEvents.FirstOrDefault(x => 
                        x != null &&
                        !string.IsNullOrWhiteSpace(x.Id) &&
                        x.Id.ToLower() == eventName.ToLower());

            if (eventId is null)
            {
                response = $"No matching events were found.";
                return false;
            }

            RandomEventManager.StartEvent(eventId);

            response = $"Forced event {eventId.Id}!";
            return true;
        }
    }
}
