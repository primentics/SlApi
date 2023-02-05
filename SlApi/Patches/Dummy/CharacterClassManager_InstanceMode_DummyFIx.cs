using HarmonyLib;

using SlApi.Dummies;

using System.Linq;

namespace SlApi.Patches.Dummy
{
    [HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.InstanceMode), MethodType.Setter)]
    public static class CharacterClassManager_InstanceMode_DummyFix
    {
        public static bool Prefix(CharacterClassManager __instance)
        {
            if (DummyPlayer.Dummies.Any(x => x.Hub.GetInstanceID() == __instance.Hub.GetInstanceID()))
            {
                __instance._targetInstanceMode = ClientInstanceMode.Host;
                return false;
            }

            return true;
        }
    }
}