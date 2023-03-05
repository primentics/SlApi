using System;

using PluginAPI.Enums;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;

using SlApi.Dummies;

using PlayerStatsSystem;

using PlayerRoles;

using InventorySystem.Items;
using SlApi.Features.Audio;
using UnityEngine;

namespace SlApi.Events.Handlers
{
    public class PlayerHandlers
    {
        public static PlayerHandlers Instance;

        public PlayerHandlers()
        {
            if (Instance != null)
                throw new InvalidOperationException("PlayerHandlers are already active!");

            Instance = this;
        }

        [PluginEvent(ServerEventType.PlayerDropItem)]
        public void OnItemDropped(Player player, ItemBase itemBase)
        {
            if (player.IsServer)
                return;

            if (DummyPlayer.IsDummy(player.ReferenceHub))
                return;

            EventHandlers.TriggerEvents(ServerEventType.PlayerDropItem, player, itemBase);
        }

        [PluginEvent(ServerEventType.PlayerChangeRole)]
        public void OnRoleChanged(Player player, PlayerRoleBase oldRole, RoleTypeId newRole, RoleChangeReason changeReason)
        {
            if (player.IsServer)
                return;

            if (DummyPlayer.IsDummy(player.ReferenceHub))
                return;

            EventHandlers.TriggerEvents(ServerEventType.PlayerChangeRole, player, oldRole, newRole, changeReason);
        }

        [PluginEvent(ServerEventType.PlayerDying)]
        public void OnPlayerDied(Player player, Player attacker, DamageHandlerBase damageHandler)
        {
            if (player.IsServer)
                return;

            if (DummyPlayer.IsDummy(player.ReferenceHub))
                return;

            EventHandlers.TriggerEvents(ServerEventType.PlayerDying, player, attacker, damageHandler);
        }

        [PluginEvent(ServerEventType.PlayerSpawn)]
        public void OnSpawned(Player player, RoleTypeId role)
        {
            if (player.IsServer)
                return;

            if (DummyPlayer.IsDummy(player.ReferenceHub))
                return;

            EventHandlers.TriggerEvents(ServerEventType.PlayerSpawn, player, role);
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        public void OnPlayerLeft(Player player)
        {
            if (player.IsServer)
                return;

            if (DummyPlayer.IsDummy(player.ReferenceHub))
                return;

            EventHandlers.TriggerEvents(ServerEventType.PlayerLeft, player);

            try
            {
                if (AudioPlayer.TryGet(player.ReferenceHub, out var audioPlayer))
                    GameObject.Destroy(audioPlayer);
            }
            catch { }

            try
            {
                if (DummyPlayer.TryGetDummies(player.ReferenceHub, out var dummies))
                {
                    foreach (var dummy in dummies)
                        dummy.Destroy();
                }
            }
            catch { }
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        public void OnPlayerJoined(Player player)
        {
            if (player.IsServer)
                return;

            if (DummyPlayer.IsDummy(player.ReferenceHub))
                return;

            EventHandlers.TriggerEvents(ServerEventType.PlayerJoined, player);
        }
    }
}