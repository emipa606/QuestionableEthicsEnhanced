using System.Collections.Generic;
using System.Linq;
using Verse;

namespace QEthics;

/// <summary>
///     Utility functions for ThingFilters.
/// </summary>
public static class ThingFilterUtility
{
    public static int TotalStackCountForOrderRequestInContainer(this ThingOrderRequest request, ThingOwner thingOwner)
    {
        if (request == null)
        {
            return 0;
        }

        if (thingOwner.Count <= 0)
        {
            return 0;
        }

        var result = 0;
        if (request.HasThing)
        {
            result += thingOwner.Where(thing => thing == request.thing).Select(thing => thing.stackCount).Sum();
        }

        if (!request.HasThingFilter)
        {
            return result;
        }

        foreach (var def in request.thingFilter.AllowedThingDefs)
        {
            result += thingOwner.TotalStackCountOfDef(def);
        }

        return result;
    }

    private static int TotalStackCountOfDefInList(this IList<Thing> thingList, ThingDef def)
    {
        if (thingList.Count <= 0)
        {
            return 0;
        }

        var result = 0;
        foreach (var thing in thingList)
        {
            if (thing.def == def)
            {
                result += thing.stackCount;
            }
        }

        return result;
    }

    extension(ThingFilter filter)
    {
        public int TotalStackCountForFilterInContainer(ThingOwner thingOwner)
        {
            if (filter == null)
            {
                return 0;
            }

            if (thingOwner.Count <= 0)
            {
                return 0;
            }

            var result = 0;
            foreach (var def in filter.AllowedThingDefs)
            {
                result += thingOwner.TotalStackCountOfDef(def);
            }

            return result;
        }

        public int TotalStackCountForFilterInList(IList<Thing> thingList)
        {
            if (filter == null)
            {
                return 0;
            }

            if (thingList.Count <= 0)
            {
                return 0;
            }

            var result = 0;
            foreach (var def in filter.AllowedThingDefs)
            {
                result += thingList.TotalStackCountOfDefInList(def);
            }

            return result;
        }
    }
}