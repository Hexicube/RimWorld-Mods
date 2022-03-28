using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorld___Improve_This {
    public class Designator_ImproveThis : Designator {
        public static DesignationDef ImproveDesignationDef = DefDatabase<DesignationDef>.GetNamed("ImproveThis", true);

        public Designator_ImproveThis() {
            defaultLabel = "DesignateOnLabel".Translate();
            defaultDesc = "DesignateOnDesc".Translate();
            icon = ContentFinder<UnityEngine.Texture2D>.Get("Improve");
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Haul;
        }

        public override int DraggableDimensions => 2;

        public override AcceptanceReport CanDesignateCell(IntVec3 p) {
            Map map = Find.CurrentMap;
            if (!GenGrid.InBounds(p, map) || GridsUtility.Fogged(p, map)) return AcceptanceReport.WasRejected;
            List<Thing> things = map.thingGrid.ThingsListAt(p);
            if (things.Any ( (Thing t) => CanDesignateThing(t))) return AcceptanceReport.WasAccepted;
            return AcceptanceReport.WasRejected;
        }
        public override void DesignateSingleCell(IntVec3 p) {
            Map map = Find.CurrentMap;
            if (GenGrid.InBounds(p, map) && !GridsUtility.Fogged(p, map)) {
                List<Thing> things = map.thingGrid.ThingsListAt(p);
                things.ForEach( (Thing t) => DesignateThing(t));
            }
        }

        public override AcceptanceReport CanDesignateThing(Thing t) {
            ImproveThisComp c = t.TryGetComp<ImproveThisComp>();
            if (c == null || c.improveRequested) return AcceptanceReport.WasRejected;
            CompQuality q = t.TryGetComp<CompQuality>();
            if (q == null || q.Quality == QualityCategory.Legendary) return AcceptanceReport.WasRejected;
            if (t.def.blueprintDef == null) return AcceptanceReport.WasRejected; // probably piano and such
            return AcceptanceReport.WasAccepted;
        }

        public override void DesignateThing(Thing t) {
            ImproveThisComp c = t.TryGetComp<ImproveThisComp>();
            if (c == null || c.improveRequested) return;
            CompQuality q = t.TryGetComp<CompQuality>();
            if (q == null || q.Quality == QualityCategory.Legendary) return;
            if (t.def.blueprintDef == null) return; // probably piano and such
            c.improveRequested = true;
        }
    }

    public class Designator_ImproveThisClear : Designator {
        public Designator_ImproveThisClear() {
            defaultLabel = "DesignateOffLabel".Translate();
            defaultDesc = "DesignateOffDesc".Translate();
            icon = ContentFinder<UnityEngine.Texture2D>.Get("NoImprove");
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_Haul;
        }

        public override int DraggableDimensions => 2;

        public override AcceptanceReport CanDesignateCell(IntVec3 p) {
            Map map = Find.CurrentMap;
            if (!GenGrid.InBounds(p, map) || GridsUtility.Fogged(p, map)) return AcceptanceReport.WasRejected;
            List<Thing> things = map.thingGrid.ThingsListAt(p);
            if (things.Any ( (Thing t) => CanDesignateThing(t))) return AcceptanceReport.WasAccepted;
            return AcceptanceReport.WasRejected;
        }
        public override void DesignateSingleCell(IntVec3 p) {
            Map map = Find.CurrentMap;
            if (GenGrid.InBounds(p, map) && !GridsUtility.Fogged(p, map)) {
                List<Thing> things = map.thingGrid.ThingsListAt(p);
                things.ForEach( (Thing t) => DesignateThing(t));
            }
        }

        public override AcceptanceReport CanDesignateThing(Thing t) {
            if (t.Map.designationManager.DesignationOn(t, Designator_ImproveThis.ImproveDesignationDef) != null) return AcceptanceReport.WasAccepted;
            return AcceptanceReport.WasRejected;
        }

        public override void DesignateThing(Thing t) {
            ImproveThisComp c = t.TryGetComp<ImproveThisComp>();
            if (c == null || !c.improveRequested) return;
            c.improveRequested = false;
        }
    }

    public class WorkGiver_ImproveThis : WorkGiver_Scanner {
        public static WorkTypeDef ImproveWorkType = DefDatabase<WorkTypeDef>.GetNamed("Improving");

        private static JobDef ImproveThisHaulJobDef = DefDatabase<JobDef>.GetNamed("ImproveHaul");
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
                            Job job = JobMaker.MakeJob(ImproveThisHaulJobDef);
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
            if (!pawn.workSettings.WorkIsActive(ImproveWorkType)) {
                JobFailReason.Is("NotAssignedToWorkType".Translate(ImproveWorkType.gerundLabel).CapitalizeFirst());
                return null;
            }
            if (!GenConstruct.CanConstruct(t, pawn, true, forced))
                return null;
            // if the building is already Masterwork, make sure the pawn is inspired
            // pawns CANNOT make Legendary things normally
            CompQuality q = t.TryGetComp<CompQuality>();
            if (q.Quality == QualityCategory.Masterwork) {
                if (pawn.InspirationDef != InspirationDefOf.Inspired_Creativity) {
                    JobFailReason.Is("ImproveInspireNeeded".Translate());
                    return null;
                }
            }
            Job j = JobMaker.MakeJob(ImproveThisJobDef, t);
            if (j.TryMakePreToilReservations(pawn, false)) return j;
            return null;
        }
    }

    public class JobDriver_HaulToImproveThisContainer : JobDriver {
        // special version of HaulToContainer to account for this case
        public Thing ThingToCarry => (Thing)job.GetTarget(TargetIndex.A);
        public Thing Container => (Thing)job.GetTarget(TargetIndex.B);

        public override string GetReport()
        {
            Thing thing = null;
            thing = ((pawn.CurJob != job || pawn.carryTracker.CarriedThing == null) ? base.TargetThingA : pawn.carryTracker.CarriedThing);
            if (thing == null || !job.targetB.HasThing)
            {
                return "ReportHaulingUnknown".Translate();
            }
            return "ReportHaulingTo".Translate(thing.Label, job.targetB.Thing.LabelShort.Named("DESTINATION"), thing.Named("THING"));
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed)) return false;
            if (!pawn.Reserve(job.GetTarget(TargetIndex.B), job, 1, -1, null, errorOnFailed)) return false;
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.A), job);
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // this collects all required materials and dumps them into B
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedNullOrForbidden(TargetIndex.B);
            this.FailOn(() => !Container.TryGetComp<ImproveThisComp>().improveRequested);
            Toil getToHaulTarget = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            Toil uninstallIfMinifiable = Toils_Construct.UninstallIfMinifiable(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            Toil startCarryingThing = Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: true);
            Toil jumpIfAlsoCollectingNextTarget = Toils_Haul.JumpIfAlsoCollectingNextTargetInQueue(getToHaulTarget, TargetIndex.A);
            Toil carryToContainer = Toils_Haul.CarryHauledThingToContainer();
            yield return Toils_Jump.JumpIf(jumpIfAlsoCollectingNextTarget, () => pawn.IsCarryingThing(ThingToCarry));
            yield return getToHaulTarget;
            yield return uninstallIfMinifiable;
            yield return startCarryingThing;
            yield return jumpIfAlsoCollectingNextTarget;
            yield return carryToContainer;
            yield return CustomDepositHauledThingInContainer(TargetIndex.B);
        }

        private Toil CustomDepositHauledThingInContainer(TargetIndex containerInd) {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                if (actor.carryTracker.CarriedThing == null)
                {
                    Log.Error(string.Concat(actor, " tried to place hauled thing in container but is not hauling anything."));
                    return;
                }
                Thing thing = curJob.GetTarget(containerInd).Thing;
                ImproveThisComp comp = thing.TryGetComp<ImproveThisComp>();
                if (comp == null || !comp.improveRequested) {
                    Log.Error(string.Concat(actor, " tried to place hauled thing into ", thing.Label, " but it is not accepting improvements."));
                    return;
                }
                ThingOwner thingOwner = comp.GetDirectlyHeldThings();
                if (thingOwner == null)
                {
                    Log.Error("Could not deposit hauled thing in container: " + curJob.GetTarget(containerInd).Thing);
                    return;
                }
                int num = actor.carryTracker.CarriedThing.stackCount;
                num = UnityEngine.Mathf.Min(GenConstruct.AmountNeededByOf((IConstructible)comp, actor.carryTracker.CarriedThing.def), num);
                actor.carryTracker.innerContainer.TryTransferToContainer(actor.carryTracker.CarriedThing, thingOwner, num);
            };
            return toil;
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
            CompQuality q = JobTarget.parent.TryGetComp<CompQuality>();
            if (q.Quality == QualityCategory.Masterwork)
                build.FailOn(() => pawn.InspirationDef != InspirationDefOf.Inspired_Creativity);
            build.WithProgressBar(TargetIndex.A, () => (float)JobTarget.workDone / (float)JobTarget.WorkToBuild);
            build.defaultCompleteMode = ToilCompleteMode.Delay;
            build.defaultDuration = 5000;
            build.activeSkill = () => SkillDefOf.Construction;
            yield return build;
        }
    }

    public class RestrictedThingOwner : ThingOwner<Thing> {
        private bool OVERRIDE = false;
        private ImproveThisComp ImproveComp;
        public RestrictedThingOwner(ImproveThisComp tc) : base(null, false) {
            ImproveComp = tc;
        }

        public RestrictedThingOwner(ThingOwner<Thing> old, ImproveThisComp tc) : base(null, false) {
            // force the contents in
            OVERRIDE = true;
            old.TryTransferAllToContainer(this);
            OVERRIDE = false;
        }

        public override int GetCountCanAccept(Thing item, bool canMergeWithExistingStacks = true)
        {
            if (!OVERRIDE) {
                if (!ImproveComp.improveRequested) return 0;
                ThingDefCountClass tdcc = ImproveComp.MaterialsNeeded().Find(tdc => tdc.thingDef == item.def);
                if (tdcc == null) return 0;
                return tdcc.count;
            }
            return base.GetCountCanAccept(item, canMergeWithExistingStacks);
        }

        public override int TryAdd(Thing item, int count, bool canMergeWithExistingStacks = true)
        {
            if (!OVERRIDE && !ImproveComp.improveRequested) return 0;
            return base.TryAdd(item, count, canMergeWithExistingStacks);
        }

        public override bool TryAdd(Thing item, bool canMergeWithExistingStacks = true)
        {
            if (!OVERRIDE && !ImproveComp.improveRequested) return false;
            return base.TryAdd(item, canMergeWithExistingStacks);
        }
    }

    public class ImproveThisComp : ThingComp, IConstructible {
        // storage

        private ThingOwner contents;
        public ThingOwner GetDirectlyHeldThings() {
            if (contents == null) contents = new RestrictedThingOwner(this);
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

        private bool _IMPROVEREQ;
        public bool improveRequested {
            get => _IMPROVEREQ;
            set {
                if (value == false && _IMPROVEREQ == true) {
                    GetDirectlyHeldThings().TryDropAll(parent.Position, parent.Map, ThingPlaceMode.Near);
                    parent.Map.designationManager.TryRemoveDesignationOn(parent, Designator_ImproveThis.ImproveDesignationDef);
                }
                if (value == true && _IMPROVEREQ == false) {
                    parent.Map.designationManager.AddDesignation(new Designation(parent, Designator_ImproveThis.ImproveDesignationDef));
                }
                _IMPROVEREQ = value;
            }
        }

        // everything else

        public override void PostExposeData() {
            base.PostExposeData();
            Scribe_Values.Look(ref _IMPROVEREQ, "improve", false);
            Scribe_Values.Look(ref workDone, "workDone", 0f);
            Scribe_Deep.Look(ref contents, "contents", this);
        }

        public override string CompInspectStringExtra() {
            System.Text.StringBuilder str = new System.Text.StringBuilder();
            str.Append(base.CompInspectStringExtra());
            if (!improveRequested && !GetDirectlyHeldThings().Any) return str.ToString();
            str.AppendLineIfNotEmpty();

            str.Append("ContainedResources".Translate() + ":");
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
                str.AppendLine();
                str.Append((need.thingDef.LabelCap + ": ") + num + " / " + needCount);
            }
            if (satisfied) {
                str.AppendLine();
                str.Append("WorkLeft".Translate() + ": " + UnityEngine.Mathf.CeilToInt(WorkLeft / 60f));
            }

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
            if (parent.def.blueprintDef == null) return null; // probably piano and such

            Command_Toggle cmd = new Command_Toggle();
            cmd.isActive = delegate() { return improveRequested; };
            cmd.toggleAction = delegate() {
                improveRequested = !improveRequested;
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
                MoteMaker.ThrowText(parent.DrawPos, parent.Map, "MessageQualityTooLow".Translate(q.GetLabel().CapitalizeFirst()), 6f);
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

        public override void PostDestroy(DestroyMode mode, Map previousMap) {
            base.PostDestroy(mode, previousMap);
            if (mode == DestroyMode.Deconstruct) GetDirectlyHeldThings().TryDropAll(parent.Position, parent.Map, ThingPlaceMode.Near);
        }
    }
}
