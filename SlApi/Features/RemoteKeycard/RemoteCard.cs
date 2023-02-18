using CustomPlayerEffects;
using Interactables.Interobjects.DoorUtils;

using MapGeneration.Distributors;

using SlApi.Configs;
using SlApi.Events;
using SlApi.Events.CustomHandlers;
using SlApi.Extensions;
using SlApi.Patches.Feature.RemoteCard;

using System.Linq;

namespace SlApi.Features.RemoteKeycard
{
    public static class RemoteCard
    {
        [Config("RemoteCard.Doors", "Whether or not to allow doors opening.")]
        public static bool Doors { get; set; } = true;

        [Config("RemoteCard.Generators", "Whether or not to allow opening generators.")]
        public static bool Generators { get; set; } = true;

        [Config("RemoteCard.WarheadControls", "Whether or not to allow using the outsite warhead controls.")]
        public static bool WarheadControls { get; set; } = true;

        [Config("RemoteCard.Lockers", "Whether or not to allow opening lockers.")]
        public static bool Lockers { get; set; } = true;

        public const KeycardPermissions WarheadPerms = KeycardPermissions.AlphaWarhead;

        static RemoteCard()
        {
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.RoundRestart, OnRoundRestart));
        }

        public static bool CanOpenDoor(ReferenceHub hub, DoorVariant door)
        {
            if (!Doors)
                return false;

            return hub.inventory.UserInventory.Items.Values.Any(x => door.RequiredPermissions.CheckPermissions(x, hub));
        }

        public static bool CanOpenGenerator(ReferenceHub hub, Scp079Generator generator)
        {
            if (!Generators)
                return false;

            return hub.CanOpenGenerator(generator);
        }

        public static bool CanOpenLocker(ReferenceHub hub, LockerChamber chamber)
        {
            if (!Lockers)
                return false;

            return hub.CanOpenLocker(chamber);
        }

        public static bool CanOpenControls(ReferenceHub hub)
        {
            if (hub.playerEffectsController.TryGetEffect<AmnesiaItems>(out var amnesia) && amnesia.IsEnabled)
                return false;

            if (!WarheadControls)
                return false;

            if (hub.HasPermissions(WarheadPerms))
                return true;

            return false;
        }

        private static void OnRoundRestart(object[] args)
        {
            PlayerInteract_UserCode_CmdSwitchAWButton_Patch._outsitePanelScript = null;
        }
    }
}
