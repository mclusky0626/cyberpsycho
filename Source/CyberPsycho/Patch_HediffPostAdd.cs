using HarmonyLib;
using Verse;

namespace CyberPsycho
{
    /// <summary>
    /// STAGE 2 — install hook. Whenever any cyberware hediff is added to a
    /// colony pawn, make sure they carry a CyberStrain tracker and apply the
    /// small install spike. Covers surgery, scenarios and pawn generation.
    /// </summary>
    [HarmonyPatch(typeof(Hediff), nameof(Hediff.PostAdd))]
    public static class Patch_Hediff_PostAdd
    {
        public static void Postfix(Hediff __instance)
        {
            HediffDef def = __instance.def;
            if (def == null) return;

            Pawn pawn = __instance.pawn;
            if (!CyberStrainUtility.IsColonyPawn(pawn)) return;

            // A suppressant dose builds tolerance on the strain tracker.
            if (def == CyberPsychoDefOf.CyberSuppressantHigh)
            {
                CyberStrainUtility.StrainComp(pawn)?.Notify_SuppressantDose();
                return;
            }

            // Cyberware install: ensure the strain tracker and apply the spike.
            if (CyberStrainUtility.CyberValueOf(def) <= 0f) return;
            CyberStrainUtility.EnsureStrainHediff(pawn);
            CyberStrainUtility.StrainComp(pawn)?.Notify_CyberwareInstalled();
        }
    }
}
