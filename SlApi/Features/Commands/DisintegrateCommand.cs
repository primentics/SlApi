using CommandSystem;

using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;

using PluginAPI.Core;

using System;

using Mirror;

using UnityEngine;

using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Pickups;

using MapGeneration.Distributors;

using SlApi.Configs;
using SlApi.Extensions;

using AdminToys;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.PlayableScps.Scp079;

namespace SlApi.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class DisintegrateCommand : ICommand
    {
        public string Command { get; } = "disintegrate";

        public string[] Aliases { get; } = new string[] { "desintegrate", "dis", "des" };

        public string Description { get; } = "Disintegrates a player.";

        [Config("DisintegrateCommand.MaxDistance", "The maximum raycast distance for the disintegrate command.")]
        public static float MaxDistance = 100f;

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission(PlayerPermissions.PlayersManagement))
            {
                response = "Missing permissions (PlayersManagement)!";
                return false;
            }

            var hub = Player.Get(sender)?.ReferenceHub;

            if (hub == null)
            {
                response = "Failed to find your Hub.";
                return false;
            }

            PerformRaycast(hub);

            response = "";
            return true;
        }

        public static void PerformRaycast(ReferenceHub hub)
        {
            if (!Physics.Raycast(hub.PlayerCameraReference.position, hub.PlayerCameraReference.forward, out var hit, MaxDistance, Physics.AllLayers))
                return;

            NetworkUtils.SendDisruptorHitMessage(hit.transform.position, Quaternion.LookRotation(-hit.normal));

            if (hit.transform.TryGetComponent(out IDestructible destructible) 
                && ReferenceHub.TryGetHubNetID(destructible.NetworkId, out var target)
                && target.netId != hub.netId)
            {
                target.UseDisruptor();
                return;
            }

            var door = hit.transform.gameObject.GetComponentInParent<DoorVariant>();

            if (door != null)
            {
                door.Destroy(true);
                return;
            }

            var work = hit.transform.gameObject.GetComponentInParent<WorkstationController>();

            if (work != null)
            {
                NetworkServer.Destroy(work.gameObject);
                return;
            }

            var tesla = hit.transform.gameObject.GetComponentInParent<TeslaGate>();

            if (tesla != null)
            {
                NetworkServer.Destroy(tesla.gameObject);
                return;
            }

            var generator = hit.transform.gameObject.GetComponentInParent<Scp079Generator>();

            if (generator != null)
            {
                NetworkServer.Destroy(generator.gameObject);
                return;
            }

            var pickup = hit.transform.gameObject.GetComponent<ItemPickupBase>();

            if (pickup != null)
            {
                pickup.DestroySelf();
                return;
            }

            var locker = hit.transform.gameObject.GetComponentInParent<Locker>();

            if (locker != null)
            {
                NetworkServer.Destroy(locker.gameObject);
                return;
            }

            var chamber = hit.transform.gameObject.GetComponentInParent<LockerChamber>();

            if (chamber != null)
            {
                NetworkServer.Destroy(chamber.gameObject);
                return;
            }

            var pedestalScpLocker = hit.transform.gameObject.GetComponentInParent<PedestalScpLocker>();

            if (pedestalScpLocker != null)
            {
                NetworkServer.Destroy(pedestalScpLocker.gameObject);
                return;
            }

            var structure = hit.transform.gameObject.GetComponentInParent<SpawnableStructure>();

            if (structure != null)
            {
                NetworkServer.Destroy(structure.gameObject);
                return;
            }

            var elevator = hit.transform.gameObject.GetComponentInParent<ElevatorChamber>();

            if (elevator != null)
            {
                NetworkServer.Destroy(elevator.gameObject);
                return;
            }

            var primitive = hit.transform.gameObject.GetComponentInParent<AdminToyBase>();

            if (primitive != null)
            {
                NetworkServer.Destroy(primitive.gameObject);
                return;
            }

            var window = hit.transform.gameObject.GetComponentInParent<BreakableWindow>();

            if (window != null)
            {
                NetworkServer.Destroy(window.gameObject);
                return;
            }

            var camera = hit.transform.gameObject.GetComponentInParent<Scp079Camera>();

            if (camera != null)
            {
                NetworkServer.Destroy(camera.gameObject);
                return;
            }

            var speaker = hit.transform.gameObject.GetComponentInParent<Scp079Speaker>();

            if (speaker != null)
            {
                NetworkServer.Destroy(speaker.gameObject);
                return;
            }

            if (hit.transform.TryGetComponent(out NetworkTransform networkTransform))
            {
                NetworkServer.Destroy(networkTransform.gameObject);
                return;
            }

            if (hit.transform.TryGetComponent(out NetworkIdentity identity))
            {
                NetworkServer.Destroy(identity.gameObject.transform.parent.gameObject);
                return;
            }
        }
    }
}