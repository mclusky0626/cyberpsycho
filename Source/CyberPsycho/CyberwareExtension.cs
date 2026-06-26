using Verse;

namespace CyberPsycho
{
    /// <summary>
    /// STAGE 1 — ModExtension attached to implant HediffDefs (see
    /// Patches/Cyberware_Extensions.xml). Its <c>cyberValue</c> is how much
    /// cyber strain pressure that implant contributes. Add this same element
    /// to any modded implant's HediffDef to make it count.
    /// </summary>
    public class CyberwareExtension : DefModExtension
    {
        public float cyberValue = 0.1f;
    }
}
