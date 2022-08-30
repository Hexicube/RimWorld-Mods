using HarmonyLib;
using RimWorld;
using Verse;

namespace RimWorld___No_Passionate_Skill_Decay
{
    // TODO: split xp gains into temporary and permanent xp?

    public class NSDPMod : Mod {
        public static NSDPSettings settings;
        public NSDPMod(ModContentPack content) : base(content) {
            settings = GetSettings<NSDPSettings>();
        }

        public override string SettingsCategory() {
            return "No Passionate Skill Decay";
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect canvas) {
            settings.DoWindowsContents(canvas);
        }
    }

    public class NSDPSettings : ModSettings {
        public int FullPassionCutoff = 20; private string FullPassionEntry = "20";
        public int HalfPassionCutoff = 17; private string HalfPassionEntry = "17";
        public int   NoPassionCutoff = 14; private string   NoPassionEntry = "14";

        public bool AboveDecay = true;
        public bool BelowDecay = false;
        public bool MemoryOverride = false;

        public AboveLimitBehaviour AboveBehave = AboveLimitBehaviour.NoLevelCap;

        public void DoWindowsContents(UnityEngine.Rect canvas) {
            Listing_Standard list = new Listing_Standard { ColumnWidth = 800f };
            list.Begin(canvas);
            list.Label("LevelLimits".Translate());
            list.ColumnWidth = 300f;
            list.TextFieldNumericLabeled(  "NoPassion".Translate(), ref   NoPassionCutoff, ref   NoPassionEntry, 1);
            list.TextFieldNumericLabeled("HalfPassion".Translate(), ref HalfPassionCutoff, ref HalfPassionEntry, 1);
            list.TextFieldNumericLabeled("FullPassion".Translate(), ref FullPassionCutoff, ref FullPassionEntry, 1);
            list.ColumnWidth = 800f;
            list.Label("LevelLimitsNote".Translate());
            list.Gap(20);
            list.Label("XPDecay".Translate());
            list.ColumnWidth = 300f;
            list.CheckboxLabeled("BelowDecay".Translate(), ref BelowDecay);
            list.CheckboxLabeled("AboveDecay".Translate(), ref AboveDecay);
            list.ColumnWidth = 800f;
            list.Label("XPDecayNote".Translate());
            list.Gap(20);
            list.Label("CapStyle".Translate());
            list.Indent(12);
            list.ColumnWidth = 288f;
            foreach (AboveLimitBehaviour val in System.Enum.GetValues(typeof(AboveLimitBehaviour))) {
                if (list.RadioButton(val.ToString().Translate(), AboveBehave == val)) AboveBehave = val;
            }
            list.ColumnWidth = 800f;
            list.Label((AboveBehave.ToString() + "Note").Translate());
            list.Indent(-12);
            list.Label("CapStyleNote".Translate());
            list.Gap(20);
            list.Label("BetterGoodMemoryNote".Translate());
            list.ColumnWidth = 300f;
            list.CheckboxLabeled("BetterGoodMemory".Translate(), ref MemoryOverride);
            list.ColumnWidth = 800f;
            list.Label(("BetterGoodMemoryNote" + AboveBehave.ToString()).Translate());
            list.End();
        }

        public override void ExposeData() {
            Scribe_Values.Look(ref FullPassionCutoff, "full_passion", 20, false); FullPassionEntry = FullPassionCutoff.ToString();
            Scribe_Values.Look(ref HalfPassionCutoff, "half_passion", 17, false); HalfPassionEntry = HalfPassionCutoff.ToString();
            Scribe_Values.Look(ref   NoPassionCutoff,   "no_passion", 14, false);   NoPassionEntry =   NoPassionCutoff.ToString();
            Scribe_Values.Look(ref AboveDecay, "above_decay", true, false);
            Scribe_Values.Look(ref BelowDecay, "below_decay", false, false);
            Scribe_Values.Look(ref AboveBehave, "above_style", AboveLimitBehaviour.NoLevelCap, false);
            Scribe_Values.Look(ref MemoryOverride, "mem_override", false, false);
            if (FullPassionCutoff == 0 && HalfPassionCutoff == 0 && NoPassionCutoff == 0) {
                Log.Message("[No Passionate Skill Decay] Detected broken defaults, setting proper defaults...");
                FullPassionCutoff = 20; FullPassionEntry = "20";
                HalfPassionCutoff = 17; HalfPassionEntry = "17";
                  NoPassionCutoff = 14;   NoPassionEntry = "14";
                AboveDecay = true;
                BelowDecay = false;
                AboveBehave = AboveLimitBehaviour.NoLevelCap;
                MemoryOverride = false;
            }
        }
    }

    public enum AboveLimitBehaviour {
        NoLevelCap, SoftLevelCap, HardLevelCap
    }

    [StaticConstructorOnStartup]
    public class NSDPHarmonyLoader {
        static NSDPHarmonyLoader() {
            Harmony h = new Harmony("hexi.skillpassion");
            h.Patch(AccessTools.Method(typeof(SkillRecord), "Learn"), new HarmonyMethod(typeof(NoSkillDecayPassion).GetMethod("PrefixLearn")));
        }
    }
    
