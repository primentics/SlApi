using HarmonyLib;

using PlayerRoles.FirstPersonControl;

using NorthwoodLib.Pools;

using System.Collections.Generic;
using System.Reflection.Emit;

using SlApi.Dummies;

namespace SlApi.Patches.Dummy
{
    [HarmonyPatch(typeof(FpcMouseLook), nameof(FpcMouseLook.UpdateRotation))]
    public static class FpcMouseLook_UpdateRotation_DummyFix
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var newInstructions = ListPool<CodeInstruction>.Shared.Rent(instructions);
            var skip = generator.DefineLabel();

            newInstructions[newInstructions.Count - 1].labels.Add(skip);
            newInstructions.InsertRange(0, new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FpcMouseLook), nameof(FpcMouseLook._hub))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DummyPlayer), nameof(DummyPlayer.IsDummy))),
                new CodeInstruction(OpCodes.Brtrue_S, skip)
            });

            foreach (CodeInstruction instruction in newInstructions)
                yield return instruction;

            ListPool<CodeInstruction>.Shared.Return(newInstructions);
        }
    }
}
