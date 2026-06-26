using UnityEngine;
using Verse;

namespace CyberPsycho
{
    /// <summary>STAGE 6 — on-pawn cyber strain gauge with a threshold marker.</summary>
    public class Gizmo_CyberStrain : Gizmo
    {
        private readonly HediffComp_CyberStrain comp;

        public Gizmo_CyberStrain(HediffComp_CyberStrain comp)
        {
            this.comp = comp;
            Order = -45f;
        }

        public override float GetWidth(float maxWidth) => Mathf.Min(150f, maxWidth);

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(rect);
            Rect inner = rect.ContractedBy(7f);

            Text.Font = GameFont.Tiny;
            Rect title = new Rect(inner.x, inner.y, inner.width, 18f);
            Widgets.Label(title, "CyberPsycho.Gizmo.Label".Translate());

            float max = Mathf.Max(0.01f, comp.MaxForBar);
            float pct = Mathf.Clamp01(comp.Severity / max);
            float threshPct = Mathf.Clamp01(comp.Threshold / max);

            Rect bar = new Rect(inner.x, title.yMax + 3f, inner.width, 16f);
            Widgets.DrawBoxSolid(bar, new Color(0.08f, 0.08f, 0.08f, 0.9f));
            Widgets.DrawBoxSolid(new Rect(bar.x, bar.y, bar.width * pct, bar.height),
                BarColor(comp.Severity / Mathf.Max(0.01f, comp.Threshold)));

            float markX = bar.x + bar.width * threshPct;
            GUI.color = new Color(1f, 0.35f, 0.35f);
            Widgets.DrawLineVertical(markX, bar.y - 2f, bar.height + 4f);
            GUI.color = Color.white;

            Rect read = new Rect(inner.x, bar.yMax + 3f, inner.width, 18f);
            Widgets.Label(read, comp.Severity.ToStringPercent() + " / " + comp.Threshold.ToStringPercent());
            Text.Font = GameFont.Small;

            TooltipHandler.TipRegion(rect, () => "CyberPsycho.Gizmo.Tooltip".Translate(
                comp.Severity.ToStringPercent(), comp.Threshold.ToStringPercent(), comp.CyberLoad.ToStringPercent()),
                117428);

            return new GizmoResult(GizmoState.Clear);
        }

        private static Color BarColor(float frac)
        {
            if (frac >= 1f) return new Color(1f, 0.2f, 0.2f);
            if (frac >= 0.7f) return new Color(0.95f, 0.7f, 0.2f);
            return new Color(0.3f, 0.75f, 0.95f);
        }
    }
}
