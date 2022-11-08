using System.Collections.Generic;
using RimWorld;
using Verse;

namespace QEthics;

/// <summary>
///     Helps process orders for Things in bills.
/// </summary>
public class BillProcessor : IExposable
{
    /// <summary>
    ///     Current bill being worked on. If null, crafter is not working on a bill. It is not saved in ExposeData().
    /// </summary>
    private Bill_Production _activeBill;

    //private static readonly IntRange ReCheckFailedBillTicksRange = new IntRange(500, 598);

    /// <summary>
    ///     Unique LoadID of the bill being worked on. This value is saved in ExposeData().
    /// </summary>
    private string _activeBillID;

    /// <summary>
    ///     Cached value that is updated after a function checks for recipe ingredients on a interval. Designed to be checked
    ///     in a WorkGiver HasJobOnThing() function, or when the ingredientContainer contents change.
    /// </summary>
    public bool anyBillIngredientsAvailable;

    /// <summary>
    ///     Things we desire that can be anything.
    /// </summary>
    public Dictionary<string, ThingOrderRequest> desiredRequests = new Dictionary<string, ThingOrderRequest>();

    public Dictionary<string, Thing> ingredientsAvailableNow = new Dictionary<string, Thing>();

    /// <summary>
    ///     The holder object we are observing orders for.
    /// </summary>
    public IThingHolder observedThingHolder;

    public bool requestsLost;


    public BillProcessor(IThingHolder observedThingHolder)
    {
        this.observedThingHolder = observedThingHolder;
        Notify_ContentsChanged();
    }

    public BillProcessor()
    {
    }

    public ThingOwner ObservedThingOwner => observedThingHolder.GetDirectlyHeldThings();

    public bool AnyPendingRequests => desiredRequests.Count > 0;

    public Bill_Production ActiveBill
    {
        get => _activeBill;
        set
        {
            _activeBill = value;
            _activeBillID = value.GetUniqueLoadID();
        }
    }

    //Inherited
    public void ExposeData()
    {
        Scribe_Values.Look(ref anyBillIngredientsAvailable, "anyBillIngredientsAvailable");
        Scribe_Values.Look(ref _activeBillID, "_activeBillID");

        if (Scribe.mode != LoadSaveMode.PostLoadInit)
        {
            return;
        }

        if (_activeBillID != null)
        {
            UpdateActiveBill();
        }

        //Notify_ContentsChanged();
    }

    public void UpdateAvailIngredientsCache()
    {
        anyBillIngredientsAvailable = false;
        ingredientsAvailableNow.Clear();

        if (AnyPendingRequests == false)
        {
            return;
        }

        //Thing giver = (observedThingHolder as Thing);
        //QEEMod.TryLog("Updating available ingredients cache for " + giver.ThingID);

        var bills = new List<Bill>();
        if (_activeBill != null)
        {
            bills.Add(_activeBill);
        }
        else
        {
            bills = (observedThingHolder as IBillGiver)?.BillStack.Bills;
        }

        var ingsFoundOnMap = new Dictionary<string, Thing>();
        var checkIntervalTicks = QEESettings.instance.ingredientCheckIntervalSeconds * 60;
        if (bills == null)
        {
            return;
        }

        foreach (var bill in bills)
        {
            if (bill is not Bill_Production curBill)
            {
                continue;
            }

            if (Find.TickManager.TicksGame < curBill.nextTickToSearchForIngredients)
            {
                QEEMod.TryLog($"checked {curBill.GetUniqueLoadID()} for avail. ingredients recently, skipping");
                continue;
            }

            if (!curBill.ShouldDoNow())
            {
                continue;
            }

            //loop through ingredients
            foreach (var curIng in curBill.recipe.ingredients)
            {
                //if same ingredient was found on map in previous loop iteration, don't search map again
                ingsFoundOnMap.TryGetValue(curIng.FixedIngredient.defName, out var ing);
                if (ing != null)
                {
                    ingredientsAvailableNow[curBill.GetUniqueLoadID()] = ing;
                    //QEEMod.TryLog("Already found " + ing.Label + " on previous iteration. Skipping");
                    break;
                }

                //check if this ingredient is on the map
                ing = IngredientUtility.FindClosestIngToBillGiver(curBill, curIng);
                if (ing == null)
                {
                    continue;
                }

                QEEMod.TryLog($"Found {ing.Label} on map, adding to ingredient cache");
                ingsFoundOnMap[curIng.FixedIngredient.defName] = ing;
                ingredientsAvailableNow[curBill.GetUniqueLoadID()] = ing;
                anyBillIngredientsAvailable = true;
                break;
                //QEEMod.TryLog("no ingredients available");
            }

            if (!ingredientsAvailableNow.TryGetValue(curBill.GetUniqueLoadID(), out var dummy))
            {
                curBill.nextTickToSearchForIngredients = Find.TickManager.TicksGame;
            }
        }
    }

