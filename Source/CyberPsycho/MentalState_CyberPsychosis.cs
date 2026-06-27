using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CyberPsycho
{
    /// <summary>
    /// STAGE 4/5 — a cyberpsycho prefers humanlike targets and locks onto
    /// anyone who hurts it, using its equipped weapon while
    /// the CyberPsychosisBoost hediff is active: super-speed, doubled melee,
    /// sharpshooter aim, pain ignored. When the episode ends the body pays for
    /// the overclock (neural burnout + exhaustion) and the mind carries the
    /// memory; nearby colonists are shaken by what they saw.
    /// </summary>
    public class MentalState_CyberPsychosis : MentalState_Berserk
    {
        private const float WitnessRadius = 30f;
        private const int RevengeLockTicks = 900;
        private const int TargetLockTicks = 420;

        private Thing revengeTarget;
        private int revengeTargetUntilTick = -1;
        private Thing currentTarget;
        private int currentTargetUntilTick = -1;

        public Thing PriorityTarget
        {
            get
            {
                int ticks = Find.TickManager?.TicksGame ?? 0;
                if (revengeTarget != null && ticks <= revengeTargetUntilTick)
                    return revengeTarget;
                if (currentTarget != null && ticks <= currentTargetUntilTick)
                    return currentTarget;
                return null;
            }
        }

        public bool HasRevengeTarget(Thing target)
        {
            int ticks = Find.TickManager?.TicksGame ?? 0;
            return target != null && target == revengeTarget && ticks <= revengeTargetUntilTick;
        }

        public void Notify_TookDamageFrom(Thing attacker)
        {
            if (attacker == null || attacker == pawn || attacker.Destroyed) return;

            int ticks = Find.TickManager?.TicksGame ?? 0;
            if (revengeTarget != null && revengeTarget != attacker && ticks <= revengeTargetUntilTick)
                return;

            revengeTarget = attacker;
            revengeTargetUntilTick = ticks + RevengeLockTicks;
            currentTarget = attacker;
            currentTargetUntilTick = revengeTargetUntilTick;
        }

        public void Notify_TargetChosen(Thing target)
        {
            if (target == null || HasRevengeTarget(target)) return;

            currentTarget = target;
            currentTargetUntilTick = (Find.TickManager?.TicksGame ?? 0) + TargetLockTicks;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref revengeTarget, "revengeTarget");
            Scribe_Values.Look(ref revengeTargetUntilTick, "revengeTargetUntilTick", -1);
            Scribe_References.Look(ref currentTarget, "currentTarget");
            Scribe_Values.Look(ref currentTargetUntilTick, "currentTargetUntilTick", -1);
        }

        public override void PostStart(string reason)
        {
            base.PostStart(reason);
            if (pawn?.health == null) return;
            if (!pawn.health.hediffSet.HasHediff(CyberPsychoDefOf.CyberPsychosisBoost))
                pawn.health.AddHediff(CyberPsychoDefOf.CyberPsychosisBoost);
        }

        public override void PostEnd()
        {
            base.PostEnd();
            RemoveBoost();

            ApplyBurnout();
            ApplyTrauma();
            NotifyWitnesses();

            CyberStrainUtility.StrainComp(pawn)?.Notify_EpisodeEnded();
        }

        private void RemoveBoost()
        {
            var boost = pawn?.health?.hediffSet?.GetFirstHediffOfDef(CyberPsychoDefOf.CyberPsychosisBoost);
            if (boost != null) pawn.health.RemoveHediff(boost);
        }

        /// <summary>The body crashes after running its cybernetics past redline.</summary>
        private void ApplyBurnout()
        {
            if (pawn?.health == null || pawn.Dead) return;
            if (!pawn.health.hediffSet.HasHediff(CyberPsychoDefOf.CyberBurnout))
                pawn.health.AddHediff(CyberPsychoDefOf.CyberBurnout);

            // Drained: the rampage burned through the pawn's reserves.
            var rest = pawn.needs?.rest;
            if (rest != null) rest.CurLevel = 0.05f;
            var food = pawn.needs?.food;
            if (food != null) food.CurLevel *= 0.4f;
        }

        /// <summary>The pawn remembers losing control (psychopaths shrug it off).</summary>
        private void ApplyTrauma()
        {
            var memories = pawn?.needs?.mood?.thoughts?.memories;
            memories?.TryGainMemory(CyberPsychoDefOf.CyberPsychoticEpisode);
        }

        /// <summary>Colonists who saw the rampage are disturbed.</summary>
        private void NotifyWitnesses()
        {
            Map map = pawn?.MapHeld;
            if (map == null) return;

            List<Pawn> colonists = map.mapPawns.FreeColonistsAndPrisonersSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn witness = colonists[i];
                if (witness == pawn || witness.Dead || witness.needs?.mood == null) continue;
                if (witness.Position.DistanceTo(pawn.Position) > WitnessRadius) continue;
                if (!GenSight.LineOfSight(witness.Position, pawn.Position, map)) continue;

                witness.needs.mood.thoughts.memories.TryGainMemory(CyberPsychoDefOf.WitnessedCyberpsycho);
            }
        }
    }
}
