using System.Collections.Generic;
using Verse;

namespace QEthics;

/// <summary>
///     Helps process orders for Things in recipes.
/// </summary>
public class ThingOrderProcessor(IThingHolder observedThingHolder) : IExposable
{
    /// <summary>
    ///     Contains the list of cached requests.
    /// </summary>
    private readonly List<ThingOrderRequest> cachedRequests = [];

    /// <summary>
    ///     The holder object we are observing orders for.
    /// </summary>
    public readonly IThingHolder observedThingHolder = observedThingHolder;

    /// <summary>
    ///     Things we desire that can be anything.
    /// </summary>
    public List<ThingOrderRequest> desiredIngredients = [];

    public bool requestsLost;

    //Constructors.
    public ThingOrderProcessor() : this(null)
    {
    }

    public ThingOwner ObservedThingOwner => observedThingHolder.GetDirectlyHeldThings();

    public IEnumerable<ThingOrderRequest> PendingRequests => cachedRequests.AsReadOnly();

    //Inherited
    public void ExposeData()
    {
        Scribe_Collections.Look(ref desiredIngredients, "desiredThings", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            Notify_ContentsChanged();
        }
    }

    //Functions
    //this function checks if any Things are lost in the order processor
    public void Cleanup()
    {
        var elementsRemoved = 0;

        //expand the lamda from the original QE mod, to allow better debug logging
        for (var index = 0; index < desiredIngredients.Count; index++)
        {
            var orderRequest = desiredIngredients[index];
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
                QEEMod.TryLog($"QEE: ABORTING CLONE! {orderRequest.Label} was destroyed");
                elementsRemoved++;
                desiredIngredients.Remove(orderRequest);
            }
            else if (orderRequest.thing.ParentHolder is not (Pawn_CarryTracker or Map or not null))
            {
                QEEMod.TryLog($"QEE: ABORTING CLONE! {orderRequest.Label} did not spawn in valid container");
                elementsRemoved++;
                desiredIngredients.Remove(orderRequest);
            }
            //QEEMod.TryLog("QEE: orderRequest is null");
        }
        /*
        int elementsRemoved = desiredIngredients.RemoveAll(orderRequest => orderRequest == null ||
           (orderRequest.HasThing &&
              //                               Check if not spawned in a valid container.
              (orderRequest.thing.Destroyed || !(orderRequest.thing.ParentHolder is Pawn_CarryTracker ||
              orderRequest.thing.ParentHolder is Map || orderRequest.thing.ParentHolder is IThingHolder))
           )
        );*/

        if (elementsRemoved > 0)
        {
            requestsLost = true;
        }
    }

    public void Reset()
    {
        //QEEMod.TryLog("QEE: Resetting cached and desired ingredients.");
        cachedRequests.Clear();
        desiredIngredients.Clear();
    }

    /// <summary>
    ///     Call this whenever contents change or upon loading the game.
    /// </summary>
    public void Notify_ContentsChanged()
    {
        Cleanup();

        //Recache our requests
        cachedRequests.Clear();

        var desiredRequests = GetDesiredRequests();
        if (desiredRequests != null)
        {
            cachedRequests.AddRange(desiredRequests);
            //QEEMod.TryLog("QEE: cachedRequest count:" + PendingRequests.Count());
        }
        //QEEMod.TryLog("QEE: Notify_ContentsChanged() desiredRequests is null!");
    }

    /// <summary>
    ///     Returns a series of ThingOrderRequests.
    /// </summary>
    /// <returns>Thing order requests.</returns>
    public IEnumerable<ThingOrderRequest> GetDesiredRequests()
    {
        if (observedThingHolder == null)
        {
            yield break;
        }

        //Desired other things.
        foreach (var desiredIngredient in desiredIngredients)
        {
            var countDifference = desiredIngredient.amount -
                                  desiredIngredient.TotalStackCountForOrderRequestInContainer(ObservedThingOwner);
            //QEEMod.TryLog("QEE: " + desiredIngredient.Label + "; countDifference: " + countDifference);
            if (countDifference > 0)
            {
                yield return new ThingOrderRequest(desiredIngredient, countDifference);
            }
        }
        //QEEMod.TryLog("QEE: GetDesiredRequests() observedThingHolder is null!");
    }
}