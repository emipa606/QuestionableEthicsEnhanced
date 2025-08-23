using System;
using System.Collections.Generic;
using RimWorld;
using Verse.AI;

namespace QEthics;

/// <summary>
///     A modification of the JobDriver_DoBill class in vanilla Rimworld. Loads growers with ingredients from a Bill,
///     completing when
///     there are no more ingredients to load for the current bill.
/// </summary>
public class JobDriver_LoadGrower : JobDriver
{
    public const PathEndMode GotoIngredientPathEndMode = PathEndMode.ClosestTouch;
    private const TargetIndex BillGiverInd = TargetIndex.A;
    private const TargetIndex IngredientInd = TargetIndex.B;
    private int logCounter = 1;

    public IBillGiver BillGiver => job.GetTarget(BillGiverInd).Thing as IBillGiver ??
                                   throw new InvalidOperationException("JobDriver_LoadGrower on non-Billgiver.");

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

        var shouldStart = pawn.Reserve(TargetThingA, job, errorOnFailed: errorOnFailed) &&
                          pawn.Reserve(TargetThingB, job, errorOnFailed: errorOnFailed);

        //QEEMod.TryLog("TryMakePreToilRes for " + pawn.LabelShort + ": " + shouldStart);
        return shouldStart;
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnBurningImmobile(BillGiverInd);
        this.FailOnDestroyedNullOrForbidden(BillGiverInd);
        this.FailOnForbidden(IngredientInd);
        this.FailOn(delegate
        {
            var building = job.GetTarget(BillGiverInd).Thing;
            if (building is not IBillGiver || job.bill == null)
            {
                QEEMod.TryLog("something is null, failing job");
                return true;
            }

            var grower = job.GetTarget(BillGiverInd).Thing as Building_GrowerBase_WorkTable;
            if (job.bill.DeletedOrDereferenced)
            {
                QEEMod.TryLog("Bill deleted, failing job");

                //refund ingredients if player cancels bill during Filling phase
                if (grower is not { status: CrafterStatus.Filling })
                {
                    return true;
                }

                QEEMod.TryLog("Bill cancelled, refunding ingredients");
                grower.StopCrafting();

                return true;
            }

            if (grower is { status: CrafterStatus.Filling })
            {
                return false;
            }

            QEEMod.TryLog("Crafter is not 'filling', ending job");
            return true;
        });


        _ = new Toil
        {
            initAction = delegate
            {
                QEEMod.TryLog($"Pawn {GetActor().LabelShort} | Toil: {logCounter} | Job: {job.GetUniqueLoadID()}");
                logCounter++;
            }
        };
        ////yield return logToil;

        //travel to ingredient and carry it
        var reserveIng = Toils_Reserve.Reserve(IngredientInd);
        yield return reserveIng;
        //yield return logToil;

        var travelToil = Toils_Goto.GotoThing(IngredientInd, PathEndMode.OnCell);
        yield return travelToil;
        //yield return logToil;

        var carryThing = Toils_Haul.StartCarryThing(IngredientInd, subtractNumTakenFromJobCount: true);
        yield return carryThing;
        //yield return logToil;

        //Opportunistically haul a nearby ingredient of same ThingDef. Checks 8 square radius.
        yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveIng, IngredientInd, TargetIndex.None, true);
        //yield return logToil;


        //head back to grower
        yield return Toils_Goto.GotoThing(BillGiverInd, PathEndMode.InteractionCell)
            .FailOnDestroyedOrNull(IngredientInd);
        //yield return logToil;

        //deposit into grower
        var depositIntoGrower = new Toil
        {
            initAction = delegate
            {
                if (TargetThingA is Building_GrowerBase_WorkTable grower)
                {
                    grower.FillThing(GetActor().carryTracker.CarriedThing);
                }
            }
        };
        yield return depositIntoGrower;

        var tryStartCrafting = new Toil
        {
            initAction = delegate
            {
                var grower = job.GetTarget(BillGiverInd).Thing as Building_GrowerBase_WorkTable;
                var actor = GetActor();

                //if all ingredients have been loaded, start crafting
                if (grower?.billProc is { AnyPendingRequests: false })
                {
                    grower.Notify_CraftingStarted();
                }

                actor.jobs.EndCurrentJob(JobCondition.Succeeded);
            }
        };
        yield return tryStartCrafting;
    } //end function MakeNewToils()
} //end class JobDriver_LoadGrower