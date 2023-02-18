using AzyWorks.Randomization.Weighted;
using AzyWorks.Utilities;
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

using UnityEngine;

namespace SlApi.Features.CustomLoadouts
{
    public static class CustomLoadoutsController
    {
        [Config("CustomLoadouts.Loadouts", "A list of all custom loadouts.")]
        public static HashSet<CustomLoadout> Loadouts = new HashSet<CustomLoadout>() { new CustomLoadout(), new CustomLoadout() };

        [Config("CustomLoadouts.Debug", "Debug toggle")]
        public static bool Debug = true;

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
            Log.Debug($"Applying loadout {customLoadout.Name} to {hub.characterClassManager.UserId} with {itemsToGive.Count} items and {modifiersToApply.Count} modifiers", Debug, "SL API::Custom Loadouts");

            if (itemsToGive != null && itemsToGive.Count > 0)
                GiveItems(hub, customLoadout.InventoryBehaviour, itemsToGive);

            if (modifiersToApply != null && modifiersToApply.Count > 0)
                ApplyModifiers(hub, modifiersToApply);

            OnLoadoutApplied?.Invoke(hub, customLoadout);

            Log.Debug($"Applied loadout", Debug, "SL API::Custom Loadouts");
        }

        public static void GiveItems(ReferenceHub hub, CustomLoadoutInventoryBehaviour inventoryBehaviour, HashSet<CustomLoadoutItem> items)
        {
            Log.Debug($"Giving items: {inventoryBehaviour} ({items.Count})", Debug, "SL API::Custom Loadouts");

            if (inventoryBehaviour is CustomLoadoutInventoryBehaviour.AddItemsClearInventory)
            {
                Log.Debug($"Removing items", Debug, "SL API::Custom Loadouts");
                
                foreach (var item in hub.inventory.UserInventory.Items)
                    hub.inventory.ServerRemoveItem(item.Key, item.Value.PickupDropModel);
            }

            foreach (var item in items)
            {
                Log.Debug($"Processing item: {item.Item} {item.Amount}", Debug, "SL API::Custom Loadouts");

                if (item.Amount <= 0)
                {
                    Log.Debug($"Amount 0", Debug, "SL API::Custom Loadouts");
                    continue;
                }

                if (item.Item.IsAmmoItem())
                {
                    hub.inventory.ServerAddAmmo(item.Item, item.Amount);
                    Log.Debug($"Added {item.Amount} of {item.Item} ammo", Debug, "SL API::Custom Loadouts");
                    continue;
                }

                for (int i = 0; i < item.Amount; i++)
                {
                    Log.Debug($"Spawning index {i} of item {item.Item}", Debug, "SL API::Custom Loadouts");

                    var itemBase = hub.inventory.ServerAddItem(item.Item);

                    if (itemBase is null)
                    {
                        Log.Debug($"ItemBase is null, users inventory is most likely full.", Debug, "SL API::Custom Loadouts");

                        if (inventoryBehaviour is CustomLoadoutInventoryBehaviour.AddItemsDropExcessive)
                        {
                            Log.Debug($"Spawning on ground", Debug, "SL API::Custom Loadouts");
                            Log.Debug($"Creating item instance", Debug, "SL API::Custom Loadouts");

                            itemBase = hub.inventory.CreateItemInstance(new ItemIdentifier(item.Item, ItemSerialGenerator.GenerateNext()), false);

                            Log.Debug($"Instance: {itemBase?.ItemSerial.ToString() ?? "null"}", Debug, "SL API::Custom Loadouts");
                            Log.Debug($"Creating pickup", Debug, "SL API::Custom Loadouts");

                            var pickup = hub.inventory.ServerCreatePickup(
                                itemBase,
                                new InventorySystem.Items.Pickups.PickupSyncInfo(
                                    item.Item,
                                    hub.GetRealPosition(),
                                    hub.GetRealRotation(),
                                    itemBase.Weight,
                                    itemBase.ItemSerial),
                                true);

                            Log.Debug($"Pickup: {pickup?.NetworkInfo.Serial.ToString() ?? "null"}", Debug, "SL API::Custom Loadouts");
                            Log.Debug($"Pickup spawned", Debug, "SL API::Custom Loadouts");
                        }
                        else
                        {
                            Log.Debug($"Breaking", Debug, "SL API::Custom Loadouts");
                            break;
                        }
                    }
                }

                Log.Debug($"Item processed", Debug, "SL API::Custom Loadouts");
            }

            Log.Debug($"Items processed.", Debug, "SL API::Custom Loadouts");
        }

