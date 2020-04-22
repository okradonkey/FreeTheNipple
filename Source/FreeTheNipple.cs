using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using OpCodes = System.Reflection.Emit.OpCodes;


namespace FreeTheNipple
{

    //Pawn_ApparelTracker.PsychologicallyNude

    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmonyInstance = new Harmony(id: "RimWorld.OkraDonkey.FreeTheNipple.main");
            harmonyInstance.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Pawn_ApparelTracker), "PsychologicallyNude", MethodType.Getter)]
    public static class Pawn_ApparelTracker_PsychologicallyNude_Transpiler
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {

            // In PsychologicallyNude IL code:
            //   ldloc.0 --> hasPants
            //   ldloc.1 --> hasShirt
            // Find the hasPants with a hasShirt two lines later.
            // Remove the hasShirt and the previous line of code (which branches after hasPants).
            // hasPants alone will then determine if the pawn is PsychologicallyNude.

            int pantsTestLine = -1;

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldloc_0) // hasPants
                {
                    if (codes[i + 2].opcode == OpCodes.Ldloc_1) // hasShirt
                    {
                        pantsTestLine = i;
                        break;
                    }
                }
            }
            if (pantsTestLine > -1)
            {
                Log.Message("Free the Nipple: The nipple has been freed");
                codes.RemoveRange(pantsTestLine + 1, 2);
            }
            else
            {
                Log.Warning("Free The Nipple: Failed to remove female shirt test");
            }
            return codes.AsEnumerable();
        }
    }
}
