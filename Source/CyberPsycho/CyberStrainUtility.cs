using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CyberPsycho
{
    public static class CyberStrainUtility
    {
        /// <summary>Pawns we track strain for: colony-relevant humanlikes.</summary>
        public static bool IsColonyPawn(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.health == null) return false;
            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike) return false;
            if (pawn.IsColonist || pawn.IsSlaveOfColony) return true;
            if (pawn.IsPrisonerOfColony)
                return CyberPsychoMod.Settings == null || CyberPsychoMod.Settings.affectsPrisoners;
            return false;
        }

        public static float CyberValueOf(HediffDef def)
        {
            if (def == null) return 0f;

            // Explicit per-implant value wins (vanilla parts patched in XML, or any
            // modded implant a user/modder tags with CyberwareExtension).
            var ext = def.GetModExtension<CyberwareExtension>();
            if (ext != null) return ext.cyberValue;

            // Otherwise, optionally treat any added body part / implant as cyberware
            // so modded bionics (EPOE, cyberpunk packs, etc.) count automatically,
            // scaled by how advanced the implant is.
            var s = CyberPsychoMod.Settings;
            if (s != null && s.countAllImplants && def.countsAsAddedPartOrImplant)
                return s.autoImplantValue * TierMultiplier(def);

            return 0f;
        }

        /// <summary>Better/more advanced implants strain the mind harder.</summary>
        private static float TierMultiplier(HediffDef def)
        {
            string n = (def.defName + " " + (def.label ?? "")).ToLowerInvariant();
            if (n.Contains("archotech") || n.Contains("archo")) return 2.2f;
            if (n.Contains("advanced") || n.Contains("ultra")) return 1.5f;

            // Fall back to the tech level of the item that installs the part.
            ThingDef item = def.spawnThingOnRemoved;
            if (item != null)
            {
                switch (item.techLevel)
                {
                    case TechLevel.Archotech: return 2.2f;
                    case TechLevel.Ultra: return 1.5f;
                    case TechLevel.Spacer: return 1.0f;
                    case TechLevel.Industrial: return 0.5f;
                    default: return 0.4f;
                }
            }

            if (n.Contains("simple") || n.Contains("prosthetic") || n.Contains("peg")
                || n.Contains("wooden") || n.Contains("denture"))
                return 0.4f;

            return 1.0f; // unknown modded implant: treat as standard bionic grade
        }

        /// <summary>
        /// How fast strain rises for this pawn. The Chosen accumulate very slowly
        /// so even heavy augmentation rarely pushes them to their high breaking point.
        /// </summary>
        public static float StrainRateMultiplierFor(Pawn pawn)
        {
            var traits = pawn?.story?.traits;
            if (traits != null && traits.HasTrait(CyberPsychoDefOf.CyberChosen))
                return 0.3f;
            return 1f;
        }

        /// <summary>Current suppressant tolerance (0..1); 0 if none built up.</summary>
        public static float ToleranceLevel(Pawn pawn)
        {
            var h = pawn?.health?.hediffSet?.GetFirstHediffOfDef(CyberPsychoDefOf.CyberSuppressantTolerance);
            return h?.Severity ?? 0f;
        }

        public static Hediff GetOrAddTolerance(Pawn pawn)
        {
            if (pawn?.health == null) return null;
            var h = pawn.health.hediffSet.GetFirstHediffOfDef(CyberPsychoDefOf.CyberSuppressantTolerance);
            if (h == null) h = pawn.health.AddHediff(CyberPsychoDefOf.CyberSuppressantTolerance);
            return h;
        }

        /// <summary>Sum of cyber value across all of a pawn's installed cybernetics.</summary>
        public static float TotalCyberLoad(Pawn pawn)
        {
            var hediffs = pawn?.health?.hediffSet?.hediffs;
            if (hediffs == null) return 0f;
            float sum = 0f;
            for (int i = 0; i < hediffs.Count; i++)
                sum += CyberValueOf(hediffs[i].def);
            return sum;
        }

        public static bool HasAnyCyberware(Pawn pawn)
        {
            var hediffs = pawn?.health?.hediffSet?.hediffs;
            if (hediffs == null) return false;
            for (int i = 0; i < hediffs.Count; i++)
                if (CyberValueOf(hediffs[i].def) > 0f) return true;
            return false;
        }

        /// <summary>Add the CyberStrain tracker if the pawn has cyberware and lacks one.</summary>
        public static Hediff EnsureStrainHediff(Pawn pawn)
        {
            if (!IsColonyPawn(pawn)) return null;
            var h = pawn.health.hediffSet.GetFirstHediffOfDef(CyberPsychoDefOf.CyberStrain);
            if (h == null && HasAnyCyberware(pawn))
                h = pawn.health.AddHediff(CyberPsychoDefOf.CyberStrain);
            return h;
        }

        public static HediffComp_CyberStrain StrainComp(Pawn pawn)
        {
            var h = pawn?.health?.hediffSet?.GetFirstHediffOfDef(CyberPsychoDefOf.CyberStrain);
            return (h as HediffWithComps)?.TryGetComp<HediffComp_CyberStrain>();
        }

        /// <summary>Per-pawn breaking point: base ± deterministic variance, traits, settings.</summary>
        public static float ThresholdFor(Pawn pawn, HediffCompProperties_CyberStrain props)
        {
            float t = props.baseThreshold;

            // Deterministic per-pawn variance — stable without storing extra save data.
            float variance = ((pawn.thingIDNumber % 1000) / 1000f - 0.5f) * 2f * props.thresholdVariance;
            t += variance;

            var traits = pawn.story?.traits;
            if (traits != null)
            {
                var cyber = traits.GetTrait(CyberPsychoDefOf.CyberStability);
                if (cyber != null) t += cyber.Degree * 0.15f;
                if (traits.HasTrait(TraitDefOf.Psychopath)) t += 0.10f;   // 사회병질자: more at home as a machine
                if (traits.HasTrait(TraitDefOf.Bloodlust)) t -= 0.05f;
                // "The Chosen": breaking point sits ~140 points above an ordinary
                // pawn (≈2.25, just below the 2.35 ceiling) — they can run almost
                // unlimited chrome before psychosis is even a risk.
                if (traits.HasTrait(CyberPsychoDefOf.CyberChosen)) t += 1.40f;
            }

            if (CyberPsychoMod.Settings != null) t += CyberPsychoMod.Settings.thresholdOffset;
            return Mathf.Clamp(t, 0.2f, 2.4f);
        }

        public static void StartCyberPsychosis(Pawn pawn, string reason)
        {
            if (pawn?.mindState?.mentalStateHandler == null) return;
            if (pawn.InMentalState) return;

            // Pawns who can't do violence can't actually fight (the engine blocks
            // attack jobs and errors on shooting stats), so an episode would just
            // be a pointless wander + burnout. Skip them entirely.
            if (pawn.WorkTagIsDisabled(WorkTags.Violent)) return;

            pawn.mindState.mentalStateHandler.TryStartMentalState(CyberPsychoDefOf.CyberPsychosis, reason);
        }
    }
}
