using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using OpCodes = System.Reflection.Emit.OpCodes;


namespace FreeTheNipple
{

    // HugsLib log observations, just for my own reference:
    // This is just a map of which code strings appear where in a HugsLib log:
    // 
    // Loaded mods:
    // Free the Nipple(OkraDonkey.FreeTheNipple): FreeTheNipple(av:1.0.0.2,fv:1.0.0.3)
    // <Title from About.xml>(<packageId from About.xml>): <AssemblyName from project properties>(av:<AssemblyVersion from project properties>,fv:<AssemblyFileVersion from project properties>)
    // 
    // Active Harmony patches:
    // Pawn_ApparelTracker.get_PsychologicallyNude: TRANS: FreeTheNipple.Remove_Shirt_Test.Transpiler
    // <vanillaClass.vanillaMethod>: <PatchType>: <namespace>.<patchClassName>.<patchEnumerableName>

    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            // Harmony_id is only used by other patches that might want to load before/after this one
            Harmony harmonyInstance = new Harmony(id: "RimWorld.OkraDonkey.FreeTheNipple.main");
            harmonyInstance.PatchAll();
        }
    }

    // Targeting Pawn_ApparelTracker.PsychologicallyNude
    // PsychologicallyNude is a property, so specify MethodType.Getter
    // (It's actually get_PsychologicallyNude)
    [HarmonyPatch(typeof(Pawn_ApparelTracker), "PsychologicallyNude", MethodType.Getter)]
    public static class Remove_Shirt_Test
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {

            // Transpiler overview:
            //   The vanilla code checks males for pants, and females for pants then shirts.
            //   Instead of rewriting the entire section, we'll just remove the shirt-test for females.
            //   There will still be separate tests for males and females, but they will be identical.
            //
            // IL Code Observations:
            //   At the beginning of the PsychologicallyNude method, two local booleans are created: hasPants and hasShirt.
            //   These are then assigned values by the HasBasicApparel(hasPants, hasShirt) method
            //   In IL code, these variables are then recalled as follows:
            //   ldloc.0 --> hasPants
            //   ldloc.1 --> hasShirt
            //
            // Strategy:
            //   Find the hasPants with a hasShirt two IL lines later.
            //   Remove the hasShirt and the previous line of code (which would branch if hasPants was false).
            //   hasPants alone will then determine if the pawn is PsychologicallyNude.
            //   (The short branch to which hasPants=false would have diverted will no longer have an entry point.)

            // Set a marker so we can tell if we've found our target, and if so, where
            int pantsTestLine = -1;

            // Load all the IL lines (from the vanilla method) and go through them one by one
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldloc_0) // Does this line recall the local variable hasPants?
                {
                    if (codes[i + 2].opcode == OpCodes.Ldloc_1) // Does this later line recall the local variable hasShirt?
                    {
                        // If yes and yes, we've identified our target
                        pantsTestLine = i;
                        break;
                    }
                }
            }
            if (pantsTestLine > -1)
            {
                // We found what we were looking for; let's remove those two IL lines
                Log.Message("Free the Nipple: The nipple has been freed");
                codes.RemoveRange(pantsTestLine + 1, 2);
            }
            else
            {
                // Something went wrong and we didn't find the shirt-test
                Log.Warning("Free The Nipple: Failed to remove female shirt-test");
            }
            // We've either removed our two lines, or made no changes
            // Now we teturn the IL codes to Harmony for reinsertion
            return codes.AsEnumerable();
        }
    }
}
