using InventorySystem;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;

using SlApi.Configs.Objects;
using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.FreezeStates;
using SlApi.Features.PlayerStates.SpectateStates;
using SlApi.Features.Voice.AdminVoice;

using System.Collections.Generic;

using UnityEngine;

namespace SlApi.Features.Spawnpoints
{
    public class SpawnpointDefinition
    {
        public string Name;

        public float Bounds;

        public int MaxPlayers;
        public int MaxAdminPlayers;

        public Vector3 Position;

        public RoleTypeId[] Roles;

        public bool AdminClearItems;
        public bool AdminFreeze;
        public bool AdminGhostToNonAdmin;
        public bool AdminGhostToSpectators;
        public bool AdminGodMode;

        public bool PlayerClearItems;
        public bool PlayerFreeze;
        public bool PlayerGhostToNonAdmin;
        public bool PlayerGhostToSpectators;
        public bool PlayerGodMode;

        public AdminVoiceChannel AdminVoiceChannel;

        public HashSet<ReferenceHub> PlayersIn;

        public SpawnpointDefinition(SpawnpointBase spawnpointBase)
        {
            Name = spawnpointBase.Name;
            Roles = spawnpointBase.AcceptedRoles;

            MaxPlayers = spawnpointBase.MaxNonAdminPlayers;
            MaxAdminPlayers = spawnpointBase.MaxPlayers;

            Bounds = spawnpointBase.Bounds;
            Position = Vector.FromVector(spawnpointBase.Position);

            AdminClearItems = spawnpointBase.AdminProperties?.ClearItems ?? false;
            AdminFreeze = spawnpointBase.AdminProperties?.Freeze ?? false;
            AdminGhostToNonAdmin = spawnpointBase.AdminProperties?.GhostToNonAdmin ?? false;
            AdminGhostToSpectators = spawnpointBase.AdminProperties?.GhostToSpectators ?? false;
            AdminGodMode = spawnpointBase.AdminProperties?.GodMode ?? false;

            PlayerClearItems = spawnpointBase.PlayerProperties?.ClearItems ?? false;
            PlayerFreeze = spawnpointBase.PlayerProperties?.Freeze ?? false;
            PlayerGhostToNonAdmin = spawnpointBase.PlayerProperties?.GhostToNonAdmin ?? false;
            PlayerGhostToSpectators = spawnpointBase.PlayerProperties?.GhostToSpectators ?? false;
            PlayerGodMode = spawnpointBase.PlayerProperties?.GodMode ?? false;

            PlayersIn = new HashSet<ReferenceHub>();

            AdminVoiceProcessor.TryGetChannel(spawnpointBase.VoiceChannelId, out AdminVoiceChannel);
        }

        public bool CanSpawn(ReferenceHub hub)
        {
            if (!Roles.Contains(hub.roleManager.CurrentRole.RoleTypeId))
                return false;

            if (hub.serverRoles.RemoteAdmin)
            {
                if (MaxAdminPlayers != -1)
                {
                    if (MaxAdminPlayers + 1 >= MaxAdminPlayers)
                        return false;

                    return true;
                }

                return true;
            }

            if (MaxPlayers != -1)
            {
                if (MaxPlayers + 1 >= MaxPlayers)
                    return false;

                return true;
            }

            return true;
        }

        public void Spawn(ReferenceHub hub)
        {
            SpawnpointManager.AddPlayer(hub, this);

            hub.TryOverridePosition(Position, Vector3.zero);

            ApplyPlayerProperties(hub);
        }

        public void AddSpawnpoint()
        {
            SpawnpointManager.OnPlayerSpawnedOnPoint += OnPlayerPoint;
            SpawnpointManager.OnPlayerEnteredPoint += OnPlayerPoint;
            SpawnpointManager.OnPlayerLeftPoint += OnPlayerRemoved;
        }

        public void RemoveSpawnpoint()
        {
            SpawnpointManager.OnPlayerSpawnedOnPoint -= OnPlayerPoint;
            SpawnpointManager.OnPlayerEnteredPoint -= OnPlayerPoint;
            SpawnpointManager.OnPlayerLeftPoint -= OnPlayerRemoved;
        }

        private void OnPlayerRemoved(SpawnpointBase spawnpointBase, ReferenceHub hub)
        {
            if (spawnpointBase.Name != Name)
                return;

            RemovePlayerProperties(hub);
        }

        private void OnPlayerPoint(SpawnpointBase spawnpointBase, ReferenceHub hub)
        {
            if (spawnpointBase.Name != Name)
                return;

            ApplyPlayerProperties(hub);
        }

        private void ApplyPlayerProperties(ReferenceHub hub)
        {
            bool freeze = PlayerFreeze;
            bool clear = PlayerClearItems;
            bool god = PlayerGodMode;
            bool gSpec = PlayerGhostToSpectators;
            bool gAdmin = PlayerGhostToNonAdmin;

            if (hub.serverRoles.RemoteAdmin)
            {
                freeze = AdminFreeze;
                clear = AdminClearItems;
                god = AdminGodMode;
                gSpec = AdminGhostToSpectators;
                gAdmin = AdminGhostToNonAdmin;
            }

            if (freeze)
            {
                if (!hub.TryGetState<PlayerFreezeState>(out var state))
                    hub.TryAddState((state = new PlayerFreezeState(hub, PlayerFreezeStateReason.BySpawnpointManager)));
                else
                {
                    state.OnAdded();
                    state.IsActive = true;
                }
            }

            if (clear)
            {
                foreach (var item in hub.inventory.UserInventory.Items)
                {
                    hub.inventory.ServerRemoveItem(item.Key, item.Value.PickupDropModel);
                }
            }

            if (god)
            {
                hub.characterClassManager.GodMode = true;
            }

            if (gSpec)
            {
                if (!hub.TryGetState<SpectateState>(out var state))
                    hub.TryAddState((state = new SpectateState(hub)));

                state.Flags = SpectateFlags.ByNoOne;
            }

            if (gAdmin)
            {
                if (!hub.TryGetState<SpectateState>(out var state))
                    hub.TryAddState((state = new SpectateState(hub)));

                state.Flags = SpectateFlags.ByStaff;
            }
        }

        private void RemovePlayerProperties(ReferenceHub hub)
        {
            bool freeze = PlayerFreeze;
            bool clear = PlayerClearItems;
            bool god = PlayerGodMode;
            bool gSpec = PlayerGhostToSpectators;
            bool gAdmin = PlayerGhostToNonAdmin;

            if (hub.serverRoles.RemoteAdmin)
            {
                freeze = AdminFreeze;
                clear = AdminClearItems;
                god = AdminGodMode;
                gSpec = AdminGhostToSpectators;
                gAdmin = AdminGhostToNonAdmin;
            }

            if (freeze)
            {
                hub.SetActive<PlayerFreezeState>(false);
            }

            if (clear)
            {
                foreach (var item in hub.inventory.UserInventory.Items)
                {
                    hub.inventory.ServerRemoveItem(item.Key, item.Value.PickupDropModel);
                }
            }

            if (god)
            {
                hub.characterClassManager.GodMode = false;
            }

            if (gSpec)
            {
                if (!hub.TryGetState<SpectateState>(out var state))
                    hub.TryAddState((state = new SpectateState(hub)));

                state.Flags = SpectateFlags.ByAnyone;
            }

            if (gAdmin)
            {
                if (!hub.TryGetState<SpectateState>(out var state))
                    hub.TryAddState((state = new SpectateState(hub)));

                state.Flags = SpectateFlags.ByAnyone;
            }
        }
    }
}