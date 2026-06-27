using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CyberPsycho
{
    public class CyberPsychoSettings : ModSettings
    {
        public float riskMultiplier = 1f;
        public float thresholdOffset = 0f;
        public bool affectsPrisoners = true;
        public bool countAllImplants = true;
        public float autoImplantValue = 0.30f;
        public bool autoDoseSuppressant = true;
        public float autoDoseMargin = 0.20f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref riskMultiplier, "riskMultiplier", 1f);
            Scribe_Values.Look(ref thresholdOffset, "thresholdOffset", 0f);
            Scribe_Values.Look(ref affectsPrisoners, "affectsPrisoners", true);
            Scribe_Values.Look(ref countAllImplants, "countAllImplants", true);
            Scribe_Values.Look(ref autoImplantValue, "autoImplantValue", 0.30f);
            Scribe_Values.Look(ref autoDoseSuppressant, "autoDoseSuppressant", true);
            Scribe_Values.Look(ref autoDoseMargin, "autoDoseMargin", 0.20f);
            base.ExposeData();
        }
    }

    public class CyberPsychoMod : Mod
    {
        public static CyberPsychoSettings Settings;

        public CyberPsychoMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<CyberPsychoSettings>();
            var harmony = new Harmony("jjomang.cyberpsycho");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override string SettingsCategory() => "Cyberpsycho";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var l = new Listing_Standard();
            l.Begin(inRect);

            l.Label("CyberPsycho.Settings.RiskMultiplier".Translate(Settings.riskMultiplier.ToStringPercent()));
            Settings.riskMultiplier = Mathf.Round(l.Slider(Settings.riskMultiplier, 0.1f, 3f) * 20f) / 20f;
            l.Gap();

            l.Label("CyberPsycho.Settings.ThresholdOffset".Translate(
                Settings.thresholdOffset.ToStringByStyle(ToStringStyle.FloatTwo, ToStringNumberSense.Offset)));
            Settings.thresholdOffset = Mathf.Round(l.Slider(Settings.thresholdOffset, -0.4f, 0.4f) * 100f) / 100f;
            l.Gap();

            l.CheckboxLabeled("CyberPsycho.Settings.AffectsPrisoners".Translate(), ref Settings.affectsPrisoners);
            l.Gap();

            l.CheckboxLabeled("CyberPsycho.Settings.CountAllImplants".Translate(), ref Settings.countAllImplants);
            if (Settings.countAllImplants)
            {
                l.Label("CyberPsycho.Settings.AutoImplantValue".Translate(Settings.autoImplantValue.ToStringPercent()));
                Settings.autoImplantValue = Mathf.Round(l.Slider(Settings.autoImplantValue, 0.01f, 0.4f) * 100f) / 100f;
            }
            l.Gap();

            l.CheckboxLabeled("CyberPsycho.Settings.AutoDose".Translate(), ref Settings.autoDoseSuppressant);
            if (Settings.autoDoseSuppressant)
            {
                l.Label("CyberPsycho.Settings.AutoDoseMargin".Translate(Settings.autoDoseMargin.ToStringPercent()));
                Settings.autoDoseMargin = Mathf.Round(l.Slider(Settings.autoDoseMargin, 0.05f, 0.5f) * 100f) / 100f;
            }

            l.End();
        }
    }
}
