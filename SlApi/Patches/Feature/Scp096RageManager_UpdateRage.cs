using HarmonyLib;

using PlayerRoles.PlayableScps.Scp096;

using SlApi.Configs;

namespace SlApi.Patches.Feature
{
    [HarmonyPatch(typeof(Scp096RageManager), nameof(Scp096RageManager.UpdateRage))]
    public static class Scp096RageManager_UpdateRage
    {
        [Config("Scp096.InfiniteRage", "Whether or not to allow SCP-096 to rage until it kills all of it's targets.")]
        public static bool InfiniteRage;

        public static bool Prefix(Scp096RageManager __instance)
        {
            if (!InfiniteRage)
                return true;

            if (!__instance.IsEnraged)
                return false;

            if (__instance._targetsTracker.Targets.Count > 0)
            {
                return false;
            }
            else
            {
                __instance.ServerEndEnrage(true);
                return false;
            }
        }
    }
}