using Verse;

namespace CyberPsycho
{
    /// <summary>
    /// Safety net that runs outside the hediff tick loop: makes sure cyberware
    /// owners have a strain tracker (covers mid-save installs and joining pawns)
    /// and strips any leftover combat-surge hediff from pawns that aren't raging.
    /// Auto-registered by RimWorld for every game.
    /// </summary>
    public class GameComponent_CyberPsycho : GameComponent
    {
        private const int ScanInterval = 600;
        private int nextScanTick;

        public GameComponent_CyberPsycho(Game game) { }

        public override void GameComponentTick()
        {
            int now = Find.TickManager.TicksGame;
            if (now < nextScanTick) return;
            nextScanTick = now + ScanInterval;

            var maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                var pawns = maps[m].mapPawns.FreeColonistsAndPrisonersSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (!CyberStrainUtility.IsColonyPawn(pawn)) continue;

                    CyberStrainUtility.EnsureStrainHediff(pawn);

                    if (pawn.MentalStateDef != CyberPsychoDefOf.CyberPsychosis)
                    {
                        var boost = pawn.health.hediffSet.GetFirstHediffOfDef(CyberPsychoDefOf.CyberPsychosisBoost);
                        if (boost != null) pawn.health.RemoveHediff(boost);
                    }
                }
            }
        }
    }
}
