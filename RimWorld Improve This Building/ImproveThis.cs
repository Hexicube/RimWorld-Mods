using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorld___Improve_This {
    public class WorkGiver_ImproveThis : WorkGiver_Scanner {
        private static JobDef ImproveThisJobDef = DefDatabase<JobDef>.GetNamed("Improve");

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) {
            if (t.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null) return false;
            if (t.Map.designationManager.DesignationOn(t, DesignationDefOf.Uninstall) != null) return false;
            ImproveThisComp c = t.TryGetComp<ImproveThisComp>();
            if (c != null) {
                if (c.improveRequested) {
                    return JobOnThing(pawn, t, forced) != null;
                }
            }
            return false;
        }

        private static bool ResourceValidator(Pawn pawn, ThingDefCountClass need, Thing th)
        {
            if (th.def != need.thingDef)
            {
                return false;
            }
            if (th.IsForbidden(pawn))
            {
                return false;
            }
            if (!pawn.CanReserve(th))
            {
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
            ImproveThisComp c = t.TryGetComp<ImproveThisComp>();
            if (c == null || !c.improveRequested) return null;
            List<ThingDefCountClass> mats = c.MaterialsNeeded().FindAll(
                m => m.count > 0
            );
            if (mats.Count > 0) {
                // needs more materials
                foreach (ThingDefCountClass mat in mats) {
                    if (pawn.Map.itemAvailability.ThingsAvailableAnywhere(mat, pawn)) {
                        Thing found = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(mat.thingDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, (Thing r) => ResourceValidator(pawn, mat, r));
                        if (found != null) {
                            Job job = JobMaker.MakeJob(JobDefOf.HaulToContainer);
                            job.targetA = found;
                            job.targetB = t;
                            job.count = mat.count;
                            job.haulMode = HaulMode.ToContainer;
                            return job;
                        }
                    }
                }
                JobFailReason.Is($"{"MissingMaterials".Translate()}: {mats[0].thingDef.label}");
                return null;
            }
            // needs work done
            Job j = JobMaker.MakeJob(ImproveThisJobDef, t);
            if (j.TryMakePreToilReservations(pawn, false)) return j;
            return null;
        }
    }

    public class JobDriver_ImproveThis : JobDriver {
        private ImproveThisComp JobTarget => ((Thing)job.GetTarget(TargetIndex.A)).TryGetComp<ImproveThisComp>();

        public override bool TryMakePreToilReservations(bool errorOnFailed) {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils() {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.A);
            Toil build = new Toil();
            build.initAction = delegate {
                GenClamor.DoClamor(build.actor, 15f, ClamorDefOf.Construction);
            };
            build.tickAction = delegate {
                Pawn actor = build.actor;
                ImproveThisComp comp = JobTarget;
                actor.skills.Learn(SkillDefOf.Construction, 0.25f);
                float speed = actor.GetStatValue(StatDefOf.ConstructionSpeed) * 1.7f;
                speed *= comp.parent.Stuff.GetStatValueAbstract(StatDefOf.ConstructionSpeedFactor);
                float workToBuild = comp.WorkToBuild;
                if (actor.Faction == Faction.OfPlayer) {
                    float statValue = actor.GetStatValue(StatDefOf.ConstructSuccessChance);
                    if (!TutorSystem.TutorialMode && Rand.Value < 1f - UnityEngine.Mathf.Pow(statValue, speed / workToBuild)) {
                        comp.FailConstruction(actor);
                        ReadyForNextToil();
                        return;
                    }
                }
                comp.workDone += speed;
                if (comp.WorkLeft <= 0) {
                    comp.CompleteConstruction(actor);
                    ReadyForNextToil();
                }
            };
            build.WithEffect(base.TargetThingA.def.repairEffect, TargetIndex.A);
            build.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            build.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            build.FailOn(() => !GenConstruct.CanConstruct(JobTarget.parent, pawn));
            build.WithProgressBar(TargetIndex.A, () => (float)JobTarget.workDone / (float)JobTarget.WorkToBuild);
            build.defaultCompleteMode = ToilCompleteMode.Delay;
            build.defaultDuration = 5000;
            build.activeSkill = () => SkillDefOf.Construction;
            yield return build;
        }
    }

    public class ImproveThisComp : ThingComp, IThingHolder, IConstructible {
        // TODO: store items
        // TODO: handle work

        // IThingHolder

        public void GetChildHolders(List<IThingHolder> outChildren) {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }
        public ThingOwner contents;
        public ThingOwner GetDirectlyHeldThings() {
            if (contents == null) contents = new ThingOwner<Thing>(this, false);
            return contents;
        }

        // IConstructible

        public ThingDef EntityToBuildStuff() {
            return parent.Stuff;
        }

        private List<ThingDefCountClass> cachedMaterialsNeeded = new List<ThingDefCountClass>();
        public List<ThingDefCountClass> MaterialsNeeded() {
            cachedMaterialsNeeded.Clear();
            float returned = parent.def.resourcesFractionWhenDeconstructed;
            List<ThingDefCountClass> list = parent.def.CostListAdjusted(parent.Stuff);
            for (int i = 0; i < list.Count; i++)
            {
                ThingDefCountClass thingDefCountClass = list[i];
                int req = thingDefCountClass.count - (int)(thingDefCountClass.count * returned);
                int num = GetDirectlyHeldThings().TotalStackCountOfDef(thingDefCountClass.thingDef);
                int num2 = req - num;
                if (num2 > 0) cachedMaterialsNeeded.Add(new ThingDefCountClass(thingDefCountClass.thingDef, num2));
            }
            return cachedMaterialsNeeded;
        }

        // useful numbers

        public float workDone;
        public float WorkToBuild => parent.def.GetStatValueAbstract(StatDefOf.WorkToBuild, parent.Stuff);
        public float WorkLeft => WorkToBuild - workDone;

        public bool improveRequested;

        // everything else

        public override void PostExposeData() {
            base.PostExposeData();
            Scribe_Values.Look(ref improveRequested, "improve", false);
            Scribe_Values.Look(ref workDone, "workDone", 0f);
            Scribe_Deep.Look(ref contents, "contents", this);
        }

        public override string CompInspectStringExtra() {
            System.Text.StringBuilder str = new System.Text.StringBuilder();
            str.Append(base.CompInspectStringExtra());
            if (!improveRequested) return str.ToString();
            str.AppendLineIfNotEmpty();

            str.AppendLine("ContainedResources".Translate() + ":");
            List<ThingDefCountClass> list = parent.def.CostListAdjusted(parent.Stuff);
            List<ThingDefCountClass> needed = MaterialsNeeded();
            float returned = parent.def.resourcesFractionWhenDeconstructed;
            bool satisfied = true;
            for (int i = 0; i < list.Count; i++)
            {
                ThingDefCountClass need = list[i];
                int needCount = need.count - (int)(need.count * returned);
                int num = needCount;
                foreach (ThingDefCountClass item in needed) {
                    if (item.thingDef == need.thingDef) num -= item.count;
                }
                if (num < needCount) satisfied = false;
                str.AppendLine((need.thingDef.LabelCap + ": ") + num + " / " + needCount);
            }
            if (satisfied) str.AppendLine("WorkLeft".Translate() + ": " + UnityEngine.Mathf.CeilToInt(WorkLeft / 60f));

            return str.ToString();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra() {
            Command btn = GetCommandButton();
            if (btn == null) yield break;
            yield return (Gizmo)btn;
        }

        private Command GetCommandButton() {
            CompQuality compQuality = parent.TryGetComp<CompQuality>();
            if (compQuality == null || compQuality.Quality == QualityCategory.Legendary) return null;

            Command_Toggle cmd = new Command_Toggle();
            cmd.isActive = delegate() { return improveRequested; };
            cmd.toggleAction = delegate() {
                if (improveRequested) {
                    improveRequested = false;
                    GetDirectlyHeldThings().TryDropAll(parent.Position, parent.Map, ThingPlaceMode.Near);
                }
                else {
                    improveRequested = true;
                }
            };
            cmd.defaultLabel = "ImproveLabel".Translate();
            cmd.defaultDesc = "ImproveDesc".Translate();
            cmd.icon = ContentFinder<UnityEngine.Texture2D>.Get("Improve", true);
            return cmd;
        }

        public void CompleteConstruction(Pawn worker) {
            workDone = 0;
            GetDirectlyHeldThings().ClearAndDestroyContents();
            CompQuality compQuality = parent.TryGetComp<CompQuality>();
            if (compQuality == null) {
                Log.Error("Attempted to ImproveThis on a building without quality!");
                return;
            }
            QualityCategory q = QualityUtility.GenerateQualityCreatedByPawn(worker, SkillDefOf.Construction);
            if (q.CompareTo(compQuality.Quality) <= 0) {
                Log.Message("ImproveThis on " + parent.Label + "failed: " + q + " was not an improvement over " + compQuality.Quality + ".");
                MoteMaker.ThrowText(parent.DrawPos, parent.Map, "MessageQualityTooLow".Translate(q.GetLabel()), 6f);
                return;
            }
            compQuality.SetQuality(q, ArtGenerationContext.Colony);
            QualityUtility.SendCraftNotification(parent, worker);
            CompArt art = parent.TryGetComp<CompArt>();
            if (art != null) {
                if (art.CanShowArt && !art.Active) {
                    art.InitializeArt(ArtGenerationContext.Colony);
                    art.JustCreatedBy(worker);
                }
            }
            improveRequested = false;
        }

        public void FailConstruction(Pawn worker) {
            workDone = 0;
            GetDirectlyHeldThings().ClearAndDestroyContents();
            Map map = parent.Map;
            MoteMaker.ThrowText(parent.DrawPos, map, "TextMote_ConstructionFail".Translate(), 6f);
            if (parent.Faction == Faction.OfPlayer && WorkToBuild > 1400f) {
                Messages.Message("MessageConstructionFailed".Translate(parent.Label, worker.LabelShort, worker.Named("WORKER")), new TargetInfo(parent.Position, map), MessageTypeDefOf.NegativeEvent);
            }
        }
    }
}
