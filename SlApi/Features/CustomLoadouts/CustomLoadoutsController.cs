using AzyWorks.System.Weights;

using InventorySystem;
using InventorySystem.Items;

using MEC;

using NorthwoodLib.Pools;

using PlayerRoles;
using PlayerStatsSystem;
using PluginAPI.Core;

using SlApi.Configs;
using SlApi.Events;
using SlApi.Events.CustomHandlers;
using SlApi.Extensions;
using SlApi.Features.CustomStats.Stats;
using SlApi.Features.PlayerStates;
using SlApi.Features.PlayerStates.FakeRoleStates;

using System;
using System.Collections.Generic;
using System.Linq;

namespace SlApi.Features.CustomLoadouts
{
    public static class CustomLoadoutsController
    {
        [Config("CustomLoadouts.Loadouts", "A list of all custom loadouts.")]
        public static HashSet<CustomLoadout> Loadouts = new HashSet<CustomLoadout>() { new CustomLoadout(), new CustomLoadout() };

        static CustomLoadoutsController()
        {
            EventHandlers.RegisterEvent(new GenericHandler(PluginAPI.Enums.ServerEventType.PlayerSpawn, OnPlayerSpawned));
        }

        public static event Action<ReferenceHub, CustomLoadoutCharacterModifier> OnModifierApplied;
        public static event Action<ReferenceHub, CustomLoadout> OnLoadoutApplied;

        public static void ApplyLoadout(
            ReferenceHub hub,
            CustomLoadout customLoadout, 

            HashSet<CustomLoadoutItem> itemsToGive, 
            HashSet<CustomLoadoutCharacterModifier> modifiersToApply)
        {
            if (itemsToGive != null && itemsToGive.Count > 0)
                GiveItems(hub, customLoadout.InventoryBehaviour, itemsToGive);

            if (modifiersToApply != null && modifiersToApply.Count > 0)
                ApplyModifiers(hub, modifiersToApply);

            OnLoadoutApplied?.Invoke(hub, customLoadout);
        }