    public class NoSkillDecayPassion {
        private static System.Reflection.FieldInfo PawnField = AccessTools.Field(typeof(SkillRecord), "pawn");
        public static bool PrefixLearn(SkillRecord __instance, ref float xp, bool direct) {
            if (direct) return true; // probably skill learners

            int level = __instance.Level;
            bool atLimit, exactlyAtLimit;
            //Log.Message("[No Passionate Skill Decay] DEBUG LV" + level);
            switch (__instance.passion) {
                case Passion.None: {
                    atLimit = level >= NSDPMod.settings.NoPassionCutoff;
                    exactlyAtLimit = level == NSDPMod.settings.NoPassionCutoff;
                    break;
                }
                case Passion.Minor: {
                    atLimit = level >= NSDPMod.settings.HalfPassionCutoff;
                    exactlyAtLimit = level == NSDPMod.settings.HalfPassionCutoff;
                    break;
                }
                case Passion.Major: {
                    atLimit = level >= NSDPMod.settings.FullPassionCutoff;
                    exactlyAtLimit = level == NSDPMod.settings.FullPassionCutoff;
                    break;
                }
                default: {
                    // unusual skill - assume that LearnRateFactor is somewhat representative of how passionate the pawn is relative to standard ones
                    // we lerp against the two appropriate values
                    // assumption: configured cutoffs increase with better passions
                    // vanilla: 0.35, 1.00, 1.50
                    float rate = __instance.LearnRateFactor();
                    int half = NSDPMod.settings.HalfPassionCutoff;
                    int cutoffEstimate;
                    if (rate > 1f) {
                        // at least minor passion
                        float lerp = (rate - 1f) / 0.5f;
                        int full = NSDPMod.settings.FullPassionCutoff;
                        cutoffEstimate = (int)System.Math.Ceiling((float)half + (float)(full - half) * lerp);
                    }
                    else {
                        // worse than minor passion
                        float lerp = (rate - 0.35f) / 0.65f;
                        int none = NSDPMod.settings.NoPassionCutoff;
                        cutoffEstimate = (int)System.Math.Floor((float)none + (float)(half - none) * lerp);
                    }
                    if (cutoffEstimate < 0) cutoffEstimate = 0;
                    else if (cutoffEstimate > 20) cutoffEstimate = 20;
                    atLimit = level >= cutoffEstimate;
                    exactlyAtLimit = level == cutoffEstimate;
                    break;
                }
            }
            //Log.Message("[No Passionate Skill Decay] DEBUG Passion is " + __instance.passion);
            //Log.Message("[No Passionate Skill Decay] DEBUG At Limit:" + atLimit);
            //Log.Message("[No Passionate Skill Decay] DEBUG At Limit (Exact):" + exactlyAtLimit);
            float factor;
            if (xp > 0) factor = __instance.LearnRateFactor(false);
            else factor = 1;
            if (atLimit) {
                if (xp > 0) {
                    //Log.Message("[No Passionate Skill Decay] DEBUG At Limit, XP>0: " + NSDPMod.settings.AboveBehave);
                    switch (NSDPMod.settings.AboveBehave) {
                        case AboveLimitBehaviour.NoLevelCap: {
                            if (NSDPMod.settings.MemoryOverride) {
                                if (!((Pawn)PawnField.GetValue(__instance)).story.traits.HasTrait(TraitDefOf.GreatMemory)) xp /= 2;
                            }
                            return true;
                        }
                        case AboveLimitBehaviour.SoftLevelCap: {
                            float remain = __instance.XpRequiredForLevelUp - __instance.xpSinceLastLevel - 1;
                            if (remain <= 0) return false;

                            if (NSDPMod.settings.MemoryOverride) {
                                if (!((Pawn)PawnField.GetValue(__instance)).story.traits.HasTrait(TraitDefOf.GreatMemory)) xp /= 2;
                            }
                            if (remain < (xp * factor)) xp = remain / factor;
                            return true;
                        }
                        case AboveLimitBehaviour.HardLevelCap: {
                            if (NSDPMod.settings.MemoryOverride) {
                                if (((Pawn)PawnField.GetValue(__instance)).story.traits.HasTrait(TraitDefOf.GreatMemory)) {
                                    xp /= 2;
                                    return true;
                                }
                            }
                            return false;
                        }
                        default: return true;
                    }
                }
                else {
                    //Log.Message("[No Passionate Skill Decay] DEBUG At Limit, XP<0: " + NSDPMod.settings.AboveDecay);
                    if (!NSDPMod.settings.AboveDecay) return false;
                    if (!exactlyAtLimit) return true;
                    float remain = __instance.xpSinceLastLevel;
                    if ((remain + (xp * factor)) < 0) xp = -remain / factor;
                    return true;
                }
            }
            else if (xp < 0) {
                //Log.Message("[No Passionate Skill Decay] DEBUG Below Limit, XP<0: " + NSDPMod.settings.BelowDecay);
                return NSDPMod.settings.BelowDecay;
            }
            else return true;
        }
    }
}
