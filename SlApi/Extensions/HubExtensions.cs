using Hints;

using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Keycards;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.NetworkMessages;

using PlayerStatsSystem;

using RelativePositioning;

using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.ResizeStates;

using System.Linq;

using UnityEngine;

namespace SlApi.Extensions
{
    public static class HubExtensions
    {
        public static bool HasKeycardPermission(this ReferenceHub hub, KeycardPermissions permissions, bool requiresAllPermissions = false)
        {
            if (hub.playerEffectsController.TryGetEffect<CustomPlayerEffects.AmnesiaItems>(out var effect) && effect.IsEnabled)
                return false;

            if (hub.inventory.CurInstance != null && hub.inventory.CurInstance is KeycardItem keycard)
                return requiresAllPermissions ? keycard.Permissions.HasFlag(permissions) : (keycard.Permissions & permissions) != 0;

            foreach (var item in hub.inventory.UserInventory.Items)
            {
                if (item.Value is KeycardItem card)
                {
                    if ((requiresAllPermissions ? card.Permissions.HasFlag(permissions) : (card.Permissions & permissions) != 0))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool HasItems(this ReferenceHub hub)
            => hub.inventory.UserInventory.Items.Any();

        public static void SetPosition(this ReferenceHub hub, Vector3 position)
        {
            if (hub.roleManager.CurrentRole is IFpcRole fpcRole && fpcRole.FpcModule != null)
            {
                var module = fpcRole.FpcModule;

                module._cachedPosition = position;
                module._transform.position = position;

                hub.connectionToClient.Send(new FpcOverrideMessage(position, hub.GetRealRotation().eulerAngles.magnitude));
            }
        }

        public static void SetRotation(this ReferenceHub hub, Quaternion rotation)
        {
            if (hub.roleManager.CurrentRole is IFpcRole fpcRole && fpcRole.FpcModule != null && fpcRole.FpcModule.MouseLook != null)
            {
                var module = fpcRole.FpcModule.MouseLook;

                module.CurrentHorizontal = WaypointBase.GetWorldRotation(module._fpmm.Motor.ReceivedPosition.WaypointId, rotation).eulerAngles.y;
                module.CurrentVertical = module._syncVertical;
            }

            hub.transform.rotation = rotation;
            hub.PlayerCameraReference.rotation = rotation;
        }

        public static Vector3 GetRealPosition(this ReferenceHub hub)
        {
            if (hub.roleManager.CurrentRole is IFpcRole fpcRole && fpcRole.FpcModule != null)
                return fpcRole.FpcModule.Position;

            return hub.PlayerCameraReference.position;
        }

        public static Quaternion GetRealRotation(this ReferenceHub hub)
        {
            if (hub.roleManager.CurrentRole is IFpcRole fpcRole && fpcRole.FpcModule != null && fpcRole.FpcModule.MouseLook != null)
                return fpcRole.FpcModule.MouseLook.TargetCamRotation;

            return hub.PlayerCameraReference.rotation;
        }

        public static void Scale(this ReferenceHub hub, float scale)
            => Resize(hub, hub.transform.localScale * scale);

        public static void Resize(this ReferenceHub hub, Vector3 size)
        {
            if (!hub.TryGetState<ResizeState>(out var state))
                hub.TryAddState((state = new ResizeState(hub)));

            state.SetScale(size);
        }

        public static void UseDisruptor(this ReferenceHub hub)
        {
            if (!hub.IsAlive())
                return;

            hub.characterClassManager.GodMode = false;
            hub.playerStats.KillPlayer(new DisruptorDamageHandler(new Footprinting.Footprint(hub), float.MaxValue));
        }

        public static void ConsoleMessage(this ReferenceHub hub, object message, string color = "green")
        {
            hub.characterClassManager.ConsolePrint(message.ToString(), color);
        }

        public static void PersonalHint(this ReferenceHub hub, object message, ushort time)
        {
            hub.hints.Show(new TextHint(message.ToString(), null, null, time));
        }

        public static void PersonalBroadcast(this ReferenceHub hub, object message, ushort time)
        {
            Broadcast.Singleton?.TargetClearElements(hub.connectionToClient);
            Broadcast.Singleton?.TargetAddElement(hub.connectionToClient, message.ToString(), time, Broadcast.BroadcastFlags.Normal);
        }

        public static bool TryGetHub(string value, out ReferenceHub hub)
        {
            hub = GetHub(value);
            return hub != null;
        }

        public static ReferenceHub GetHub(string value)
        {
            if (int.TryParse(value, out var pId))
                return ReferenceHub.GetHub(pId);
            else
                return ReferenceHub.AllHubs.FirstOrDefault(x => x.nicknameSync.MyNick.ToLower().Contains(value.ToLower()) || x.characterClassManager.UserId.Contains(value));
        }
    }
}