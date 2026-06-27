using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace CyberPsycho
{
    /// <summary>
    /// STAGE 4 — combat target layer for a cyberpsycho.
    ///
    /// This node keeps target choice inside the mod for both ranged and melee
    /// combat, so cyberpsychos do not drift into neutral animals while a better
    /// humanlike or revenge target is available.
    ///
    /// Why not vanilla JobGiver_AIFightEnemies? Without a Lord/duty (which mental
    /// states lack) it re-issues the attack every think cycle, so the warmup
    /// never finishes and the pawn just raises and lowers the gun. Here we only
    /// fire when CanHitTarget is already true, and otherwise move to a firing
    /// position, so the shot actually goes off.
    /// </summary>
    public class JobGiver_CyberPsychoAssault : ThinkNode_JobGiver
    {
        private const float AcquireRadius = 60f;
        private const float RevengeRadius = 80f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            try
            {
                // Incapable of Violence: no attack job can run.
                if (pawn.WorkTagIsDisabled(WorkTags.Violent)) return null;

                Thing target = FindTarget(pawn);
                if (target == null) return null;

                Verb verb = pawn.TryGetAttackVerb(target, !pawn.IsColonist);
                if (verb == null) return null;
                if (verb.IsMeleeAttack) return MeleeJob(target);

                // Clean shot from here: fire, and let the aim warmup finish.
                if (verb.CanHitTarget(target))
                    return RangedJob(target, verb);

                // No shot from here: walk to a real firing position, shoot next cycle.
                CastPositionRequest req = default;
                req.caster = pawn;
                req.target = target;
                req.verb = verb;
                req.maxRangeFromTarget = verb.verbProps.range;
                req.wantCoverFromTarget = false;

                if (CastPositionFinder.TryFindCastPosition(req, out IntVec3 castPos)
                    && castPos.IsValid && castPos != pawn.Position)
                {
                    Job go = JobMaker.MakeJob(JobDefOf.Goto, castPos);
                    go.expiryInterval = 200;
                    go.checkOverrideOnExpire = true;
                    go.collideWithPawns = true;
                    return go;
                }

                // Nowhere good to shoot from: close in on the same target instead
                // of letting vanilla Berserk pick a random animal or neutral pawn.
                return MeleeJob(target);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Cyberpsycho] assault JobGiver failed, falling back to Berserk: " + e,
                    93371 + (pawn?.thingIDNumber ?? 0));
                return null;
            }
        }

        private static Job RangedJob(Thing target, Verb verb)
        {
            Job job = JobMaker.MakeJob(JobDefOf.AttackStatic, target);
            job.verbToUse = verb;
            job.killIncappedTarget = true;
            job.endIfCantShootInMelee = false;
            job.expiryInterval = Rand.RangeInclusive(240, 360);
            job.checkOverrideOnExpire = true;
            return job;
        }

        private static Job MeleeJob(Thing target)
        {
            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            job.killIncappedTarget = true;
            job.attackDoorIfTargetLost = true;
            job.expiryInterval = Rand.RangeInclusive(300, 480);
            job.checkOverrideOnExpire = true;
            return job;
        }

        private static Thing FindTarget(Pawn pawn)
        {
            var state = pawn.MentalState as MentalState_CyberPsychosis;
            Thing priorityTarget = state?.PriorityTarget;
            bool priorityIsRevenge = state?.HasRevengeTarget(priorityTarget) == true;
            if (IsValidTarget(pawn, priorityTarget, priorityIsRevenge)
                && (priorityIsRevenge || IsHumanlikePawn(priorityTarget)))
                return priorityTarget;

            Thing humanTarget = FindBestTarget(pawn, humansOnly: true);
            if (humanTarget != null)
            {
                state?.Notify_TargetChosen(humanTarget);
                return humanTarget;
            }

            if (IsValidTarget(pawn, priorityTarget, revengeTarget: false))
                return priorityTarget;

            Thing fallbackTarget = FindBestTarget(pawn, humansOnly: false);
            if (fallbackTarget != null)
                state?.Notify_TargetChosen(fallbackTarget);
            return fallbackTarget;
        }

        private static Thing FindBestTarget(Pawn pawn, bool humansOnly)
        {
            const TargetScanFlags flags = TargetScanFlags.NeedLOSToPawns
                                          | TargetScanFlags.NeedReachableIfCantHitFromMyPos;
            IAttackTarget target = AttackTargetFinder.BestAttackTarget(
                pawn, flags,
                t => IsValidTarget(pawn, t, revengeTarget: false)
                     && (!(t is Pawn p) || p.RaceProps.Humanlike == humansOnly),
                0f, AcquireRadius);
            return target?.Thing;
        }

        private static bool IsValidTarget(Pawn pawn, Thing target, bool revengeTarget)
        {
            if (pawn == null || target == null || target == pawn || target.Destroyed || !target.Spawned)
                return false;
            if (target.Map != pawn.Map)
                return false;

            float radius = revengeTarget ? RevengeRadius : AcquireRadius;
            if (target.Position.DistanceTo(pawn.Position) > radius)
                return false;

            if (target is Pawn targetPawn)
            {
                if (targetPawn.Dead || targetPawn.Downed)
                    return false;
                return revengeTarget || targetPawn.RaceProps.Humanlike || targetPawn.HostileTo(pawn);
            }

            return target is Building_Turret;
        }

        private static bool IsHumanlikePawn(Thing target)
        {
            return target is Pawn pawn && pawn.RaceProps.Humanlike;
        }
    }
}
