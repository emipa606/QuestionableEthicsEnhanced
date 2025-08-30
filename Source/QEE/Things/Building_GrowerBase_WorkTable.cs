﻿using System.Collections.Generic;
using System.Text;
using BioReactor;
using RimWorld;
using Verse;

namespace QEthics;

/// <summary>
///     Base class for grower buildings.
/// </summary>
public abstract class Building_GrowerBase_WorkTable : Building_WorkTable, IThingHolder, IMaintainableGrower,
    IBillGiverExtension
{
    protected static readonly SimpleCurve cleanlinessCurve = [];

    /// <summary>
    ///     Current active recipe being crafted.
    /// </summary>
    public RecipeDef activeRecipe;

    /// <summary>
    ///     Helps process bills for this building.
    /// </summary>
    public BillProcessor billProc;

    /// <summary>
    ///     Current progress being made during crafting.
    /// </summary>
    protected int craftingProgress;

    /// <summary>
    ///     From 0.0 to 1.0. If the maintenance hits 0, the recipe should fail. Default: 25%
    /// </summary>
    protected float doctorMaintenance = 0.25f;


    private GrowerProperties growerPropsInt;

    /// <summary>
    ///     Internal container representation of stored items.
    /// </summary>
    public ThingOwner<Thing> ingredientContainer;


    /// <summary>
    ///     From 0.0 to 1.0. If the maintenance hits 0, the recipe should fail. Default: 25%
    /// </summary>
    protected float scientistMaintenance = 0.25f;

    /// <summary>
    ///     Status of this crafter.
    /// </summary>
    public CrafterStatus status = CrafterStatus.Idle;

    static Building_GrowerBase_WorkTable()
    {
        cleanlinessCurve.Add(-5.0f, 5.00f);
        cleanlinessCurve.Add(-2.0f, 1.75f);
        cleanlinessCurve.Add(0.0f, 1.0f);
        cleanlinessCurve.Add(0.4f, 0.35f);
        cleanlinessCurve.Add(2.0f, 0.1f);
    }

    protected Building_GrowerBase_WorkTable()
    {
        ingredientContainer = new ThingOwner<Thing>(this, false);
        billProc = new BillProcessor(this);
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


    public void Notify_BillAdded(Bill theBill)
    {
        //QEEMod.TryLog("Bill Added!");
        billProc.UpdateDesiredRequests();
        billProc.UpdateAvailIngredientsCache();
    }

    public override void Notify_BillDeleted(Bill theBill)
    {
        //QEEMod.TryLog("Bill Removed!");
        base.Notify_BillDeleted(theBill);
        billProc.UpdateDesiredRequests();
        billProc.UpdateAvailIngredientsCache();
    }

    public float RoomCleanliness
    {
        get
        {
            var room = this.GetRoom(RegionType.Set_Passable);
            return room?.GetStat(RoomStatDefOf.Cleanliness) ?? 0f;
        }
    }

    public float ScientistMaintenance
    {
        get => scientistMaintenance;
        set => scientistMaintenance = value;
    }

    public float DoctorMaintenance
    {
        get => doctorMaintenance;
        set => doctorMaintenance = value;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return ingredientContainer;
    }


    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref craftingProgress, "craftingProgress");
        Scribe_Values.Look(ref status, "status");
        // Use the ThingOwner-specific overload
        Scribe_Deep.Look(ref ingredientContainer, "ingredientContainer", this);
        Scribe_Values.Look(ref scientistMaintenance, "scientistMaintenance");
        Scribe_Values.Look(ref doctorMaintenance, "doctorMaintenance");
        Scribe_Defs.Look(ref activeRecipe, "activeRecipe");
        Scribe_Deep.Look(ref billProc, "billProc", this);

        //if (Scribe.mode == LoadSaveMode.PostLoadInit)
        //{
        //    billProc.anyBillIngredientsAvailable = true;
        //}
    }

    public override string GetInspectString()
    {
        if (ParentHolder is not Verse.Map)
        {
            return null;
        }

        var builder = new StringBuilder(base.GetInspectString());

        //Status
        builder.AppendLine();
        builder.AppendLine("QE_GrowerStatus".Translate() + ": " +
                           TransformStatusLabel(("QE_GrowerStatus_" + status).Translate()));

        //Ingredients: Needed
        if (status == CrafterStatus.Filling)
        {
            builder.Append("QE_GrowerIngredientsNeeded".Translate() + ": " + formatCachedIngredients());
        }

        return builder.ToString().TrimEndNewlines();
    }

    private string formatCachedIngredients(string format = "{0} x {1}", char delimiter = ',')
    {
        var builder = new StringBuilder();

        foreach (var currIngredient in activeRecipe.ingredients)
        {
            var remainingCount =
                IngredientUtility.RemainingCountForIngredient(ingredientContainer, activeRecipe, currIngredient);

            if (remainingCount <= 0)
            {
                continue;
            }

            //add a delimiter if any ingredients were added in previous iterations
            if (builder.Length > 0)
            {
                builder.Append(delimiter);
                builder.Append(' ');
            }

            builder.Append(string.Format(format, currIngredient.FixedIngredient.LabelCap, remainingCount));
        }

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
                Tick_Idle();
                break;
            case CrafterStatus.Filling:
                Tick_Filling();
                break;
            case CrafterStatus.Crafting:
                Tick_Crafting();
                break;
            case CrafterStatus.Finished:
                Tick_Finished();
                break;
        }
    }

    /// <summary>
    ///     Idle tick.
    /// </summary>
    protected virtual void Tick_Idle()
    {
        if (!this.IsHashIntervalTick(60))
        {
            return;
        }

        billProc.UpdateDesiredRequests();


        if (!this.IsHashIntervalTick(QEESettings.instance.ingredientCheckIntervalSeconds * 60))
        {
            return;
        }

        if (HarmonyPatches.VNPELoaded)
        {
            Building_GrowerBase_WorkTable_VNPE.VNPE_Check(this);
        }

        billProc.UpdateAvailIngredientsCache();
    }

    /// <summary>
    ///     Filling tick.
    /// </summary>
    protected virtual void Tick_Filling()
    {
        if (!this.IsHashIntervalTick(60))
        {
            return;
        }

        //Check if any Things are lost in the order processor.
        billProc.ValidateDesiredRequests();

        if (billProc.requestsLost)
        {
            //Abort if any of the requests were lost.
            StopCrafting();
            Notify_ThingLostInBillProcessor();
            billProc.requestsLost = false;
            Tick_Idle();
            return;
        }

        if (!this.IsHashIntervalTick(QEESettings.instance.ingredientCheckIntervalSeconds * 60))
        {
            return;
        }

        if (HarmonyPatches.VNPELoaded)
        {
            Building_GrowerBase_WorkTable_VNPE.VNPE_Check(this);
        }

        billProc.UpdateAvailIngredientsCache();
    }

    /// <summary>
    ///     Crafting tick.
    /// </summary>
    protected virtual void Tick_Crafting()
    {
        //Increment crafting.
        float powerModifier;
        if (PowerTrader == null || PowerTrader.PowerOn)
        {
            powerModifier = 1f;
            craftingProgress += 60;
            if (craftingProgress >= TicksNeededToCraft)
            {
                craftingProgress = TicksNeededToCraft;

                status = CrafterStatus.Finished;

                Notify_CraftingFinished();
            }
        }
        else
        {
            powerModifier = 15f;
        }

        //Set maintenance to decay
        var cleanlinessModifer = cleanlinessCurve.Evaluate(RoomCleanliness);
        var decayRate = 0.0012f * cleanlinessModifer * powerModifier / QEESettings.instance.maintRateFloat;

        scientistMaintenance -= decayRate;
        doctorMaintenance -= decayRate;
    }

    /// <summary>
    ///     Finished tick.
    /// </summary>
    protected virtual void Tick_Finished()
    {
    }

    public virtual void Notify_StartedCarryThing(Pawn pawn)
    {
    }

    public virtual void Notify_FillingStarted(Bill activeBill)
    {
    }

    public virtual void Notify_CraftingStarted()
    {
    }

    protected virtual void Notify_CraftingFinished()
    {
    }

    protected virtual void Notify_ThingLostInBillProcessor()
    {
    }

    public void FillThing(Thing thing)
    {
        if (thing == null)
        {
            return;
        }

        ingredientContainer.TryAddOrTransfer(thing);
        billProc.Notify_ContentsChanged();
    }

    public virtual void Notify_ProductExtracted(Pawn actor)
    {
    }

    /// <summary>
    ///     Stops the growing process and resets the vat. Refunds any ingredients used, if keepIngredients is true.
    ///     This function is called if growing fails, succeeds, or is manually stopped via the Stop gizmo.
    ///     It does not spawn any products.
    /// </summary>
    /// <param name="keepIngredients"></param>
    public virtual void StopCrafting(bool keepIngredients = true)
    {
        QEEMod.TryLog($"Stopping growing process. Keep Ingredients: {keepIngredients}");

        craftingProgress = 0;
        status = CrafterStatus.Idle;
        activeRecipe = null;
        billProc.Reset();

        if (ingredientContainer is { Count: > 0 })
        {
            QEEMod.TryLog($"Ingredient container count: {ingredientContainer.Count}");

            if (keepIngredients)
            {
                _ = ingredientContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
                //QEEMod.TryLog("TryDropAll() success: " + wasSuccess);
            }
        }

        ingredientContainer.ClearAndDestroyContents();
    }
}