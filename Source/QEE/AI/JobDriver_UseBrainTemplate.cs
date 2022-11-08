using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace QEthics;

public class JobDriver_UseBrainTemplate : JobDriver
{
    public static float workRequired = 2000;

    public float ticksWork;
    public bool workStarted;

    public Pawn Patient => TargetThingA as Pawn;

    public BrainScanTemplate BrainTemplate => TargetThingB as BrainScanTemplate;

    public Building_Bed Bed => job.targetC.Thing as Building_Bed;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (!pawn.CanReserve(TargetThingA))
        {
            return false;
        }

        if (!pawn.CanReserve(TargetThingB))
        {
            return false;
        }

        return pawn.CanReserve(job.targetC.Thing) || Patient.CurrentBed() == Bed;
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref ticksWork, "ticksWork"); //workStarted
        Scribe_Values.Look(ref workStarted, "workStarted");
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        //A - Sleeper   B - Brain template   C - Bed

        this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
        this.FailOnDestroyedNullOrForbidden(TargetIndex.B);
        this.FailOnDestroyedNullOrForbidden(TargetIndex.C);

        yield return Toils_Reserve.Reserve(TargetIndex.B);

        if (Patient.CurJobDef != JobDefOf.LayDown || Patient.CurrentBed() != Bed)
        {
            QEEMod.TryLog("Patient not in bed, carrying them to bed");
            //get the patient and carry them to the bed
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.C);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.InteractionCell);
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, null, false);
            yield return Toils_Reserve.Release(TargetIndex.C);
        }

        //anesthetize the patient
        var anesthetizePatient = new Toil
        {
            initAction = delegate
            {
                Toils_Reserve.Reserve(TargetIndex.A);

                Patient.health.AddHediff(HediffDefOf.Anesthetic);

                if (Patient.CurJobDef == JobDefOf.LayDown)
                {
                    return;
                }

                Patient.pather.StopDead();
                Patient.jobs.StartJob(new Job(JobDefOf.LayDown, TargetC)
                {
                    forceSleep = true
                });
            }
        };
        yield return anesthetizePatient;

        //Surgeon gets the brain template, carries it, then travel to bed
        QEEMod.TryLog("Carrying brain template to bed");
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell);
        yield return Toils_Haul.StartCarryThing(TargetIndex.B);
        yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.ClosestTouch);

        var applyBrainTemplateToil = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Never,
                tickAction = delegate
                {
                    ticksWork -= StatDefOf.ResearchSpeed.Worker.IsDisabledFor(pawn)
                        ? 1f
                        : 1f * pawn.GetStatValue(StatDefOf.ResearchSpeed);

                    if (!(ticksWork <= 0))
                    {
                        return;
                    }

                    BrainManipUtility.ApplyBrainScanTemplateOnPawn(Patient, BrainTemplate);

                    //Give headache
                    Patient.health.AddHediff(QEHediffDefOf.QE_Headache, Patient.health.hediffSet.GetBrain());
                }
            }.WithProgressBar(TargetIndex.A, () => (workRequired - ticksWork) / workRequired)
            .WithEffect(EffecterDefOf.Research, TargetIndex.A);
        applyBrainTemplateToil.AddPreInitAction(delegate
        {
            ticksWork = workRequired;
            workStarted = true;
            //QEEMod.TryLog("preInitAction: ticksWork = " + ticksWork);
        });
        applyBrainTemplateToil.AddEndCondition(delegate
        {
            if (workStarted && ticksWork <= 0)
            {
                //QEEMod.TryLog("Succeeded: ticksWork = " + ticksWork);
                return JobCondition.Succeeded;
            }

            //QEEMod.TryLog("Ongoing: ticksWork = " + ticksWork);
            return JobCondition.Ongoing;
        });
        applyBrainTemplateToil.AddFailCondition(() => workStarted && Patient.CurJobDef != JobDefOf.LayDown);

        yield return applyBrainTemplateToil;
    }
}