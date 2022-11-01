using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HediffHUD
{
    [StaticConstructorOnStartup]
    public static class HediffHudMod
    {
        static HediffHudMod()
        {
            new Harmony("Hexi.HediffHud").PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(HealthCardUtility), "VisibleHediffs")]
    public static class HediffHudMod_VisibleHediffPostfix
    {
        [HarmonyPostfix]
        private static IEnumerable<Hediff> VisibleHediffPostfix(
            IEnumerable<Hediff> returned,
            Pawn pawn, bool showBloodLoss)
        {
            var hasHediffAlready = new List<BodyPartRecord>();
            foreach (Hediff h in returned)
            {
                if (!hasHediffAlready.Contains(h.Part)) hasHediffAlready.Add(h.Part);
                yield return h; // things already shown - use this?
            }
            
            var theList = new List<BodyPartDef>();
            var theListGroup = new List<BodyPartGroupDef>();
            foreach (RecipeDef recipe in pawn.def.AllRecipes)
            {
                if (recipe.appliedOnFixedBodyPartGroups != null)
                {
                    foreach (var bodyGroup in recipe.appliedOnFixedBodyPartGroups)
                    {
                        if (!theListGroup.Contains(bodyGroup)) theListGroup.Add(bodyGroup);
                    }
                }
                if (recipe.appliedOnFixedBodyParts != null)
                {
                    foreach (BodyPartDef bodyPartDef in recipe.appliedOnFixedBodyParts)
                    {
                        if (!theList.Contains(bodyPartDef))
                        {
                            theList.Add(bodyPartDef);
                        }
                    }
                }
            }
            foreach (BodyPartRecord partRecord in pawn.def.race.body.AllParts)
            {
                if (pawn.health.hediffSet.PartIsMissing(partRecord)) continue; // exists
                if (hasHediffAlready.Contains(partRecord)) continue; // not already showing
                if (!theList.Contains(partRecord.def) && !partRecord.groups.Any(it => theListGroup.Contains(it))) continue; // has an operation
                yield return HediffDefCanUpgrade.MakeHediff(pawn, partRecord);
            }
        }
    }
    // TODO: postfix VisibleHediffs to show replaceable parts

    public class HediffDefCanUpgrade : HediffDef
    {
        public static Hediff MakeHediff(Pawn pawn, BodyPartRecord record)
        {
            Hediff h = new Hediff();
            h.pawn = pawn;
            h.def = new HediffDefCanUpgrade();
            h.Part = record;
            return h;
        }
        
        public HediffDefCanUpgrade()
        {
            isBad = false;
            makesAlert = false;
            everCurableByItem = false;
            description = "";
        }
    }
}