    /// <summary>
    ///     Checks if any Things were lost in the bill processor.
    /// </summary>
    public void ValidateDesiredRequests()
    {
        var elementsRemoved = 0;

        foreach (var entry in desiredRequests)
        {
            var orderRequest = entry.Value;
            if (orderRequest == null)
            {
                continue;
            }

            if (!orderRequest.HasThing)
            {
                continue;
            }

            if (orderRequest.thing.Destroyed)
            {
                //QEEMod.TryLog("QEE: ABORTING CLONE! " + orderRequest.Label + " was destroyed");
                elementsRemoved++;
                desiredRequests.Remove(entry.Key);
            }
            else if (orderRequest.thing.ParentHolder is not (Pawn_CarryTracker or Map or { }))
            {
                //QEEMod.TryLog("QEE: ABORTING CLONE! " + orderRequest.Label + " did not spawn in valid container");
                elementsRemoved++;
                desiredRequests.Remove(entry.Key);
            }
            //QEEMod.TryLog("QEE: orderRequest is null");
        }

        if (elementsRemoved > 0)
        {
            requestsLost = true;
        }
    }

    public void Reset()
    {
        _activeBillID = null;
        _activeBill = null;

        //recache desired requests and available ingredients
        UpdateDesiredRequests();
        UpdateAvailIngredientsCache();
    }

    /// <summary>
    ///     Call this whenever contents change
    /// </summary>
    public void Notify_ContentsChanged()
    {
        ValidateDesiredRequests();

        //Recache our requests
        UpdateDesiredRequests();
    }

    public void UpdateDesiredRequests()
    {
        desiredRequests.Clear();

        var bills = new List<Bill>();
        if (_activeBill != null)
        {
            bills.Add(_activeBill);
        }
        else
        {
            bills = (observedThingHolder as IBillGiver)?.BillStack.Bills;
        }

        if (bills == null)
        {
            return;
        }

        foreach (var bill in bills)
        {
            var theBill = bill as Bill_Production;

            if (theBill?.recipe?.ingredients == null)
            {
                continue;
            }

            foreach (var curIng in theBill.recipe.ingredients)
            {
                var filterCopy = new ThingFilter();
                filterCopy.CopyAllowancesFrom(curIng.filter);

                var request = new ThingOrderRequest(filterCopy);

                var countNeededForCrafting =
                    IngredientUtility.RemainingCountForIngredient(ObservedThingOwner, theBill.recipe, curIng);

                if (countNeededForCrafting <= 0)
                {
                    continue;
                }
                //QEEMod.TryLog("Adding " + curIng.FixedIngredient.label + " amount: " + countNeededForCrafting);

                request.amount = countNeededForCrafting;

                desiredRequests[curIng.FixedIngredient.defName] = request;
            } //end foreach loop
        }
    } //end UpdateDesiredRequests

    /// <summary>
    ///     Updates the _activeBill variable from the billID. Should only need to be called when loading a save.
    /// </summary>
    public void UpdateActiveBill()
    {
        var bills = (observedThingHolder as IBillGiver)?.BillStack;
        if (bills == null)
        {
            return;
        }

        foreach (var bill in bills)
        {
            var curBill = (Bill_Production)bill;
            if (curBill.GetUniqueLoadID() == _activeBillID)
            {
                _activeBill = curBill;
            }
        }
    }
}