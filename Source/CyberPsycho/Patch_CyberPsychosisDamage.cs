using System.Reflection;
using HarmonyLib;
using Verse;

namespace CyberPsycho
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PostApplyDamage))]
    public static class Patch_Pawn_HealthTracker_PostApplyDamage
    {
        private static readonly FieldInfo PawnField = AccessTools.Field(typeof(Pawn_HealthTracker), "pawn");

        public static void Postfix(Pawn_HealthTracker __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (totalDamageDealt <= 0f) return;

            Pawn victim = PawnField?.GetValue(__instance) as Pawn;
            if (!(victim?.MentalState is MentalState_CyberPsychosis state)) return;

            Thing attacker = dinfo.Instigator;
            if (attacker == null || attacker == victim) return;
            state.Notify_TookDamageFrom(attacker);
        }
    }
}
