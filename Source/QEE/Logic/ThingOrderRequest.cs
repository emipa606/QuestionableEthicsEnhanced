using Verse;

namespace QEthics;

/// <summary>
///     Represents a request for either a specified Thing or a ThingDef.
/// </summary>
public class ThingOrderRequest : IExposable
{
    /// <summary>
    ///     How many are being requested.
    /// </summary>
    public int amount = 1;

    /// <summary>
    ///     Label to use if its not null.
    /// </summary>
    public string customLabel;

    /// <summary>
    ///     Thing being requested.
    /// </summary>
    public Thing thing;

    /// <summary>
    ///     ThingDef from filter being requested.
    /// </summary>
    public ThingFilter thingFilter;

    public ThingOrderRequest()
    {
    }

    public ThingOrderRequest(ThingOrderRequest other)
    {
        customLabel = other.customLabel;
        thing = other.thing;
        amount = other.amount;
        if (other.HasThingFilter)
        {
            var filter = new ThingFilter();
            filter.CopyAllowancesFrom(other.thingFilter);
            thingFilter = filter;
        }

        Initialize();
    }

    public ThingOrderRequest(ThingOrderRequest other, int amount)
    {
        customLabel = other.customLabel;
        thing = other.thing;
        this.amount = amount;
        if (other.HasThingFilter)
        {
            var filter = new ThingFilter();
            filter.CopyAllowancesFrom(other.thingFilter);
            thingFilter = filter;
        }

        Initialize();
    }

    public ThingOrderRequest(int amount = 1)
    {
        this.amount = amount;
        Initialize();
    }

    public ThingOrderRequest(Thing thing, int amount = 1)
    {
        this.thing = thing;
        this.amount = amount;
        Initialize();
    }

    public ThingOrderRequest(Thing thing, ThingDef thingDef, int amount = 1)
    {
        this.thing = thing;
        var filter = new ThingFilter();
        filter.SetAllow(thingDef, true);
        thingFilter = filter;
        this.amount = amount;
        Initialize();
    }

    public ThingOrderRequest(ThingDef thingDef, int amount = 1)
    {
        var filter = new ThingFilter();
        filter.SetAllow(thingDef, true);
        thingFilter = filter;
        this.amount = amount;
        Initialize();
    }

    public ThingOrderRequest(ThingFilter thingFilter, int amount = 1)
    {
        this.thingFilter = thingFilter;
        this.amount = amount;
        Initialize();
    }

    public bool HasThing => thing != null;

    public bool HasThingFilter => thingFilter != null;

    public string Label
    {
        get
        {
            if (customLabel != null)
            {
                return customLabel;
            }

            if (HasThing)
            {
                return thing.LabelNoCount;
            }

            return HasThingFilter ? thingFilter.Summary : "<null>";
        }
    }

    public string LabelCap
    {
        get
        {
            if (customLabel != null)
            {
                return customLabel.CapitalizeFirst();
            }

            if (HasThing)
            {
                return thing.LabelCapNoCount;
            }

            return HasThingFilter ? thingFilter.Summary.CapitalizeFirst() : "<null>";
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref customLabel, "customLabel");
        Scribe_Deep.Look(ref thingFilter, "thingFilter");
        Scribe_References.Look(ref thing, "thing");
        Scribe_Values.Look(ref amount, "amount", 1);
    }

    public ThingRequest GetThingRequest()
    {
        if (HasThing)
        {
            return ThingRequest.ForDef(thing.def);
        }

        return HasThingFilter ? thingFilter.BestThingRequest : ThingRequest.ForUndefined();
    }

    /// <summary>
    ///     Initializes the the order request.
    /// </summary>
    public void Initialize()
    {
        thingFilter?.ResolveReferences();
    }

    public bool ThingMatches(Thing otherThing)
    {
        if (HasThing)
        {
            return thing == otherThing;
        }

        return HasThingFilter && thingFilter.Allows(otherThing);
    }

    public bool ThingDefMatches(Thing aThing)
    {
        if (HasThing)
        {
            return thing.def == aThing.def;
        }

        return HasThingFilter && thingFilter.Allows(aThing);
    }

    public static implicit operator ThingOrderRequest(Thing thing)
    {
        return new ThingOrderRequest(thing);
    }

    public static implicit operator ThingOrderRequest(ThingFilter thingFilter)
    {
        return new ThingOrderRequest(thingFilter);
    }

    public static implicit operator ThingOrderRequest(ThingDef thingDef)
    {
        return new ThingOrderRequest(thingDef);
    }
}