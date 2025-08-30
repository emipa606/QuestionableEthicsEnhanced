using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace QEthics;

/// <summary>
///     Base class for grower buildings.
/// </summary>
public abstract class Building_GrowerBase : Building, IThingHolder
{
    /// <summary>
    ///     Current progress being made during crafting.
    /// </summary>
    protected int craftingProgress;

    private GrowerProperties growerPropsInt;

    /// <summary>
    ///     Internal container representation of stored items.
    /// </summary>
    public ThingOwner<Thing> innerContainer;

    /// <summary>
    ///     The crafter order processor. Is set by the player during Idle status.
    /// </summary>
    public ThingOrderProcessor orderProcessor;

    /// <summary>
    ///     Status of this crafter.
    /// </summary>
    public CrafterStatus status = CrafterStatus.Idle;

    protected Building_GrowerBase()
    {
        innerContainer = new ThingOwner<Thing>(this, false);
        orderProcessor = new ThingOrderProcessor(this);
    }

    /// <summary>
    ///     Grower building properties.
    /// </summary>
    public GrowerProperties GrowerProps
    {
        get
        {
            if (growerPropsInt != null)
            {
                return growerPropsInt;
            }

            growerPropsInt = def.GetModExtension<GrowerProperties>();

            //Fallback; Is defaults.
            if (growerPropsInt == null)
            {
                growerPropsInt = new GrowerProperties();
            }

            return growerPropsInt;
        }
    }

    protected CompPowerTrader PowerTrader => GetComp<CompPowerTrader>();

    /// <summary>
    ///     Ticks needed until the crafting is finished.
    /// </summary>
    protected abstract int TicksNeededToCraft { get; }

    protected int TicksLeftToCraft => TicksNeededToCraft - craftingProgress;

    protected float CraftingProgressPercent
    {
        get
        {
            if (TicksNeededToCraft == 0)
            {
                return 1;
            }

            var percentComplete = craftingProgress / (float)TicksNeededToCraft;
            return percentComplete > 1.0f ? 1.0f : percentComplete;
        }
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return innerContainer;
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref craftingProgress, "craftingProgress");
        Scribe_Values.Look(ref status, "status");
        // Use the ThingOwner-specific overload so Scribe sets the parent correctly and doesn't use Activator
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_Deep.Look(ref orderProcessor, "orderProcessor", this);
        /*if(Scribe.mode == LoadSaveMode.LoadingVars)
        {
            orderProcessor.Notify_ContentsChanged();
        }*/
    }

    public override string GetInspectString()
    {
        var builder = new StringBuilder(base.GetInspectString());

        //Status
        builder.AppendLine();
        builder.AppendLine("QE_GrowerStatus".Translate() + ": " +
                           TransformStatusLabel(("QE_GrowerStatus_" + status).Translate()));

        //Ingredients: Needed
        if (status == CrafterStatus.Filling)
        {
            builder.AppendLine("QE_GrowerIngredientsNeeded".Translate() + ": " +
                               orderProcessor.FormatCachedIngredientsInThingOrderProcessor());
        }

        //Crafting Progress
        /*if (status == CrafterStatus.Crafting)
        {
            builder.AppendLine("QE_GrowerCraftingProgress".Translate() + ": " + CraftingProgressPercent.ToStringPercent());
        }*/

        //Ingredients: Filled
        /*if (innerContainer.Count > 0)
        {
            builder.AppendLine("QE_GrowerIngredientsFilled".Translate() + ": " + innerContainer.FormatIngredientsInThingOwner());
        }*/

        return builder.ToString().TrimEndNewlines();
    }

    protected virtual string TransformStatusLabel(string label)
    {
        return label;
    }

    public override void Tick()
    {
        base.Tick();

        if (!this.IsHashIntervalTick(60))
        {
            return;
        }

        switch (status)
        {
            case CrafterStatus.Idle:
            {
                Tick_Idle();
            }
                break;
            case CrafterStatus.Filling:
            {
                //Check if any Things are lost in the order processor.
                orderProcessor.Cleanup();

                if (orderProcessor.requestsLost)
                {
                    //Abort if any of the requests were lost.
                    Reset();
                    Notify_ThingLostInOrderProcessor();
                    orderProcessor.requestsLost = false;
                    Tick_Idle();
                }
                else
                {
                    Tick_Filling();
                }
            }
                break;
            case CrafterStatus.Crafting:
            {
                Tick_Crafting();
            }
                break;
            case CrafterStatus.Finished:
            {
                Tick_Finished();
            }
                break;
        }
    }

    /// <summary>
    ///     Idle tick.
    /// </summary>
    protected virtual void Tick_Idle()
    {
    }

    /// <summary>
    ///     Filling tick.
    /// </summary>
    protected virtual void Tick_Filling()
    {
        if (orderProcessor.PendingRequests.Any())
        {
            return;
        }

        //QEEMod.TryLog("PendingRequests is 0. Starting crafting.");
        status = CrafterStatus.Crafting;
        Notify_CraftingStarted();
    }

    protected virtual void Notify_ThingLostInOrderProcessor()
    {
    }

    public virtual void Notify_StartedCarryThing(Pawn pawn)
    {
    }

    protected virtual void Notify_CraftingStarted()
    {
    }

    protected virtual void Notify_CraftingFinished()
    {
    }

    /// <summary>
    ///     Crafting tick.
    /// </summary>
    protected virtual void Tick_Crafting()
    {
        //Increment crafting.
        var doCrafting = PowerTrader is not { PowerOn: false };

        if (!doCrafting)
        {
            return;
        }

        craftingProgress += 60;
        if (craftingProgress < TicksNeededToCraft)
        {
            return;
        }

        craftingProgress = TicksNeededToCraft;

        status = CrafterStatus.Finished;

        Notify_CraftingFinished();
    }

    /// <summary>
    ///     Finished tick.
    /// </summary>
    protected virtual void Tick_Finished()
    {
    }

    /// <summary>
    ///     The crafting has finished.
    /// </summary>
    protected virtual void CraftingFinished()
    {
        Reset();
    }

    protected virtual void Reset()
    {
        craftingProgress = 0;
        status = CrafterStatus.Idle;
    }

    public void FillThing(Thing thing)
    {
        if (thing == null)
        {
            return;
        }

        innerContainer.TryAddOrTransfer(thing);
        orderProcessor.Notify_ContentsChanged();
    }

    public virtual bool TryExtractProduct(Pawn actor)
    {
        return true;
    }
}