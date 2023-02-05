using HarmonyLib;

using Respawning;

using SlApi.Configs;

namespace SlApi.Patches.Feature
{
    [HarmonyPatch(typeof(RespawnManager), nameof(RespawnManager.Update))]
    public static class RespawnManager_Update_Patch
    {
        [Config("Respawning.Disable", "Whether or not to completely disable respawns.")]
        public static bool Disable = false;

        public static bool Prefix()
            => !Disable;
    }
}