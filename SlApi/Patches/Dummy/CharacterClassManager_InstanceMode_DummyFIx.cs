using HarmonyLib;

using SlApi.Dummies;

namespace SlApi.Patches.Dummy
{
    [HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.InstanceMode), MethodType.Setter)]
    public static class CharacterClassManager_InstanceMode_DummyFix
    {
        public static bool Prefix(CharacterClassManager __instance, ref ClientInstanceMode value)
        {
            if (DummyPlayer.TryGetDummy(__instance.Hub, out _))
                value = ClientInstanceMode.DedicatedServer;

            return true;
        }
    }
}