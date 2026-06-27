using RimWorld;
using Verse;
using Verse.AI;

namespace CyberPsycho
{
    /// <summary>
    /// STAGE 5 — auto-medication. Vanilla drug policies don't know about cyber
    /// strain, so colonists won't dose themselves before snapping. This think-tree
    /// node makes a free colonist walk over and take a cyber-suppressant once their
    /// strain climbs within a margin of their personal breaking point — provided
    /// the drug is available, they aren't already suppressed, and tolerance hasn't
    /// rendered it useless. Injected near the top of the Humanlike think tree.
    /// </summary>
    public class JobGiver_TakeCyberSuppressant : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            var s = CyberPsychoMod.Settings;
            if (s == null || !s.autoDoseSuppressant) return null;

            if (pawn == null || !pawn.Spawned || pawn.Downed || pawn.Drafted) return null;
            if (!pawn.IsColonistPlayerControlled) return null;
            if (pawn.InMentalState) return null;

            var comp = CyberStrainUtility.StrainComp(pawn);
            if (comp == null) return null;

            // Already dosed → nothing to do.
            if (pawn.health.hediffSet.HasHediff(CyberPsychoDefOf.CyberSuppressantHigh)) return null;

            // Only when strain is within the margin below the breaking point.
            if (comp.Severity < comp.Threshold - s.autoDoseMargin) return null;

            // Don't waste pills once tolerance has made the drug useless.
            if (CyberStrainUtility.ToleranceLevel(pawn) >= comp.Props.toleranceFailPoint) return null;

            Thing drug = FindSuppressant(pawn);
            if (drug == null) return null;

            Job job = JobMaker.MakeJob(JobDefOf.Ingest, drug);
            job.count = 1;
            return job;
        }

        private static Thing FindSuppressant(Pawn pawn)
        {
            bool Validator(Thing t) =>
                !t.IsForbidden(pawn) && !t.IsBurning() && pawn.CanReserve(t);

            return GenClosest.ClosestThingReachable(
                pawn.Position, pawn.Map,
                ThingRequest.ForDef(CyberPsychoDefOf.CyberSuppressant),
                PathEndMode.OnCell, TraverseParms.For(pawn),
                9999f, Validator);
        }
    }
}
