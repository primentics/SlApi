using CommandSystem;

using InventorySystem.Items.Pickups;

using PluginAPI.Core;

using SlApi.Features.Grab;
using SlApi.Features.PlayerStates;

using System;

namespace SlApi.Features.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class GrabCommand : ICommand
    {
        public string Command { get; } = "grab";
        public string Description { get; } = "grab";

        public string[] Aliases { get; } = Array.Empty<string>();

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender)?.ReferenceHub;

            if (player == null)
            {
                response = "Player invalid.";
                return false;
            }

            if (!PlayerGrabHelper.TryGetObject(player, out var target))
            {
                response = "No targets.";
                return false;
            }

            if (!player.TryGetState(out PlayerGrabState grabber))
                player.TryAddState(grabber = new PlayerGrabState(player));

            if (target.TryGetComponent(out IDestructible destructible) 
                && ReferenceHub.TryGetHubNetID(destructible.NetworkId, out var hub) 
                && hub.netId != player.netId)
            {
                grabber.GrabPlayer(hub);

                response = $"Grabbed player {hub.nicknameSync.MyNick}";
                return true;
            }
            else if (target.TryGetComponent(out ItemPickupBase pickup))
            {
                grabber.GrabPickup(pickup);

                response = $"Grabbed pickup {pickup.Info.ItemId}";
                return true;
            }
            else
            {
                grabber.GrabOther(target);

                response = $"Grabbed unknown target {target.name}";
                return true;
            }
        }
    }
}