        public static void GiveItems(ReferenceHub hub, CustomLoadoutInventoryBehaviour inventoryBehaviour, HashSet<CustomLoadoutItem> items)
        {
            if (inventoryBehaviour is CustomLoadoutInventoryBehaviour.AddItemsClearInventory)
            {
                foreach (var item in hub.inventory.UserInventory.Items)
                    hub.inventory.ServerRemoveItem(item.Key, item.Value.PickupDropModel);
            }

            foreach (var item in items)
            {
                if (item.Amount <= 0)
                {
                    continue;
                }

                if (item.Item.IsAmmoItem())
                {
                    hub.inventory.ServerAddAmmo(item.Item, item.Amount);
                    continue;
                }

                for (int i = 0; i < item.Amount; i++)
                {
                    var itemBase = hub.inventory.ServerAddItem(item.Item);
                    if (itemBase is null)
                    {
                        if (inventoryBehaviour is CustomLoadoutInventoryBehaviour.AddItemsDropExcessive)
                        {
                            itemBase = hub.inventory.CreateItemInstance(new ItemIdentifier(item.Item, ItemSerialGenerator.GenerateNext()), false);
                            _ = hub.inventory.ServerCreatePickup(
                                itemBase,
                                new InventorySystem.Items.Pickups.PickupSyncInfo(
                                    item.Item,
                                    hub.GetRealPosition(),
                                    hub.GetRealRotation(),
                                    itemBase.Weight,
                                    itemBase.ItemSerial),
                                true);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        public static void ApplyModifiers(ReferenceHub hub, HashSet<CustomLoadoutCharacterModifier> modifiers)
        {
            if (!hub.TryGetState<CustomLoadoutState>(out var state))
                hub.TryAddState((state = new CustomLoadoutState(hub)));

            foreach (var modifier in modifiers)
            {
                OnModifierApplied?.Invoke(hub, modifier);

                if (modifier.Duration is CustomLoadoutCharacterModifierDuration.Instant)
                {
                    ApplyInstantModifier(hub, modifier);
                    continue;
                }
            }
        }

        public static void ApplyInstantModifier(ReferenceHub hub, CustomLoadoutCharacterModifier modifier)
        {
            switch (modifier.Type)
            {
                case CustomLoadoutCharacterModifierType.ArtificialHealth:
                    {
                        if (!int.TryParse(modifier.Value, out var value))
                            break;

                        if (!hub.playerStats.TryGetModule<AhpStat>(out var module))
                            break;

                        var process = module._activeProcesses.FirstOrDefault();

                        if (process is null)
                            process = hub.playerStats.GetModule<AhpStat>().ServerAddProcess(value, value, 1.2f, 0.7f, 0f, false);

                        if (value > process.Limit)
                            process.Limit = value;

                        process.CurrentAmount = value;

                        break;
                    }

                case CustomLoadoutCharacterModifierType.MaxArtificialHealth:
                    {
                        if (!int.TryParse(modifier.Value, out var value))
                            break;

                        var ahpProcesses = hub.GetActiveAhpProcesses();
                        var ahpProcess = ahpProcesses.FirstOrDefault();

                        if (ahpProcess is null)
                            ahpProcess = hub.playerStats.GetModule<AhpStat>().ServerAddProcess(value, value, 1.2f, 0.7f, 0f, false);

                        ahpProcess.Limit = value;
                        break;
                    }

                case CustomLoadoutCharacterModifierType.Health:
                    {
                        if (!int.TryParse(modifier.Value, out var value))
                            break;

                        if (!hub.playerStats.TryGetModule<HealthStat>(out var module))
                            break;

                        module.CurValue = value;
                        break;
                    }

                case CustomLoadoutCharacterModifierType.MaxHealth:
                    {
                        if (!int.TryParse(modifier.Value, out var value))
                            break;

                        if (!hub.playerStats.TryGetModule<HealthStat>(out var module) || !(module is CustomHealthStat customHealthStat))
                            break;

                        customHealthStat.OverrideMaxValue(value);
                        break;
                    }

                case CustomLoadoutCharacterModifierType.FakeRole:
                    {
                        RoleTypeId role = RoleTypeId.None;

                        if (int.TryParse(modifier.Value, out var id))
                            role = (RoleTypeId)id;
                        else if (Enum.TryParse(modifier.Value, out role)) { }

                        if (role is RoleTypeId.None)
                            return;

                        if (!hub.TryGetState<FakeRoleState>(out var state))
                            hub.TryAddState((state = new FakeRoleState(hub)));

                        state.FakeRole(role);
                        break;
                    }
            }
        }

        public static bool TryGetModifiers(ReferenceHub hub, CustomLoadout loadout, out HashSet<CustomLoadoutCharacterModifier> modifiers)
        {
            var validModifiers = HashSetPool<CustomLoadoutCharacterModifier>.Shared.Rent();

            foreach (var modifier in loadout.Modifiers)
            {
                if (GetChance(hub, modifier.Chance) is 0)
                {
                    continue;
                }

                validModifiers.Add(modifier);
            }

            modifiers = validModifiers.Where(x => CalculateChance(x.Chance, hub)).ToHashSet();

            HashSetPool<CustomLoadoutCharacterModifier>.Shared.Return(validModifiers);
            return modifiers.Count > 0;
        }

        public static bool TryGetItems(ReferenceHub hub, CustomLoadout loadout, out HashSet<CustomLoadoutItem> items)
        {
            var validItems = HashSetPool<CustomLoadoutItem>.Shared.Rent();
            foreach (var item in loadout.Items)
            {
                if (item.Amount <= 0)
                {
                    continue;
                }

                if (GetChance(hub, item.Chance) is 0)
                {
                    continue;
                }

                validItems.Add(item);
            }

            items = validItems.Where(x => CalculateChance(x.Chance, hub)).ToHashSet();

            HashSetPool<CustomLoadoutItem>.Shared.Return(validItems);
            return items.Count > 0;
        }

        public static bool TryGetLoadout(ReferenceHub hub, HashSet<CustomLoadout> loadouts, out CustomLoadout customLoadout)
        {
            var validLoadouts = HashSetPool<CustomLoadout>.Shared.Rent();

            foreach (var loadout in loadouts)
            {
                if (!ProcessLoadoutRestriction(hub, loadout.Restriction))
                {
                    continue;
                }

                if (GetChance(hub, loadout.Chance) is 0)
                {
                    continue;
                }

                validLoadouts.Add(loadout);
            }

            if (loadouts.Count is 0)
            {
                customLoadout = null;
                HashSetPool<CustomLoadout>.Shared.Return(validLoadouts);
                return false;
            }

            if (loadouts.Count is 1)
            {
                customLoadout = loadouts.First();
                HashSetPool<CustomLoadout>.Shared.Return(validLoadouts);
                return true;
            }

            if (!WeightPick.TryPick(validLoadouts, x => GetChance(hub, x.Chance), out customLoadout))
            {
                HashSetPool<CustomLoadout>.Shared.Return(validLoadouts);
                return false;
            }

            HashSetPool<CustomLoadout>.Shared.Return(validLoadouts);
            return customLoadout != null;
        }

        public static int GetChance(ReferenceHub hub, CustomLoadoutChance chance)
            => GetChance(hub, chance.PerRoleChances);

        public static int GetChance(ReferenceHub hub, Dictionary<string, int> chances)
        {
            if (chances.TryGetValue("*", out var everyoneChance))
            {
                return everyoneChance;
            }
            else if (chances.TryGetValue(hub.characterClassManager.UserId, out var idChance))
            {
                return idChance;
            }
            else if (hub.TryGetRoleKey(out var roleKey) && chances.TryGetValue(roleKey, out var roleChance))
            {
                return roleChance;
            }

            return 0;
        }

        public static bool CalculateChance(CustomLoadoutChance loadoutChance, ReferenceHub hub)
        {
            var chance = GetChance(hub, loadoutChance);
            if (chance <= 0)
            {
                return false;
            }

            if (chance >= 100)
            {
                return true;
            }

            var dict = new Dictionary<bool, int>() {
                [true] = chance,
                [false] = 100 - chance
            };

            return WeightPick.Pick(dict, x => x.Value).Key;
        }

        public static bool ProcessLoadoutRestriction(ReferenceHub hub, CustomLoadoutRestriction restriction)
        {
            if (restriction != null)
            {
                if (restriction.Type != CustomLoadoutRestrictionType.None)
                {
                    if (restriction.Type is CustomLoadoutRestrictionType.UserId)
                    {
                        if (!restriction.Items.Contains(hub.characterClassManager.UserId))
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else if (restriction.Type is CustomLoadoutRestrictionType.UserRole)
                    {
                        if (restriction.Items.Contains("*"))
                        {
                            return true;
                        }
                        else if (hub.TryGetRoleKey(out var role))
                        {
                            if (restriction.Items.Contains(role))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool TryGetLoadouts(ReferenceHub hub, RoleTypeId role, out HashSet<CustomLoadout> loadouts)
        {
            loadouts = new HashSet<CustomLoadout>();
            foreach (var loadout in Loadouts)
            {
                if (loadout.Roles.ContainsKey(role))
                {
                    if (CalculateChance(
                        WeightPick.Pick(
                            loadout.Roles.Where(x => x.Key == role && GetChance(hub, x.Value) != 0),
                            y => GetChance(hub, y.Value)).Value,
                        hub))
                    {
                        loadouts.Add(loadout);
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }
            }

            return loadouts.Count > 0;
        }

        private static void OnPlayerSpawned(object[] args)
        {
            var hub = (args[0] as Player).ReferenceHub;
            var role = (RoleTypeId)args[1];

            if (role is RoleTypeId.None || role is RoleTypeId.Spectator || role is RoleTypeId.CustomRole || role is RoleTypeId.Spectator)
                return;

            Timing.CallDelayed(0.5f, () => 
            {
                if (TryGetLoadouts(hub, role, out var loadouts))
                {
                    if (TryGetLoadout(hub, loadouts, out var loadout))
                    {
                        ApplyLoadout(
                            hub,
                            loadout,

                            TryGetItems(hub, loadout, out var items) ? items : null,
                            TryGetModifiers(hub, loadout, out var modifiers) ? modifiers : null);
                    }
                }
            });
        }
    }
}