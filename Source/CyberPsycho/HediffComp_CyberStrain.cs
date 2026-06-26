using UnityEngine;
using Verse;

namespace CyberPsycho
{
    public class HediffCompProperties_CyberStrain : HediffCompProperties
    {
        public int intervalTicks = 250;
        public float riskPerCyberValuePerDay = 0.5f;
        public float suppressedFallPerDay = 0.6f;
        public float baselineFallPerDay = 0.08f;
        public float withdrawalSurgeFactor = 2.5f;
        public float installSpike = 0.04f;
        public float baseThreshold = 0.85f;
        public float thresholdVariance = 0.08f;
        public float mtbDaysAtThreshold = 1.2f;
        public float mtbDaysAtMax = 0.15f;
        public float mtbRampSpan = 0.55f;
        public float episodeReliefFactor = 0.4f;
        public float warnDelta = 0.10f;
        public float tolerancePerDose = 0.08f;
        public float toleranceFailPoint = 0.85f;

        public HediffCompProperties_CyberStrain()
        {
            compClass = typeof(HediffComp_CyberStrain);
        }
    }

    /// <summary>
    /// STAGE 2/3 — the brain of the mod. Every <c>intervalTicks</c> it sums the
    /// pawn's installed cyber value, drifts strain up (or down, if suppressed),
    /// and once over the pawn's threshold rolls an MTB chance for cyberpsychosis.
    /// </summary>
    public class HediffComp_CyberStrain : HediffComp
    {
        public HediffCompProperties_CyberStrain Props => (HediffCompProperties_CyberStrain)props;

        private float cachedThreshold = -1f;

        public float Severity => parent.Severity;
        public float MaxForBar => parent.def.maxSeverity;
        public float CyberLoad => CyberStrainUtility.TotalCyberLoad(parent.pawn);

        public float Threshold
        {
            get
            {
                if (cachedThreshold < 0f)
                    cachedThreshold = CyberStrainUtility.ThresholdFor(parent.pawn, Props);
                return cachedThreshold;
            }
        }

        public bool NearThreshold =>
            parent.Severity >= Threshold - Props.warnDelta && !parent.pawn.InMentalState;

        public void Notify_CyberwareInstalled()
        {
            parent.Severity += Props.installSpike;
            cachedThreshold = -1f; // a new implant can come with a trait/anatomy change
        }

        /// <summary>Catharsis after an episode so they don't instantly re-trigger.</summary>
        public void Notify_EpisodeEnded()
        {
            parent.Severity *= Props.episodeReliefFactor;
        }

        /// <summary>Each suppressant dose builds tolerance, eroding its effect.</summary>
        public void Notify_SuppressantDose()
        {
            var tol = CyberStrainUtility.GetOrAddTolerance(parent.pawn);
            if (tol != null)
                tol.Severity = Mathf.Min(tol.Severity + Props.tolerancePerDose, 1f);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            Pawn pawn = parent.pawn;
            if (pawn == null || !pawn.Spawned) return;
            if (!pawn.IsHashIntervalTick(Props.intervalTicks)) return;

            float load = CyberLoad;
            bool suppressed = pawn.health.hediffSet.HasHediff(CyberPsychoDefOf.CyberSuppressantHigh);
            bool dependent = pawn.health.hediffSet.HasHediff(CyberPsychoDefOf.CyberSuppressantAddiction);

            // Drift rate (per day).
            float perDay;
            if (suppressed)
            {
                // Suppressant works less the more tolerance has built up; past the
                // fail point it stops working entirely and strain climbs anyway.
                float tol = CyberStrainUtility.ToleranceLevel(pawn);
                if (tol >= Props.toleranceFailPoint)
                    perDay = load * Props.riskPerCyberValuePerDay * 0.5f; // drug overwhelmed
                else
                    perDay = -Props.suppressedFallPerDay * (1f - tol);    // dosed: strain bleeds off
            }
            else if (load <= 0f)
                perDay = -Props.baselineFallPerDay;              // no implants: slowly recover
            else
            {
                perDay = load * Props.riskPerCyberValuePerDay;   // implants push strain up
                if (dependent) perDay *= Props.withdrawalSurgeFactor; // missed doses → surge
            }

            float mult = CyberPsychoMod.Settings != null ? CyberPsychoMod.Settings.riskMultiplier : 1f;
            if (perDay > 0f) perDay *= mult * CyberStrainUtility.StrainRateMultiplierFor(pawn);

            parent.Severity = Mathf.Clamp(
                parent.Severity + perDay * (Props.intervalTicks / 60000f),
                0f, parent.def.maxSeverity);

            // STAGE 3 — threshold detection + probability roll.
            if (parent.Severity >= Threshold && !pawn.InMentalState && pawn.RaceProps.Humanlike)
            {
                float over = Mathf.Clamp01(
                    (parent.Severity - Threshold) / Mathf.Max(0.01f, Props.mtbRampSpan));
                float mtb = Mathf.Lerp(Props.mtbDaysAtThreshold, Props.mtbDaysAtMax, over);
                if (Rand.MTBEventOccurs(mtb, 60000f, Props.intervalTicks))
                    CyberStrainUtility.StartCyberPsychosis(pawn, "CyberPsycho.PsychosisReason".Translate());
            }
        }

        // Auto-remove the tracker once strain is gone and no cyberware remains.
        // Severity check short-circuits so the load scan only runs when near zero.
        public override bool CompShouldRemove =>
            base.CompShouldRemove ||
            (parent.Severity <= 0.0001f && CyberStrainUtility.TotalCyberLoad(parent.pawn) <= 0f);

        public override string CompTipStringExtra
        {
            get
            {
                if (parent.pawn == null) return null;
                return "CyberPsycho.StrainTip".Translate(
                    Severity.ToStringPercent(),
                    Threshold.ToStringPercent(),
                    CyberLoad.ToStringPercent());
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref cachedThreshold, "cp_cachedThreshold", -1f);
        }
    }
}
