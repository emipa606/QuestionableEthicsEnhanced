using RimWorld;
using Verse;
using Verse.AI;

namespace QEthics;

/// <summary>
///     Checks objects of type Building_GrowerBase_WorkTable for available bills and assigns jobs accordingly.
///     Heavily-modified version of the WorkGiver_DoBill class in vanilla Rimworld (too many private members to subclass).
/// </summary>
public class WorkGiver_DoBill_Grower : WorkGiver_Scanner
{
    #region TranslatedStrings

    private static string NoBillsQueuedTrans,
        GrowerBusyTrans,
        NoIngredientsTrans,
        RecentIngSearchTrans,
        WorkTypeMismatchTrans,
        ShouldDoFalseTrans,
        GenericFailReasonTrans,
        IsBurningTrans,
        PawnRestrictionTrans,
        UnderAllowedSkillTrans,
        AboveAllowedSkillTrans,
        ReservedByTrans,
        ReservedTrans;

    #endregion

    #region Constructors

    public WorkGiver_DoBill_Grower()
    {
        //Initialized in this way, these strings will only be loaded once per Rimworld.exe load
        NoBillsQueuedTrans = "QE_JobFailReasonNoBillsQueued".Translate();
        GrowerBusyTrans = "QE_JobFailReasonGrowerBusy".Translate();
        NoIngredientsTrans = "QE_JobFailReasonNoIngredients".Translate();
        RecentIngSearchTrans = "QE_JobFailReasonRecentIngSearch".Translate();
        WorkTypeMismatchTrans = "QE_JobFailReasonWorkTypeMismatch".Translate();
        ShouldDoFalseTrans = "QE_JobFailReasonShouldDoFalse".Translate();
        GenericFailReasonTrans = "QE_JobFailReasonGeneric".Translate();
        IsBurningTrans = "BurningLower".Translate();
        PawnRestrictionTrans = "QE_JobFailReasonPawnRestriction".Translate();
        UnderAllowedSkillTrans = "UnderAllowedSkill".Translate();
        AboveAllowedSkillTrans = "AboveAllowedSkill".Translate();
        ReservedByTrans = "IsReservedBy".Translate();
        ReservedTrans = "Reserved".Translate();
    }

    #endregion Constructors

    // -------------------------------------------------------------------
    // Unchanged members and functions from Workgiver_DoBill class
    // -------------------------------------------------------------------

    //private static readonly IntRange ReCheckFailedBillTicksRange = new IntRange(500, 600);

    public override ThingRequest PotentialWorkThingRequest => def.fixedBillGiverDefs is { Count: 1 }
        ? ThingRequest.ForDef(def.fixedBillGiverDefs[0])
        : ThingRequest.ForGroup(ThingRequestGroup.PotentialBillGiver);

    /* return true if all conditions are met:
        a. any bills outstanding
        b. status is 'Idle'
        c. the usual reservation and forbidden checks pass
    */
    public override bool HasJobOnThing(Pawn pawn, Thing aThing, bool forced = false)
    {
        var grower = aThing as Building_GrowerBase_WorkTable;
        _ = aThing as IBillGiver;
        var processor = grower?.billProc;
        LocalTargetInfo target = aThing;
        if (grower == null)
        {
            return false;
        }

        if (grower.status is CrafterStatus.Crafting or CrafterStatus.Finished)
        {
            JobFailReason.Is(GrowerBusyTrans);

            return false;
        }

        if (!processor.AnyPendingRequests)
        {
            JobFailReason.Is(NoBillsQueuedTrans);
            return false;
        }

        //check if the grower's cached ingredients search found ingredients for any bill
        if (grower.billProc.anyBillIngredientsAvailable == false)
        {
            JobFailReason.Is(NoIngredientsTrans);
            return false;
        }

        if (aThing.IsBurning())
        {
            JobFailReason.Is(IsBurningTrans);
            return false;
        }

        if (aThing.IsForbidden(pawn))
        {
            return false;
        }

        if (!ThingIsUsableBillGiver(aThing))
        {
            return false;
        }

        string reason;

        //if the grower isn't growing anything, loop through the billstack and check for any bill the pawn can do
        if (grower.billProc.ActiveBill == null)
        {
            if (GetBillPawnCanDo(pawn, grower, out reason) != null)
            {
                return true;
            }

            JobFailReason.Is(reason);
            return false;
        }

        if (PawnCanDoThisBill(pawn, grower.billProc.ActiveBill, target, out reason))
        {
            return true;
        }

        JobFailReason.Is(reason);
        return false;
    } //end HasJobOnThing()

