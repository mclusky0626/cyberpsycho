using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace CyberPsycho
{
    /// <summary>
    /// STAGE 4 — ranged combat layer for a cyberpsycho.
    ///
    /// This node ONLY takes over when the pawn has a usable ranged weapon and a
    /// clean (or reachable) shot. In every other case — melee weapon, fists,
    /// can't-shoot, no firing position, or any error — it returns null so the
    /// vanilla JobGiver_Berserk that sits below it in the think tree handles the
    /// pawn (proven melee behaviour). That keeps the worst case at "charges and
    /// melees" instead of "stands frozen".
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

        protected override Job TryGiveJob(Pawn pawn)
        {
            try
            {
                // Can't shoot at all (e.g. incapable of Violence) → let Berserk run.
                if (pawn.WorkTagIsDisabled(WorkTags.Violent)) return null;

                Thing target = FindTarget(pawn);
                if (target == null) return null;

                Verb verb = pawn.TryGetAttackVerb(target, !pawn.IsColonist);
                if (verb == null || verb.IsMeleeAttack) return null; // melee/unarmed → Berserk

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

                // Nowhere good to shoot from → defer to Berserk (it closes in / melees).
                return null;
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

        private static Thing FindTarget(Pawn pawn)
        {
            const TargetScanFlags flags = TargetScanFlags.NeedLOSToPawns
                                          | TargetScanFlags.NeedReachableIfCantHitFromMyPos
                                          | TargetScanFlags.NeedThreat;
            return (Thing)AttackTargetFinder.BestAttackTarget(
                pawn, flags,
                t => (t is Pawn p && !p.Dead && !p.Downed) || t is Building_Turret,
                0f, AcquireRadius);
        }
    }
}
