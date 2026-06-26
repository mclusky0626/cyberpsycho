using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace CyberPsycho
{
    /// <summary>STAGE 6 — high-priority alert when a colonist nears their breaking point.</summary>
    public class Alert_CyberPsychosisRisk : Alert
    {
        private readonly List<Pawn> culprits = new List<Pawn>();

        public Alert_CyberPsychosisRisk()
        {
            defaultLabel = "CyberPsycho.Alert.Label".Translate();
            defaultPriority = AlertPriority.High;
        }

        private List<Pawn> AtRisk()
        {
            culprits.Clear();
            var maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                var pawns = maps[m].mapPawns.FreeColonistsAndPrisonersSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    var comp = CyberStrainUtility.StrainComp(pawns[i]);
                    if (comp != null && comp.NearThreshold)
                        culprits.Add(pawns[i]);
                }
            }
            return culprits;
        }

        public override AlertReport GetReport() => AlertReport.CulpritsAre(AtRisk());

        public override TaggedString GetExplanation()
        {
            var sb = new StringBuilder();
            foreach (var p in AtRisk())
                sb.AppendLine("  - " + p.LabelShortCap);
            return "CyberPsycho.Alert.Explanation".Translate(sb.ToString().TrimEndNewlines());
        }
    }
}
