using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace QEthics
{
    /// <summary>
    /// A modification of the JobDriver_DoBill class in vanilla Rimworld. Loads growers with ingredients from a Bill, completing when
    /// there are no more ingredients to load for the current bill.
    /// </summary>
    public class JobDriver_LoadGrower : JobDriver
    {
        public const PathEndMode GotoIngredientPathEndMode = PathEndMode.ClosestTouch;
        public const TargetIndex BillGiverInd = TargetIndex.A;
        public const TargetIndex IngredientInd = TargetIndex.B;

        public IBillGiver BillGiver
        {
            get
            {
                IBillGiver billGiver = job.GetTarget(BillGiverInd).Thing as IBillGiver;
                if (billGiver == null)
                {
                    throw new InvalidOperationException("JobDriver_LoadGrower on non-Billgiver.");
                }
                return billGiver;
            }
        }

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

            return pawn.Reserve(TargetThingA, job, errorOnFailed: errorOnFailed) && pawn.Reserve(TargetThingB, job, errorOnFailed: errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //AddEndCondition(delegate
            //{
            //    Thing thing = GetActor().jobs.curJob.GetTarget(BillGiverInd).Thing;
            //    if (thing is Building && !thing.Spawned)
            //    {
            //        return JobCondition.Incompletable;
            //    }
            //    return JobCondition.Ongoing;
            //});
            this.FailOnBurningImmobile(BillGiverInd);
            this.FailOnDestroyedNullOrForbidden(BillGiverInd);
            this.FailOn(delegate
            {
                Thing building = job.GetTarget(BillGiverInd).Thing;
                IBillGiver billGiver = building as IBillGiver;
                if (building == null || billGiver == null || job.bill == null)
                {
                    return true;
                }

                if (!billGiver.CurrentlyUsableForBills())
                {
                    return true;
                }

                Building_GrowerBase_WorkTable grower = job.GetTarget(BillGiverInd).Thing as Building_GrowerBase_WorkTable;
                if (job.bill.DeletedOrDereferenced)
                {
                    //refund ingredients if player cancels bill during Filling phase
                    if (grower != null && grower.status == CrafterStatus.Filling)
                    {
                        QEEMod.TryLog("Bill cancelled, refunding ingredients");
                        grower.StopCrafting(true);
                    }

                    return true;
                }

                if(grower.status != CrafterStatus.Filling)
                {
                    return true;
                }

                return false;
            });

            //notify that we're starting the job
            //Toil notifyFillingStarted = new Toil()
            //{
            //    initAction = delegate ()
            //    {
            //        Building_GrowerBase_WorkTable vat = TargetThingA as Building_GrowerBase_WorkTable;
            //        if (vat != null && vat.status == CrafterStatus.Idle)
            //        {
            //            vat.Notify_FillingStarted(GetActor().CurJob.bill.recipe);
            //        }

            //        QEEMod.TryLog(pawn.Name + " starting new job 'QE_LoadGrowerJob'");
            //    }
            //};
            //yield return notifyFillingStarted;

            //travel to ingredient and carry it
            Toil reserveIng = Toils_Reserve.Reserve(IngredientInd);
            yield return reserveIng;
            Toil travelToil = Toils_Goto.GotoThing(IngredientInd, PathEndMode.OnCell);
            yield return travelToil;
            Toil carryThing = Toils_Haul.StartCarryThing(IngredientInd, subtractNumTakenFromJobCount: true);
            yield return carryThing;

            //Opportunistically haul a nearby ingredient of same ThingDef. Checks 8 square radius.
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveIng, IngredientInd, TargetIndex.None, takeFromValidStorage: true);

            //head back to grower
            yield return Toils_Goto.GotoThing(BillGiverInd, PathEndMode.InteractionCell).FailOnDestroyedOrNull(IngredientInd);

            //deposit into grower
            Toil depositIntoGrower = new Toil()
            {
                initAction = delegate ()
                {
                    Building_GrowerBase_WorkTable grower = TargetThingA as Building_GrowerBase_WorkTable;
                    if (grower != null)
                    {
                        grower.FillThing(GetActor().carryTracker.CarriedThing);
                    }
                }
            };
            yield return depositIntoGrower;

            Toil tryStartCrafting = new Toil()
            {
                initAction = delegate ()
                {
                    Building_GrowerBase_WorkTable grower = job.GetTarget(BillGiverInd).Thing as Building_GrowerBase_WorkTable;
                    Pawn actor = GetActor();

                    //if all ingredients have been loaded, start crafting
                    if (grower != null && grower.RemainingCountForIngredient("all", true) == 0)
                    {
                        grower.Notify_CraftingStarted();

                        //decrement billcount by 1
                        Bill_Production curBill = actor.CurJob.bill as Bill_Production;
                        if (curBill.repeatMode == BillRepeatModeDefOf.RepeatCount)
                        {
                            if (curBill.repeatCount > 0)
                            {
                                curBill.repeatCount--;
                            }
                        }
                    }

                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            };
            yield return tryStartCrafting;

        } //end function MakeNewToils()

    } //end class JobDriver_LoadGrower
}