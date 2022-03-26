using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace RimWorld___Better_Infections
{
    public class BetInfMod : Mod {
        public BetInfMod(ModContentPack content) : base(content) {
        }
    }

    [StaticConstructorOnStartup]
    public class BetInfHarmonyLoader {
        static BetInfHarmonyLoader() {
            Harmony h = new Harmony("hexi.infection");
            h.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), "HealthTick"), new HarmonyMethod(typeof(BetterInfections).GetMethod("PrefixHealthTick")));
        }
    }
    
    public class BetterInfections {
        private static System.Reflection.FieldInfo NoPainField = AccessTools.Field(typeof(Hediff), "causesNoPain");
        private static (Hediff, bool) FindOrMakePermRot(List<Hediff> set, Hediff src) {
            foreach (Hediff h in set) {
                if (h.def != null &&
                    h.sourceHediffDef != null &&
                    h.sourceHediffDef.Equals(src.def) &&
                    h.Part == src.Part) return (h, false);
            }
            HediffWithComps he = HediffMaker.MakeHediff(HediffDefOf.Misc, src.pawn, src.Part) as HediffWithComps;
            he.sourceHediffDef = src.def;
            he.Severity = 0;
            HediffComp_GetsPermanent comp = he.TryGetComp<HediffComp_GetsPermanent>();
            if (comp != null) comp.IsPermanent = true;
            NoPainField.SetValue(he, true);
            return (he, true);
        }

        private const float SCAR_START = 0.90f; // infections hit final stage at 87%, needs 89% to give headroom; rounded up
        private const float SCAR_END   = 0.95f; // infections will always scar before 95%
        private const float SCAR_FACTOR = 1 / (SCAR_END - SCAR_START);
        public static bool PrefixHealthTick(Pawn_HealthTracker __instance) {
            List<Hediff> newScars = new List<Hediff>();
            __instance.hediffSet.hediffs.ForEach(delegate(Hediff h) {
                // look for not-immune infections
                if (h.def.Equals(HediffDefOf.WoundInfection)) {
                    if (!h.FullyImmune()) {
                        // check the severity
                        float s = h.Severity / h.def.lethalSeverity;
                        float permOdds = 0f;
                        if (s >= SCAR_END) permOdds = 1f; // below math isn't a true guarantee, caps at 0.5%/tick
                        else if (s >= SCAR_START) {
                            permOdds = (s - SCAR_START) * SCAR_FACTOR; // 0-1 range
                            // power 1.5
                            permOdds = (permOdds * permOdds * permOdds) / (permOdds * permOdds);
                            // flat mult because of tick rate
                            permOdds *= 0.005f;
                        }

                        if (permOdds == 1f || Rand.Chance(permOdds)) {
                            (Hediff, bool) rotData = FindOrMakePermRot(__instance.hediffSet.hediffs, h);
                            float max = h.Part.def.GetMaxHealth(h.pawn);
                            if (rotData.Item1.Severity < max) {
                                Log.Message("Odds passed and scar isn't maxed, scarring...");

                                int p = Rand.RangeInclusive(5, 20);
                                float f = rotData.Item1.Severity + p * 0.1f;
                                if (f >= max) {
                                    f = max;
                                    Log.Message("Scar is maxed out (" + max + "), infection will be lethal.");
                                }
                                Log.Message("Pawn: " + h.pawn.Label + ", Body part: " + h.Part.Label + ", Infection progress: " + s + ", Tick odds: " + permOdds + ", Scar: " + rotData.Item1.Severity + "->" + f);

                                rotData.Item1.Severity = f;
                                h.Severity -= h.def.lethalSeverity * 0.02f;
                                __instance.Notify_HediffChanged(h);
                                if (rotData.Item2) newScars.Add(rotData.Item1);
                            }
                        }
                    }
                }
            });
            newScars.ForEach(delegate(Hediff h) { h.pawn.health.AddHediff(h, h.Part); });
            return true;
        }
    }
}