    public override Job JobOnThing(Pawn p, Thing t, bool forced = false)
    {
        _ = t as IBillGiver;
        var grower = t as Building_GrowerBase_WorkTable;
        var processor = grower?.billProc;

        //if null is returned from this function, the game will throw an error. Instead,
        //return this simple wait job to avoid unnecessary errors.
        var returnJobOnFailure = new Job(JobDefOf.Wait, 5);

        //check if there are any pending requests (unfulfilled bills)
        if (processor is { AnyPendingRequests: false })
        {
            QEEMod.TryLog("AnyPendingRequests is false inside JobOnThing()!");
            return returnJobOnFailure;
        }

        //check if there are ingredients available for those bills
        if (processor != null && processor.ingredientsAvailableNow.Count <= 0)
        {
            QEEMod.TryLog("ingredientsAvailableNow.Count is 0 inside JobOnThing()!");
            return returnJobOnFailure;
        }

        //get the reference to the currently active bill
        Bill theBill = processor?.ActiveBill;
        if (theBill == null)
        {
            QEEMod.TryLog("Attempt to get ActiveBill failed inside JobOnThing()!");
            return returnJobOnFailure;
        }

        //get the cached ingredient that's available. This Thing is not always going to be used in the Job, 
        //but we know there's at least one stack of this Thing available on the map
        grower.billProc.ingredientsAvailableNow.TryGetValue(theBill.GetUniqueLoadID(), out var cachedThing);
        if (cachedThing == null)
        {
            QEEMod.TryLog($"Attempt to retrieve cached ingredients failed for {theBill.GetUniqueLoadID()}");
            return returnJobOnFailure;
        }

        //get the nearest Thing to the Pawn with the same ThingDef
        var countForVat = 0;
        var thingToFill = IngredientUtility.ThingPawnShouldRetrieveForBill(theBill, p, ref countForVat);
        if (thingToFill == null)
        {
            grower.billProc.anyBillIngredientsAvailable = false;
            QEEMod.TryLog($"ThingPawnShouldRetrieveForBill() is null for {theBill.GetUniqueLoadID()}");

            return returnJobOnFailure;
        }

        //all checks have passed! Return the Job and notify the grower that it's time to start Filling
        grower.Notify_FillingStarted(theBill);

        var returnJob = new Job(QEJobDefOf.QE_LoadGrowerJob, t, thingToFill)
        {
            count = countForVat,
            bill = theBill
        };
        return returnJob;
    } //end JobOnThing()

    /// <summary>
    ///     Checks if a pawn can be assigned to a bill based on factors including PawnRestrictions, workSkills, etc.
    ///     If a bill passes all the checks, it's assigned as the ActiveBill in the billProcessor.
    /// </summary>
    /// <param name="p"></param>
    /// <param name="grower"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    public Bill GetBillPawnCanDo(Pawn p, Building_GrowerBase_WorkTable grower, out string reason)
    {
        reason = GenericFailReasonTrans;

        var bills = grower.billStack;
        LocalTargetInfo target = grower;

        //loop through bills
        foreach (var bill in bills)
        {
            var theBill = bill as Bill_Production;

            if (!PawnCanDoThisBill(p, theBill, target, out reason))
            {
                continue;
            }

            //check if cached ingredients search found ingredients for this bill
            if (theBill == null)
            {
                continue;
            }

            grower.billProc.ingredientsAvailableNow.TryGetValue(theBill.GetUniqueLoadID(), out var cachedThing);
            if (cachedThing == null)
            {
                QEEMod.TryLog("GetBillPawnCanDo - no ingredients available");
                reason = NoIngredientsTrans;
                continue;
            }

            grower.billProc.ActiveBill = theBill;
            QEEMod.TryLog($"{p.LabelShort} can do bill {theBill.GetUniqueLoadID()}");
            return theBill;
        }

        return null;
    } //end GetBillPawnCanDo()


    /// <summary>
    ///     Checks if a pawn can be assigned to a bill based on factors including PawnRestrictions, workSkills, etc.
    ///     Does not check if ingredients are nearby.
    /// </summary>
    /// <param name="p"></param>
    /// <param name="theBill"></param>
    /// <param name="target"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    public bool PawnCanDoThisBill(Pawn p, Bill theBill, LocalTargetInfo target, out string reason)
    {
        reason = GenericFailReasonTrans;
        if (Find.TickManager.TicksGame < theBill.nextTickToSearchForIngredients)
        {
            reason = RecentIngSearchTrans;
            return false;
        }

        if (theBill.recipe?.requiredGiverWorkType != null && theBill.recipe.requiredGiverWorkType != def.workType)
        {
            reason = WorkTypeMismatchTrans;
            return false;
        }

        var shouldDo = theBill.ShouldDoNow();

        if (!shouldDo)
        {
            reason = ShouldDoFalseTrans;
            return false;
        }

        if (theBill.PawnRestriction != null && theBill.PawnRestriction != p)
        {
            reason = string.Format(PawnRestrictionTrans, theBill.PawnRestriction.LabelShort);

            return false;
        }

        if (theBill.recipe?.workSkill != null)
        {
            var level = p.skills.GetSkill(theBill.recipe.workSkill).Level;
            if (level < theBill.allowedSkillRange.min)
            {
                reason = string.Format(UnderAllowedSkillTrans, theBill.allowedSkillRange.min);
                return false;
            }

            if (level > theBill.allowedSkillRange.max)
            {
                reason = string.Format(AboveAllowedSkillTrans, theBill.allowedSkillRange.max);
                return false;
            }
        }

        if (!p.Map.reservationManager.CanReserve(p, target))
        {
            var pawn2 = p.Map.reservationManager.FirstRespectedReserver(target, p);

            reason = pawn2 != null
                ? string.Format(ReservedByTrans, theBill.billStack.billGiver.LabelShort, pawn2.LabelShort)
                : string.Format(ReservedTrans);

            return false;
        }

        QEEMod.TryLog($"{p.LabelShort} can do bill {theBill.GetUniqueLoadID()}");
        return true;
    } //end PawnCanDoThisBill()

    public bool ThingIsUsableBillGiver(Thing thing)
    {
        return def.fixedBillGiverDefs != null && def.fixedBillGiverDefs.Contains(thing.def);
    }

    public override Danger MaxPathDanger(Pawn pawn)
    {
        return Danger.Some;
    }
} //end class WorkGiver_DoBill_Grower