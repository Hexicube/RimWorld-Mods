using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorld___Improve_This {
    public class ImproveThis_Mod : Mod {
        public static ImproveThis_Settings Settings;
        public ImproveThis_Mod(ModContentPack content) : base(content) {
            Settings = GetSettings<ImproveThis_Settings>();
        }

        public override string SettingsCategory() {
            return "Improve This";
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect canvas) {
            Settings.DoWindowsContents(canvas);
        }
    }

    public class ImproveThis_Settings : ModSettings {
        // defaults are 5%
        private int       AwfulSkillReq = 0;
        private int        PoorSkillReq = 0;
        private int      NormalSkillReq = 3;
        private int        GoodSkillReq = 8;
        private int  ExcellentSkillReq = 18;
        private int MasterworkSkillReq = 21;
        private string[] SkillEntry = new string[6];

        public int GetSkillReq(QualityCategory quality, bool inspired, bool production) {
            int q = (int)quality;
            if (inspired) q -= 2;
            if (production) q--;
            if (q < 0) q = 0;

            if (q == 0) return AwfulSkillReq;
            if (q == 1) return PoorSkillReq;
            if (q == 2) return NormalSkillReq;
            if (q == 3) return GoodSkillReq;
            if (q == 4) return ExcellentSkillReq;
            return MasterworkSkillReq;
        }

        public void DoWindowsContents(UnityEngine.Rect canvas) {
            Listing_Standard list = new Listing_Standard { ColumnWidth = 300f };
            list.Begin(canvas);

            list.Label("IT_SkillReq".Translate());
            list.TextFieldNumericLabeled(      "QualityCategory_Poor".Translate(), ref      AwfulSkillReq, ref SkillEntry[0]);
            list.TextFieldNumericLabeled(    "QualityCategory_Normal".Translate(), ref       PoorSkillReq, ref SkillEntry[1]);
            list.TextFieldNumericLabeled(      "QualityCategory_Good".Translate(), ref     NormalSkillReq, ref SkillEntry[2]);
            list.TextFieldNumericLabeled( "QualityCategory_Excellent".Translate(), ref       GoodSkillReq, ref SkillEntry[3]);
            list.TextFieldNumericLabeled("QualityCategory_Masterwork".Translate(), ref  ExcellentSkillReq, ref SkillEntry[4]);
            list.TextFieldNumericLabeled( "QualityCategory_Legendary".Translate(), ref MasterworkSkillReq, ref SkillEntry[5]);
            
            list.Gap(20);

            list.Label("IT_SkillTrials".Translate());
            list.TextFieldNumericLabeled("IT_TrialOdds".Translate(), ref Cutoff, ref CutoffStr, 0, 1);
            if (list.ButtonText("IT_DoTrials".Translate())) DoTrials(Cutoff);

            list.End();
        }

        public override void ExposeData() {
            Scribe_Values.Look(ref      AwfulSkillReq, "awfulSkill",  0, false); SkillEntry[0] =      AwfulSkillReq.ToString();
            Scribe_Values.Look(ref       PoorSkillReq,  "poorSkill",  0, false); SkillEntry[1] =       PoorSkillReq.ToString();
            Scribe_Values.Look(ref     NormalSkillReq,  "normSkill",  3, false); SkillEntry[2] =     NormalSkillReq.ToString();
            Scribe_Values.Look(ref       GoodSkillReq,  "goodSkill",  8, false); SkillEntry[3] =       GoodSkillReq.ToString();
            Scribe_Values.Look(ref  ExcellentSkillReq, "excelSkill", 18, false); SkillEntry[4] =  ExcellentSkillReq.ToString();
            Scribe_Values.Look(ref MasterworkSkillReq,  "mastSkill", 21, false); SkillEntry[5] = MasterworkSkillReq.ToString();
        }

        private int[,] CurTrials = new int[21,7];
        private float Cutoff = 0.05f; private string CutoffStr = "0.05";
        private void DoTrials() {
            //1mil trials at each pawn level
            for (int a = 0; a < 1000000; a++) {
                for (int l = 0; l <= 20; l++) {
                    int v = (int)QualityUtility.GenerateQualityCreatedByPawn(l, false);
                    CurTrials[l,v]++;
                }
            }
        }
        public void DoTrials(float cutoff) {
            // do first trials if none
            int sum = 0;
            for (int a = 0; a < 7; a++) sum += CurTrials[0,a];
            if (sum == 0) DoTrials();
            // work out cutoff for each
            AwfulSkillReq = 0;
            PoorSkillReq = 0;
            NormalSkillReq = 0;
            GoodSkillReq = 0;
            ExcellentSkillReq = 0;
            MasterworkSkillReq = 0;
            for (int l = 20; l >= 0; l--) {
                sum = 0;
                for (int a = 0; a < 7; a++) sum += CurTrials[l,a];
                double pct = (double)CurTrials[l,6] / (double)sum;
                if (MasterworkSkillReq < l) {
                    if (pct < cutoff) MasterworkSkillReq = l + 1;
                }
                pct += (double)CurTrials[l,5] / (double)sum;
                if (ExcellentSkillReq < l) {
                    if (pct < cutoff) ExcellentSkillReq = l + 1;
                }
                pct += (double)CurTrials[l,4] / (double)sum;
                if (GoodSkillReq < l) {
                    if (pct < cutoff) GoodSkillReq = l + 1;
                }
                pct += (double)CurTrials[l,3] / (double)sum;
                if (NormalSkillReq < l) {
                    if (pct < cutoff) NormalSkillReq = l + 1;
                }
                pct += (double)CurTrials[l,2] / (double)sum;
                if (PoorSkillReq < l) {
                    if (pct < cutoff) PoorSkillReq = l + 1;
                }
                pct += (double)CurTrials[l,1] / (double)sum;
                if (AwfulSkillReq < l) {
                    if (pct < cutoff) AwfulSkillReq = l + 1;
                }
            }
            SkillEntry[0] =      AwfulSkillReq.ToString();
            SkillEntry[1] =       PoorSkillReq.ToString();
            SkillEntry[2] =     NormalSkillReq.ToString();
            SkillEntry[3] =       GoodSkillReq.ToString();
            SkillEntry[4] =  ExcellentSkillReq.ToString();
            SkillEntry[5] = MasterworkSkillReq.ToString();
        }
    }

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
            if (!pawn.HasReserved(th) && !pawn.CanReserve(th))
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
                            if (pawn.HasReserved(job.targetB) || pawn.CanReserve(job.targetB, ignoreOtherReservations: forced)) return job;
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
            // make sure the pawn is capable according to settings
            if (c.WorkLeft <= 120f) {
                CompQuality q = t.TryGetComp<CompQuality>();
                int skill = pawn.skills.GetSkill(SkillDefOf.Construction).Level;
                bool inspired = pawn.InspirationDef == InspirationDefOf.Inspired_Creativity;
                bool production = false;
                if (ModsConfig.IdeologyActive && pawn.Ideo != null) {
                    Precept_Role role = pawn.Ideo.GetRole(pawn);
                    if (role != null && role.def.roleEffects != null) {
                        RoleEffect eff = role.def.roleEffects.FirstOrFallback((RoleEffect e) => e is RoleEffect_ProductionQualityOffset);
                        if (eff != null) production = true;
                    }
                }
                int skillReq = ImproveThis_Mod.Settings.GetSkillReq(q.Quality, inspired, production);
                if (skillReq > skill) {
                    int skillUpperReq = ImproveThis_Mod.Settings.GetSkillReq(q.Quality, true, ModsConfig.IdeologyActive);
                    if (skillUpperReq > skill) {
                        if (ModsConfig.IdeologyActive) JobFailReason.Is("ImproveSkillInspireOrRoleNeeded".Translate(skillReq.ToString(), skillUpperReq.ToString()));
                        else JobFailReason.Is("ImproveSkillInspireNeeded".Translate(skillReq.ToString(), skillUpperReq.ToString()));
                    }
                    else if (ModsConfig.IdeologyActive) JobFailReason.Is("ImproveInspireOrRoleNeeded".Translate(skillReq.ToString()));
                    else JobFailReason.Is("ImproveInspireNeeded".Translate(skillReq.ToString()));
                    return null;
                }
            }
            Job j = JobMaker.MakeJob(ImproveThisJobDef, t);
            if (pawn.HasReserved(j.targetA) || pawn.CanReserve(j.targetA, ignoreOtherReservations: forced)) return j;
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
            if (!pawn.HasReserved(job.targetA, job)) {
                if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed)) return false;
            }
            if (!pawn.HasReserved(job.targetB, job)) {
                if (!pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed)) {
                    pawn.Map.reservationManager.Release(job.targetA, pawn, job);
                    return false;
                }
            }
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
            if (pawn.HasReserved(job.targetA, job)) return true;
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils() {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.A);
            Toil build = new Toil();
            build.initAction = delegate {
                GenClamor.DoClamor(build.actor, 15f, ClamorDefOf.Construction);
            };
            CompQuality q = JobTarget.parent.TryGetComp<CompQuality>();
            build.tickAction = delegate {
                if (!JobTarget.improveRequested) {
                    ReadyForNextToil();
                    return;
                }
                Pawn actor = build.actor;
                if (JobTarget.WorkLeft <= 120f) {
                    int skill = actor.skills.GetSkill(SkillDefOf.Construction).Level;
                    bool inspired = actor.InspirationDef == InspirationDefOf.Inspired_Creativity;
                    bool production = false;
                    if (ModsConfig.IdeologyActive && pawn.Ideo != null) {
                        Precept_Role role = pawn.Ideo.GetRole(pawn);
                        if (role != null && role.def.roleEffects != null) {
                            RoleEffect eff = role.def.roleEffects.FirstOrFallback((RoleEffect e) => e is RoleEffect_ProductionQualityOffset);
                            if (eff != null) production = true;
                        }
                    }
                    int skillReq = ImproveThis_Mod.Settings.GetSkillReq(q.Quality, inspired, production);
                    if (skillReq > skill) {
                        ReadyForNextToil();
                        return;
                    }
                }
                actor.skills.Learn(SkillDefOf.Construction, 0.25f);
                float speed = actor.GetStatValue(StatDefOf.ConstructionSpeed) * 1.7f;
                if (JobTarget.parent.Stuff != null) speed *= JobTarget.parent.Stuff.GetStatValueAbstract(StatDefOf.ConstructionSpeedFactor);
                float workToBuild = JobTarget.WorkToBuild;
                if (actor.Faction == Faction.OfPlayer) {
                    float statValue = actor.GetStatValue(StatDefOf.ConstructSuccessChance);
                    if (!TutorSystem.TutorialMode && Rand.Value < 1f - UnityEngine.Mathf.Pow(statValue, speed / workToBuild)) {
                        JobTarget.FailConstruction(actor);
                        ReadyForNextToil();
                        return;
                    }
                }
                JobTarget.workDone += speed;
                if (JobTarget.WorkLeft <= 0) {
                    JobTarget.CompleteConstruction(actor);
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
            build.finishActions.Add(delegate {
                pawn.Map.reservationManager.Release(job.targetA, pawn, job);
            });
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
            List<ThingDefCountClass> list = parent.def.CostListAdjusted(parent.Stuff, false);
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
            List<ThingDefCountClass> list = parent.def.CostListAdjusted(parent.Stuff, false);
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
                int skillReq = ImproveThis_Mod.Settings.GetSkillReq(parent.GetComp<CompQuality>().Quality, false, false);
                if (skillReq > 0) {
                    str.AppendLine();
                    if (ModsConfig.IdeologyActive) str.Append("NeedsCreativeOrRole".Translate(skillReq.ToString()));
                    else str.Append("NeedsCreative".Translate(skillReq.ToString()));
                }
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
