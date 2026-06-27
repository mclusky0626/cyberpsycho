using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CyberPsycho
{
    /// <summary>
    /// Targeting layer for the neural-quell lance: only a pawn currently in the
    /// throes of a cyberpsychotic episode is a valid target, so the single-use
    /// device can't be wasted on anyone else.
    /// </summary>
    public class CompTargetable_Cyberpsycho : CompTargetable_SinglePawn
    {
        protected override TargetingParameters GetTargetingParameters()
        {
            return new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                canTargetAnimals = false,
                validator = (TargetInfo x) =>
                    x.Thing is Pawn p && !p.Dead
                    && p.MentalStateDef == CyberPsychoDefOf.CyberPsychosis
            };
        }
    }

    public class CompProperties_NeuralQuell : CompProperties
    {
        public CompProperties_NeuralQuell()
        {
            compClass = typeof(CompTargetEffect_NeuralQuell);
        }
    }

    /// <summary>
    /// Effect of the neural-quell lance. Ends the cyberpsychotic episode (which,
    /// via MentalState_CyberPsychosis.PostEnd, already applies neural burnout,
    /// trauma and strain relief) and then exacts the price of a forced shutdown:
    /// the pawn is knocked out cold and left with permanent neural scarring that
    /// stacks with every forced quell.
    /// </summary>
    public class CompTargetEffect_NeuralQuell : CompTargetEffect
    {
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (!(target is Pawn p) || p.Dead || p.health == null) return;

            // End the episode — PostEnd handles burnout / trauma / relief.
            if (p.MentalStateDef == CyberPsychoDefOf.CyberPsychosis)
                p.MentalState?.RecoverFromState();

            // The forced quell's price: out cold + permanent scarring.
            if (!p.health.hediffSet.HasHediff(HediffDefOf.Anesthetic))
                p.health.AddHediff(HediffDefOf.Anesthetic);
            p.health.AddHediff(CyberPsychoDefOf.NeuralScar);

            if (p.Spawned)
                MoteMaker.ThrowText(p.DrawPos, p.Map, "CyberPsycho.Quelled".Translate(), 3.65f);
        }
    }
}
