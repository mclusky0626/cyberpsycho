using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace CyberPsycho
{
    /// <summary>STAGE 6 — append the strain gauge to a controllable colonist's gizmos.</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (var g in __result)
                yield return g;

            if (__instance == null || !__instance.IsColonistPlayerControlled)
                yield break;

            var comp = CyberStrainUtility.StrainComp(__instance);
            if (comp != null)
                yield return new Gizmo_CyberStrain(comp);
        }
    }
}
