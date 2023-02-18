using CommandSystem;

using PlayerRoles;

using SlApi.Extensions;

using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.FakeRoleStates;

using System;

namespace SlApi.Features.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class FakeRoleCommand : ICommand
    {
        public string Command { get; } = "fakerole";
        public string Description { get; } = "fakerole <target> <role>";

        public string[] Aliases { get; } = Array.Empty<string>();

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 2)
            {
                response = $"fakerole <target> <role>";
                return false;
            }

            if (!HubExtensions.TryGetHub(arguments.At(0), out var hub))
            {
                response = $"Player \"{arguments.At(0)}\" has not been found.";
                return false;
            }

            if (!hub.TryGetState<FakeRoleState>(out var state))
                hub.TryAddState(state = new FakeRoleState(hub));

            RoleTypeId role = RoleTypeId.None;

            if (int.TryParse(arguments.At(1), out var roleId))
                role = (RoleTypeId)roleId;
            else
            {
                if (!Enum.TryParse(arguments.At(1), out role))
                {
                    response = $"\"{arguments.At(1)}\" is not a valid role type.";
                    return false;
                }
            }    

            if (role is RoleTypeId.None)
            {
                response = "Please provide a valid role you want to fake.";
                return false;
            }

            state.FakeRole(role);

            response = $"Faked role of {hub.nicknameSync.MyNick} as {role}!";
            return true;
        }
    }
}
