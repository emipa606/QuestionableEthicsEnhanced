using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace QEthics
{
    /// <summary>
    /// Helps process orders for Things in bills.
    /// </summary>
    public class BillProcessor //: IExposable
    {

        #region Members
        /// <summary>
        /// Things we desire that can be anything.
        /// </summary>
        public Dictionary<string, ThingOrderRequest> desiredRequests = new Dictionary<string, ThingOrderRequest>();
        //public List<ThingOrderRequest> desiredIngredients = new List<ThingOrderRequest>();

        /// <summary>
        /// Cached value that is updated after a function checks for recipe ingredients on a interval. Designed to be checked 
        /// in a WorkGiver HasJobOnThing() function, or when the ingredientContainer contents change.
        /// </summary>
        public bool anyBillIngredientsAvailable = false;

        /// <summary>
        /// The holder object we are observing orders for.
        /// </summary>
        public IThingHolder observedThingHolder;

        public bool requestsLost = false;

        public Dictionary<string, Thing> ingredientsAvailableNow = new Dictionary<string, Thing>();

        private static readonly IntRange ReCheckFailedBillTicksRange = new IntRange(500, 598);

        #endregion Members

        #region Constructors

        public BillProcessor(IThingHolder observedThingHolder)
        {
            this.observedThingHolder = observedThingHolder;
            Notify_ContentsChanged();
        }

        public BillProcessor()
        {
            
        }

        #endregion Constructors

        #region Properties
        public ThingOwner ObservedThingOwner
        {
            get
            {
                return observedThingHolder.GetDirectlyHeldThings();
            }
        }

        public bool AnyPendingRequests
        {
            get
            {
                return desiredRequests.Count > 0;
            }
        }

        #endregion Properties

        #region Methods

        //is this expensive? Should I cache this?
        public Bill_Production ActiveBill
        {
            get
            {
                //BillStack bills = (observedThingHolder as IBillGiver)?.BillStack;
                Bill_Production billShouldDo = (observedThingHolder as IBillGiver)?.BillStack?.FirstShouldDoNow as Bill_Production;
                if (billShouldDo == null)
                {
                    return null;
                }

                return billShouldDo;
            }
        }

        public bool UpdateAvailIngredientsCache()
        {
            //QEEMod.TryLog("Updating bill ingredients cache");
            BillStack bills = (observedThingHolder as IBillGiver)?.BillStack;

            if(bills == null || AnyPendingRequests == false)
            {
                anyBillIngredientsAvailable = false;
                return false;
            }

            //TODO - loop over desiredRequests instead of bill ingredients?
            for (int i = 0; i < bills.Count; i++)
            {
                Bill curBill = bills[i];
                if (Find.TickManager.TicksGame < curBill.lastIngredientSearchFailTicks + ReCheckFailedBillTicksRange.RandomInRange)
                {
                    QEEMod.TryLog("checked " + curBill.GetUniqueLoadID() + " for avail. ingredients recently, skipping");
                    continue;
                }
                curBill.lastIngredientSearchFailTicks = 0;

                if (curBill.ShouldDoNow())
                {

                    //check if there are ingredients that this bill can use
                    Thing ing = IngredientUtility.FindClosestIngToBillGiver(curBill);
                    if (ing != null)
                    {
                        QEEMod.TryLog("Found " + ing.Label + " on map, adding to ingredient cache");
                        ingredientsAvailableNow[curBill.GetUniqueLoadID()] = ing;
                        anyBillIngredientsAvailable = true;
                        return true;
                    }
                    else
                    {
                        QEEMod.TryLog("no ingredients available");
                        curBill.lastIngredientSearchFailTicks = Find.TickManager.TicksGame;
                    }
                }
            }

            ingredientsAvailableNow.Clear();

            anyBillIngredientsAvailable = false;
            return false;
        }

        /// <summary>
        /// Checks if any Things were lost in the bill processor.
        /// </summary>
        public void ValidateDesiredRequests()
        {
            int elementsRemoved = 0;

            foreach (KeyValuePair<string, ThingOrderRequest> entry in desiredRequests)
            {
                ThingOrderRequest orderRequest = entry.Value;
                if (orderRequest != null)
                {
                    if (orderRequest.HasThing)
                    { 
                        if(orderRequest.thing.Destroyed)
                        {
                            //Log.Message("QEE: ABORTING CLONE! " + orderRequest.Label + " was destroyed");
                            elementsRemoved++;
                            desiredRequests.Remove(entry.Key);
                        }
                        else if (!(orderRequest.thing.ParentHolder is Pawn_CarryTracker ||
                      orderRequest.thing.ParentHolder is Map || orderRequest.thing.ParentHolder is IThingHolder))
                        {
                            //Log.Message("QEE: ABORTING CLONE! " + orderRequest.Label + " did not spawn in valid container");
                            elementsRemoved++;
                            desiredRequests.Remove(entry.Key);
                        }
                    }
                }
                else
                {
                    //Log.Message("QEE: orderRequest is null");
                }
            }

            if (elementsRemoved > 0)
            {
                requestsLost = true;
            }
        }

        public void Reset()
        {
            //Log.Message("QEE: Resetting desired ingredients...");
            desiredRequests.Clear();
            //ValidateDesiredRequests();
            ingredientsAvailableNow.Clear();
        }

        /// <summary>
        /// Call this whenever contents change
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

            Bill_Production theBill = ActiveBill;

            if (theBill?.recipe?.ingredients != null)
            {
                foreach (IngredientCount curIng in theBill.recipe.ingredients)
                {
                    ThingFilter filterCopy = new ThingFilter();
                    filterCopy.CopyAllowancesFrom(curIng.filter);

                    ThingOrderRequest request = new ThingOrderRequest(filterCopy);

                    //int storedCount = vatStoredIngredients.FirstOrDefault(thing => thing.def == curIng.FixedIngredient)?.stackCount ?? 0;
                    int storedCount = request.TotalStackCountForOrderRequestInContainer(ObservedThingOwner);
                    int countNeededFromRecipe = (int)(curIng.CountRequiredOfFor(curIng.FixedIngredient, theBill.recipe) *
                        QEESettings.instance.organTotalResourcesFloat);

                    int countNeededForCrafting = countNeededFromRecipe - storedCount;
                    countNeededForCrafting = countNeededForCrafting < 0 ? 0 : countNeededForCrafting;

                    if (countNeededForCrafting > 0)
                    {
                        QEEMod.TryLog("Adding " + curIng.FixedIngredient.label + " amount: " + countNeededForCrafting);

                        request.amount = countNeededForCrafting;

                        desiredRequests[curIng.FixedIngredient.defName] = request;
                    }
                }
            }
        }

        //Inherited
        //public void ExposeData()
        //{
        //    //Scribe_Collections.Look(ref desiredRequests, "desiredRequests", LookMode.Deep);
        //    Scribe_Values.Look(ref anyBillIngredientsAvailable, "anyBillIngredientsAvailable");
        //    if (Scribe.mode == LoadSaveMode.PostLoadInit)
        //    {
        //        Notify_ContentsChanged();
        //    }
        //}

        #endregion Methods
    }
}
