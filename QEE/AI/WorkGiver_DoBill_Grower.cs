using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace QEthics
{
    /// <summary>
    /// Checks objects of type Building_GrowerBase_WorkTable for available bills and assigns jobs accordingly.
    /// Heavily-modified version of the WorkGiver_DoBill class in vanilla Rimworld (too many private members to sub-class).
    /// </summary>
    public class WorkGiver_DoBill_Grower : WorkGiver_Scanner
    {
        /* return true if all conditions are met:
            a) any bills outstanding
            b) status is 'Idle'
            c) the usual reservation and forbidden checks pass 
        */
        public override bool HasJobOnThing(Pawn pawn, Thing aThing, bool forced = false)
        {
            Building_GrowerBase_WorkTable grower = aThing as Building_GrowerBase_WorkTable;
            IBillGiver billGiver = aThing as IBillGiver;
            LocalTargetInfo target = aThing;
            if (grower == null)
            {
                return false;
            }

            else if (billGiver == null || !ThingIsUsableBillGiver(aThing) || !billGiver.UsableForBillsAfterFueling())
            {
                return false;
            }

            else if (aThing.IsBurning())
            {
                JobFailReason.Is("BurningLower".Translate());
                return false;
            }

            else if (aThing.IsForbidden(pawn))
            {
                //JobFailReason.Is("ForbiddenLower".Translate());
                return false;
            }

            //else if (!pawn.CanReserve(target, 1, -1, null, forced))
            else if (!pawn.Map.reservationManager.CanReserve(pawn, target, 1, -1, null, false))
            {
                Pawn pawn2 = pawn.Map.reservationManager.FirstRespectedReserver(target, pawn);

                if (pawn2 != null)
                {
                    JobFailReason.Is("ReservedBy".Translate(pawn2.LabelShort, pawn2));
                }
                else
                {
                    JobFailReason.Is("QE_JobFailReasonCannotReserve".Translate(pawn2.Named("PAWN"), grower.def.LabelCap));
                }
                return false;
            }

            //else if (grower.status != CrafterStatus.Idle)
            else if (grower.status != CrafterStatus.Idle && grower.status != CrafterStatus.Filling)
            {
                JobFailReason.Is("QE_JobFailReasonGrowerBusy".Translate(grower.def.LabelCap));
                return false;
            }

            //loop through BillStack and check for eligible jobs for this pawn
            else if (GetBillPawnShouldDoNow(pawn, billGiver.BillStack) == null)
            {
                return false;
            }

            return true;

        }

        public override Job JobOnThing(Pawn p, Thing t, bool forced = false)
        {
            IBillGiver billGiver = t as IBillGiver;
            Building_GrowerBase_WorkTable grower = t as Building_GrowerBase_WorkTable;

            //get the bill the pawn should do
            Bill theBill = GetBillPawnShouldDoNow(p, billGiver.BillStack);

            //get the item to fill from surrounding Things
            int countForVat = 0;
            Thing thingToFill = IngredientUtility.FindClosestIngForBill(theBill, p, ref countForVat);

            if (thingToFill != null)
            {
                grower.Notify_FillingStarted(theBill);

                Job returnJob = new Job(QEJobDefOf.QE_LoadGrowerJob, t, thingToFill);
                returnJob.count = countForVat;
                returnJob.bill = theBill;
                return returnJob;
            }

            return null;
        }

        public Bill GetBillPawnShouldDoNow(Pawn p, BillStack bills)
        {
            string failReason = "QE_JobFailReasonNoBillsQueued".Translate();
            for (int i = 0; i < bills.Count; i++)
            {
                if (bills[i].ShouldDoNow())
                {
                    if (bills[i].pawnRestriction != null && bills[i].pawnRestriction != p)
                    {
                        JobFailReason.Is("QE_JobFailReasonPawnRestriction".Translate(bills[i].recipe.LabelCap, bills[i].pawnRestriction.Named("PAWN")));
                        continue;
                    }
                    if (bills[i].recipe.workSkill != null)
                    {
                        int level = p.skills.GetSkill(bills[i].recipe.workSkill).Level;
                        if (level < bills[i].allowedSkillRange.min)
                        {
                            JobFailReason.Is("UnderAllowedSkill".Translate(bills[i].allowedSkillRange.min));
                            return null;
                        }
                        if (level > bills[i].allowedSkillRange.max)
                        {
                            JobFailReason.Is("AboveAllowedSkill".Translate(bills[i].allowedSkillRange.max));
                            return null;
                        }
                    }

                    //check if there are ingredients that this bill can use
                    int dummy = 0;
                    if(IngredientUtility.FindClosestIngForBill(bills[i], p, ref dummy) == null)
                    {
                        //JobFailReason.Is("No ingredients available");
                        JobFailReason.Is("MissingMaterials".Translate());
                        return null;
                    }

                    return bills[i];
                }
            }

            JobFailReason.Is(failReason);
            return null;
        }

        // -------------------------------------------------------------------
        // Unchanged members and functions from Workgiver_DoBill class
        // -------------------------------------------------------------------

        private class DefCountList
        {
            private List<ThingDef> defs = new List<ThingDef>();

            private List<float> counts = new List<float>();

            public int Count => defs.Count;

            public float this[ThingDef def]
            {
                get
                {
                    int num = defs.IndexOf(def);
                    if (num < 0)
                    {
                        return 0f;
                    }
                    return counts[num];
                }
                set
                {
                    int num = defs.IndexOf(def);
                    if (num < 0)
                    {
                        defs.Add(def);
                        counts.Add(value);
                        num = defs.Count - 1;
                    }
                    else
                    {
                        counts[num] = value;
                    }
                    CheckRemove(num);
                }
            }

            public float GetCount(int index)
            {
                return counts[index];
            }

            public void SetCount(int index, float val)
            {
                counts[index] = val;
                CheckRemove(index);
            }

            public ThingDef GetDef(int index)
            {
                return defs[index];
            }

            private void CheckRemove(int index)
            {
                if (counts[index] == 0f)
                {
                    counts.RemoveAt(index);
                    defs.RemoveAt(index);
                }
            }

            public void Clear()
            {
                defs.Clear();
                counts.Clear();
            }

            public void GenerateFrom(List<Thing> things)
            {
                Clear();
                for (int i = 0; i < things.Count; i++)
                {
                    DefCountList defCountList;
                    ThingDef def;
                    (defCountList = this)[def = things[i].def] = defCountList[def] + (float)things[i].stackCount;
                }
            }
        }

        private List<ThingCount> chosenIngThings = new List<ThingCount>();

        private static readonly IntRange ReCheckFailedBillTicksRange = new IntRange(500, 600);

        private static string MissingMaterialsTranslated;

        private static List<Thing> relevantThings = new List<Thing>();

        private static HashSet<Thing> processedThings = new HashSet<Thing>();

        private static List<Thing> newRelevantThings = new List<Thing>();

        private static List<IngredientCount> ingredientsOrdered = new List<IngredientCount>();

        private static List<Thing> tmpMedicine = new List<Thing>();

        private static DefCountList availableCounts = new DefCountList();

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                if (def.fixedBillGiverDefs != null && def.fixedBillGiverDefs.Count == 1)
                {
                    return ThingRequest.ForDef(def.fixedBillGiverDefs[0]);
                }
                return ThingRequest.ForGroup(ThingRequestGroup.PotentialBillGiver);
            }
        }

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Some;
        }

        public static void ResetStaticData()
        {
            MissingMaterialsTranslated = "MissingMaterials".Translate();
        }

        public bool ThingIsUsableBillGiver(Thing thing)
        {
            Pawn pawn = thing as Pawn;
            Corpse corpse = thing as Corpse;
            Pawn pawn2 = null;
            if (corpse != null)
            {
                pawn2 = corpse.InnerPawn;
            }
            if (def.fixedBillGiverDefs != null && def.fixedBillGiverDefs.Contains(thing.def))
            {
                return true;
            }
            if (pawn != null)
            {
                if (def.billGiversAllHumanlikes && pawn.RaceProps.Humanlike)
                {
                    return true;
                }
                if (def.billGiversAllMechanoids && pawn.RaceProps.IsMechanoid)
                {
                    return true;
                }
                if (def.billGiversAllAnimals && pawn.RaceProps.Animal)
                {
                    return true;
                }
            }
            if (corpse != null && pawn2 != null)
            {
                if (def.billGiversAllHumanlikesCorpses && pawn2.RaceProps.Humanlike)
                {
                    return true;
                }
                if (def.billGiversAllMechanoidsCorpses && pawn2.RaceProps.IsMechanoid)
                {
                    return true;
                }
                if (def.billGiversAllAnimalsCorpses && pawn2.RaceProps.Animal)
                {
                    return true;
                }
            }
            return false;
        }

    }
}