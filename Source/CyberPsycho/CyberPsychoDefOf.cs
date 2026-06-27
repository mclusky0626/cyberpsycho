using RimWorld;
using Verse;
using Verse.AI;

namespace CyberPsycho
{
    [DefOf]
    public static class CyberPsychoDefOf
    {
        public static HediffDef CyberStrain;
        public static HediffDef CyberPsychosisBoost;
        public static HediffDef CyberBurnout;
        public static HediffDef NeuralScar;
        public static HediffDef CyberSuppressantHigh;
        public static HediffDef CyberSuppressantAddiction;
        public static HediffDef CyberSuppressantTolerance;

        public static ThingDef CyberSuppressant;

        public static MentalStateDef CyberPsychosis;

        public static TraitDef CyberStability;
        public static TraitDef CyberChosen;

        public static ThoughtDef CyberPsychoticEpisode;
        public static ThoughtDef WitnessedCyberpsycho;

        static CyberPsychoDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CyberPsychoDefOf));
        }
    }
}
