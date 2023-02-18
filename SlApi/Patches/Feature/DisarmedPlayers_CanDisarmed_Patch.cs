using HarmonyLib;

using InventorySystem.Disarming;
using InventorySystem.Items;

using PlayerRoles;

using SlApi.Configs;

namespace SlApi.Patches.Feature
{
    [HarmonyPatch(typeof(DisarmedPlayers), nameof(DisarmedPlayers.CanDisarm))]
    public static class DisarmedPlayers_CanDisarmed_Patch
    {
        [Config("Cuffing.AllowInterTeam", "Whether or not to remove all disarming limitations.")]
        public static bool TeamCuff;

        public static bool Prefix(ReferenceHub disarmerHub, ReferenceHub targetHub, ref bool __result)
        {
            if (!TeamCuff && (disarmerHub.GetFaction() == targetHub.GetFaction()))
            {
                __result = false;
                return false;
            }

            if (!disarmerHub.IsHuman() || !targetHub.IsHuman())
            {
                __result = false;
                return false;
            }

            if (targetHub.interCoordinator.AnyBlocker(BlockedInteraction.BeDisarmed))
            {
                __result = false;
                return false;
            }

            ItemBase curInstance = disarmerHub.inventory.CurInstance;
            IDisarmingItem disarmingItem;
            __result = curInstance != null && (disarmingItem = (curInstance as IDisarmingItem)) != null && disarmingItem.AllowDisarming;
            return false;
        }
    }
}