using HarmonyLib;

using PlayerStatsSystem;

using SlApi.Features.CustomStats.Stats;

namespace SlApi.Features.CustomStats
{
    public static class CustomStatsController
    {
        [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.Awake))]
        public static bool Prefix(PlayerStats __instance)
        {
            __instance._hub = ReferenceHub.GetHub(__instance.gameObject);

            for (int i = 0; i < __instance.StatModules.Length; i++)
            {
                if (__instance.StatModules[i] is HealthStat)
                {
                    __instance.StatModules[i] = new CustomHealthStat();
                    __instance._dictionarizedTypes[typeof(HealthStat)] = __instance.StatModules[i];
                    __instance.StatModules[i].Init(__instance._hub);
                }
                else
                {
                    __instance._dictionarizedTypes[__instance.StatModules[i].GetType()] = __instance.StatModules[i];
                    __instance.StatModules[i].Init(__instance._hub);
                }
            }

            return false;
        }
    }
}