        public static void ApplyModifiers(ReferenceHub hub, HashSet<CustomLoadoutCharacterModifier> modifiers)
        {
            Log.Debug($"Applying {modifiers.Count} modifiers", Debug, "SL API::Custom Loadouts");

            if (!hub.TryGetState<CustomLoadoutState>(out var state))
                hub.TryAddState((state = new CustomLoadoutState(hub)));

            foreach (var modifier in modifiers)
            {
                Log.Debug($"Applying modifier {modifier.Type} {modifier.Value}", Debug, "SL API::Custom Loadouts");

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
            Log.Debug($"Applying instant modifier", Debug, "SL API::Custom Loadouts");

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
            Log.Debug($"Validating modifiers", Debug, "SL API::Custom Loadouts");

            var validModifiers = HashSetPool<CustomLoadoutCharacterModifier>.Shared.Rent();

            foreach (var modifier in loadout.Modifiers)
            {
                Log.Debug($"Processing modifier: {modifier.Type}", Debug, "SL API::Custom Loadouts");

                if (GetChance(hub, modifier.Chance) is 0)
                {
                    Log.Debug($"Chance 0", Debug, "SL API::Custom Loadouts");
                    continue;
                }

                Log.Debug($"Modifier valid", Debug, "SL API::Custom Loadouts");
                validModifiers.Add(modifier);
            }

            modifiers = validModifiers.Where(x => CalculateChance(x.Chance, hub)).ToHashSet();

            Log.Debug($"Valid modifiers: {modifiers.Count}", Debug, "SL API::Custom Loadouts");

            HashSetPool<CustomLoadoutCharacterModifier>.Shared.Return(validModifiers);
            return modifiers.Count > 0;
        }

        public static bool TryGetItems(ReferenceHub hub, CustomLoadout loadout, out HashSet<CustomLoadoutItem> items)
        {
            Log.Debug($"Validating items", Debug, "SL API::Custom Loadouts");

            var validItems = HashSetPool<CustomLoadoutItem>.Shared.Rent();

            foreach (var item in loadout.Items)
            {
                Log.Debug($"Item: {item.Item} {item.Amount}", Debug, "SL API::Custom Loadouts");

                if (item.Amount <= 0)
                {
                    Log.Debug($"Amount 0", Debug, "SL API::Custom Loadouts");
                    continue;
                }

                if (GetChance(hub, item.Chance) is 0)
                {
                    Log.Debug($"Chance 0", Debug, "SL API::Custom Loadouts");
                    continue;
                }

                Log.Debug($"Item validated", Debug, "SL API::Custom Loadouts");
                validItems.Add(item);
            }

            items = validItems.Where(x => CalculateChance(x.Chance, hub)).ToHashSet();

            Log.Debug($"Valid items: {items.Count}", Debug, "SL API::Custom Loadouts");

            HashSetPool<CustomLoadoutItem>.Shared.Return(validItems);
            return items.Count > 0;
        }

        public static bool TryGetLoadout(ReferenceHub hub, HashSet<CustomLoadout> loadouts, out CustomLoadout customLoadout)
        {
            Log.Debug($"Validating loadout", Debug, "SL API::Custom Loadouts");

            var validLoadouts = HashSetPool<CustomLoadout>.Shared.Rent();

            foreach (var loadout in loadouts)
            {
                Log.Debug($"Processing loadout restriction: {loadout.Name}", Debug, "SL API::Custom Loadouts");

                if (!ProcessLoadoutRestriction(hub, loadout.Restriction))
                {
                    Log.Debug($"Restriction check failed", Debug, "SL API::Custom Loadouts");
                    continue;
                }

                Log.Debug($"Processing loadout chance: {loadout.Name}", Debug, "SL API::Custom Loadouts");

                if (GetChance(hub, loadout.Chance) is 0)
                {
                    Log.Debug($"Chance failed.", Debug, "SL API::Custom Loadouts");
                    continue;
                }

                Log.Debug($"Adding valid loadout: {loadout.Name}", Debug, "SL API::Custom Loadouts");
                validLoadouts.Add(loadout);
            }

            if (loadouts.Count is 0)
            {
                Log.Debug($"No valid loadouts", Debug, "SL API::Custom Loadouts");
                customLoadout = null;
                return false;
            }

            if (loadouts.Count is 1)
            {
                Log.Debug($"One valid loadout: {loadouts.First().Name}", Debug, "SL API::Custom Loadouts");
                customLoadout = loadouts.First();
                return true;
            }

            Log.Debug($"Picking by weight ({validLoadouts.Count})", Debug, "SL API::Custom Loadouts");

            if (!WeightPicker.TryPick(validLoadouts, x => GetChance(hub, x.Chance), out customLoadout))
            {
                Log.Debug($"Failed to pick loadout", Debug, "SL API::Custom Loadouts");
                HashSetPool<CustomLoadout>.Shared.Return(validLoadouts);
                return false;
            }

            Log.Debug($"Picked loadout: {customLoadout.Name}", Debug, "SL API::Custom Loadouts");
            HashSetPool<CustomLoadout>.Shared.Return(validLoadouts);
            return customLoadout != null;
        }

        public static int GetChance(ReferenceHub hub, CustomLoadoutChance chance)
            => GetChance(hub, chance.PerRoleChances);

        public static int GetChance(ReferenceHub hub, Dictionary<string, int> chances)
        {
            Log.Debug($"Getting chance: {chances.Count}", Debug, "SL API::Custom Loadouts");

            if (chances.TryGetValue("*", out var everyoneChance))
            {
                Log.Debug($"Returning everyone chance: {everyoneChance}", Debug, "SL API::Custom Loadouts");
                return everyoneChance;
            }
            else if (chances.TryGetValue(hub.characterClassManager.UserId, out var idChance))
            {
                Log.Debug($"Returning ID chance: {idChance}", Debug, "SL API::Custom Loadouts");
                return idChance;
            }
            else if (hub.TryGetRoleKey(out var roleKey) && chances.TryGetValue(roleKey, out var roleChance))
            {
                Log.Debug($"Returning role chance: {roleChance}", Debug, "SL API::Custom Loadouts");
                return roleChance;
            }

            Log.Debug($"No chance found", Debug, "SL API::Custom Loadouts");
            return 0;
        }

        public static bool CalculateChance(CustomLoadoutChance loadoutChance, ReferenceHub hub)
        {
            Log.Debug($"Calculating chance: {loadoutChance.PerRoleChances.Count}", Debug, "SL API::Custom Loadouts");

            var chance = GetChance(hub, loadoutChance);

            Log.Debug($"Retrieved chance: {chance}", Debug, "SL API::Custom Loadouts");

            if (chance <= 0)
            {
                Log.Debug($"Failed - zero", Debug, "SL API::Custom Loadouts");
                return false;
            }

            if (chance >= 100)
            {
                Log.Debug($"Success - hundred", Debug, "SL API::Custom Loadouts");
                return true;
            }

            var random = StaticRandom.RandomInt(0, 100);

            Log.Debug($"Random: {random}", Debug, "SL API::Custom Loadouts");

            var scaledChance = Mathf.CeilToInt(chance / ((random / 10) * (chance / 10)));

            Log.Debug($"Scaled: {scaledChance}", Debug, "SL API::Custom Loadouts");

            if (scaledChance < 10 && random > 10)
                scaledChance *= 10;
            else if (scaledChance > 10 && random < 10)
                scaledChance /= 10;

            Log.Debug($"Fixed chance: {scaledChance}", Debug, "SL API::Custom Loadouts");
            Log.Debug($"Success: {scaledChance >= random}", Debug, "SL API::Custom Loadouts");

            return scaledChance >= random;
        }

        public static bool ProcessLoadoutRestriction(ReferenceHub hub, CustomLoadoutRestriction restriction)
        {
            Log.Debug($"Processing restriction: {restriction?.Type.ToString() ?? "null"}", Debug, "SL API::Custom Loadouts");

            if (restriction != null)
            {
                Log.Debug($"Processing", Debug, "SL API::Custom Loadouts");

                if (restriction.Type != CustomLoadoutRestrictionType.None)
                {
                    Log.Debug($"Restricted", Debug, "SL API::Custom Loadouts");

                    if (restriction.Type is CustomLoadoutRestrictionType.UserId)
                    {
                        Log.Debug($"Searching ID {hub.characterClassManager.UserId}", Debug, "SL API::Custom Loadouts");

                        if (!restriction.Items.Contains(hub.characterClassManager.UserId))
                        {
                            Log.Debug($"Failed", Debug, "SL API::Custom Loadouts");
                            return false;
                        }
                        else
                        {
                            Log.Debug($"Success", Debug, "SL API::Custom Loadouts");
                            return true;
                        }
                    }
                    else if (restriction.Type is CustomLoadoutRestrictionType.UserRole)
                    {
                        Log.Debug($"Searching role", Debug, "SL API::Custom Loadouts");

                        if (restriction.Items.Contains("*"))
                        {
                            Log.Debug($"Success - *", Debug, "SL API::Custom Loadouts");
                            return true;
                        }
                        else if (hub.TryGetRoleKey(out var role))
                        {
                            if (restriction.Items.Contains(role))
                            {
                                Log.Debug($"Success", Debug, "SL API::Custom Loadouts");
                                return true;
                            }
                            else
                            {
                                Log.Debug($"Failed", Debug, "SL API::Custom Loadouts");
                                return false;
                            }
                        }
                        else
                        {
                            Log.Debug($"Failed", Debug, "SL API::Custom Loadouts");
                            return false;
                        }
                    }
                    else
                    {
                        Log.Debug($"Unknown type", Debug, "SL API::Custom Loadouts");
                        return false;
                    }
                }
            }

            Log.Debug($"Null restriction success", Debug, "SL API::Custom Loadouts");
            return true;
        }

        public static bool TryGetLoadouts(ReferenceHub hub, RoleTypeId role, out HashSet<CustomLoadout> loadouts)
        {
            loadouts = new HashSet<CustomLoadout>();

            Log.Debug($"TryGetLoadouts: {role} {(hub.TryGetRoleKey(out var roleKey) ? roleKey : "No role")}", Debug, "SL API::Custom Loadouts");

            foreach (var loadout in Loadouts)
            {
                Log.Debug($"Visiting role: {loadout.Name}", Debug, "SL API::Custom Loadouts");

                if (loadout.Roles.ContainsKey(role))
                {
                    Log.Debug($"Role allowed", Debug, "SL API::Custom Loadouts");

                    if (CalculateChance(
                        WeightPicker.Pick(
                            loadout.Roles.Where(x => x.Key == role && GetChance(hub, x.Value) != 0),
                            y => GetChance(hub, y.Value)).Value,
                        hub))
                    {
                        Log.Debug($"Chance success", Debug, "SL API::Custom Loadouts");
                        loadouts.Add(loadout);
                    }
                    else
                    {
                        Log.Debug($"Chance failed", Debug, "SL API::Custom Loadouts");
                        continue;
                    }
                }
                else
                {
                    Log.Debug($"Role disallowed", Debug, "SL API::Custom Loadouts");
                    continue;
                }
            }

            Log.Debug($"Found {loadouts.Count} loadouts", Debug, "SL API::Custom Loadouts");

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
                Log.Debug($"OnPlayerSpawned {role}", Debug, "SL API::Custom Loadouts");

                if (TryGetLoadouts(hub, role, out var loadouts))
                {
                    Log.Debug($"OnPlayerSpawned: Loadouts: {loadouts.Count}", Debug, "SL API::Custom Loadouts");

                    if (TryGetLoadout(hub, loadouts, out var loadout))
                    {
                        Log.Debug($"OnPlayerSpawned: Loadout: {loadout.Name}", Debug, "SL API::Custom Loadouts");

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