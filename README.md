# Cyberpsycho (사이버사이코)

A RimWorld **1.6** mod. Installing cybernetic implants raises a colonist's
**cyber strain**. Push it past their personal threshold and they snap into
**cyberpsychosis** — a Berserk-style rage with massively boosted combat stats
that attacks everything in sight. Craft and dose the **Cyber-Suppressant** to
hold strain down, Luciferium-style; skip doses and strain surges.

Inspired by Cyberpunk 2077's cyberpsychosis.

## How it plays

1. Install bionics / archotech parts / brain implants → each adds cyber strain.
2. Strain climbs over time; the more (and higher-tech) the cybernetics, the faster.
3. A per-colonist **gauge** (on the pawn) and a **warning alert** track the risk.
4. Research **Cyber-Stabilization** → craft **Cyber-Suppressant** at a drug lab.
5. Dosing pushes strain down but creates a Luciferium-style **dependence**:
   miss your doses and strain surges, often straight into cyberpsychosis.
6. Over the threshold, an MTB roll triggers **cyberpsychosis**: +50% move speed,
   ×2 melee damage, ignores pain, attacks any living thing it can reach.

## Implementation map (matches the design plan)

| Stage | What | Where |
|-------|------|-------|
| 1 — Data structures | `CyberStrain` Hediff, thresholds, `CyberwareExtension` ModExtension | `Defs/HediffDefs/Hediffs_CyberStrain.xml`, `Source/CyberPsycho/CyberwareExtension.cs` |
| 2 — Strain on install | Harmony postfix on `Hediff.PostAdd`, per-tick recompute | `Source/CyberPsycho/Patch_HediffPostAdd.cs`, `HediffComp_CyberStrain.cs` |
| 3 — Threshold + trigger | Interval check, MTB probability, start mental state | `HediffComp_CyberStrain.cs`, `CyberStrainUtility.cs` |
| 4 — Psychosis behavior | `CyberPsychosis` MentalStateDef + Berserk AI + stat-boost Hediff | `Defs/MentalStateDefs/...`, `Patches/ThinkTree_CyberPsychosis.xml`, `MentalState_CyberPsychosis.cs` |
| 5 — Suppressant drug | DrugDef, ChemicalDef/NeedDef, addiction Hediffs | `Defs/ThingDefs_Items/Drug_CyberSuppressant.xml`, `Defs/ChemicalDefs/...`, `Defs/HediffDefs/Hediffs_Suppressant.xml` |
| 6 — UI & feedback | On-pawn gauge gizmo, alert, health-tab bar | `Gizmo_CyberStrain.cs`, `Patch_PawnGizmos.cs`, `Alert_CyberPsychosisRisk.cs` |
| 7 — Balancing & content | Per-pawn threshold variance, trait, psychopath tie-in, research, mod settings | `CyberStrainUtility.cs`, `Defs/TraitDefs/...`, `Defs/ResearchProjectDefs/...`, `CyberPsychoMod.cs` |

## Requirements

- RimWorld 1.6
- [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) (`brrainz.harmony`)

## Install / enable

This repo is already symlinked into your RimWorld `Mods/` folder as
`Cyberpsycho`. In game: **Options → Mods**, enable **Harmony** then
**Cyberpsycho** (Harmony first), and restart.

To remove the symlink:
```
rm "$HOME/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/Cyberpsycho"
```

## Building the DLL

Requires the .NET SDK. From the repo root:
```
dotnet build Source/CyberPsycho/CyberPsycho.csproj -c Release
```
The assembly is written to `Assemblies/CyberPsycho.dll`. The project references
your local RimWorld 1.6 assemblies; override the path with
`-p:RimWorldManaged=/path/to/Data/Managed` if you move the game.

## Testing in-game (dev mode)

Enable **Development mode** (Options). Then:
- Select a colonist → the **cyber strain** gauge appears once they have cyberware.
- Install a bionic via surgery, or use the **debug tools** (god mode) to add the
  `CyberStrain` Hediff / set its severity, or directly start the **CyberPsychosis**
  mental state from the pawn's dev "mental state" menu to see the rage + stat boost.

## Tuning

- Per-implant cyber values: `Patches/Cyberware_Extensions.xml`.
- Rates, thresholds, MTB: the `<comps>` block in `Defs/HediffDefs/Hediffs_CyberStrain.xml`.
- Global multipliers: **Options → Mod settings → Cyberpsycho**.

## Extending to modded cyberware

Add this to any implant's `HediffDef` (or a patch targeting it):
```xml
<modExtensions>
  <li Class="CyberPsycho.CyberwareExtension">
    <cyberValue>0.12</cyberValue>
  </li>
</modExtensions>
```

## Notes

- The Cyber-Suppressant reuses the vanilla Luciferium pill icon so it has art out
  of the box. Drop your own texture at `Textures/Things/Item/Drug/CyberSuppressant`
  and point `texPath` at it.
- By default only colony pawns (colonists, prisoners, slaves) build strain.
