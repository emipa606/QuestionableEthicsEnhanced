using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace QEthics;

/// <summary>
///     Building for growing things like organs. Requires constant maintenance in order to not botch the crafting. Dirty
///     rooms increase maintenance drain even more.
/// </summary>
public class Building_OrganVat : Building_GrowerBase_WorkTable
{
    private VatGrowerProperties vatGrowerPropsInt;

    /// <summary>
    ///     Ticks required to craft organ. Takes organGrowthRate ModSetting into account.
    /// </summary>
    public override int TicksNeededToCraft =>
        (int)(activeRecipe?.workAmount * QEESettings.instance.organGrowthRateFloat ?? 0);

    public VatGrowerProperties VatGrowerProps
    {
        get
        {
            if (vatGrowerPropsInt != null)
            {
                return vatGrowerPropsInt;
            }

            vatGrowerPropsInt = def.GetModExtension<VatGrowerProperties>();

            //Fallback; Is defaults.
            if (vatGrowerPropsInt == null)
            {
                vatGrowerPropsInt = new VatGrowerProperties();
            }

            return vatGrowerPropsInt;
        }
    }

    public override string GetInspectString()
    {
        var builder = new StringBuilder(base.GetInspectString());

        if (status != CrafterStatus.Crafting)
        {
            return builder.ToString().TrimEndNewlines();
        }

        builder.AppendLine();
        builder.AppendLine("QE_VatGrowerMaintenance".Translate($"{scientistMaintenance:0%}",
            $"{doctorMaintenance:0%}"));

        builder.AppendLine(
            "QE_VatGrowerCleanlinessMult".Translate(cleanlinessCurve.Evaluate(RoomCleanliness).ToString("0.00")));

        return builder.ToString().TrimEndNewlines();
    }

    public override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        //Draw bottom graphic
        var drawAltitude = drawLoc;
        VatGrowerProps.bottomGraphic?.Graphic.Draw(drawAltitude, Rotation, this);

        //Draw product
        drawAltitude += new Vector3(0f, 0.005f, 0f);
        var recipeProps = activeRecipe?.GetModExtension<RecipeGraphicProperties>();

        if (status is CrafterStatus.Crafting or CrafterStatus.Finished && activeRecipe != null &&
            recipeProps?.productGraphic?.Graphic?.MatSingle != null)
        {
            var material = recipeProps.productGraphic.Graphic.MatSingle;
            var scale = (0.2f + (CraftingProgressPercent * 0.8f)) * VatGrowerProps.productScaleModifier *
                        recipeProps.scale;
            var scaleVector = new Vector3(scale, 1f, scale);
            var matrix = default(Matrix4x4);
            matrix.SetTRS(drawAltitude + VatGrowerProps.productOffset, Quaternion.AngleAxis(0f, Vector3.up),
                scaleVector);

            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        //Draw top graphic
        if (VatGrowerProps.topGraphic != null)
        {
            drawAltitude += new Vector3(0f, 0.005f, 0f);
            VatGrowerProps.topGraphic.Graphic.Draw(drawAltitude, Rotation, this);
        }

        //Draw top detail graphic
        if (VatGrowerProps.topDetailGraphic != null && (PowerTrader?.PowerOn ?? false))
        {
            drawAltitude += new Vector3(0f, 0.005f, 0f);
            VatGrowerProps.topDetailGraphic.Graphic.Draw(drawAltitude, Rotation, this);
        }


        //Draw glow graphic
        var usePowered = PowerTrader?.PowerOn == true;
        drawAltitude += new Vector3(0f, 0.005f, 0f);

        switch (status)
        {
            case CrafterStatus.Filling:
                if (activeRecipe == null)
                {
                    break;
                }

                var totalAmount = activeRecipe.ingredients.Sum(count =>
                    IngredientUtility.TotalCountForIngredient(activeRecipe, count));
                var currentAmount = totalAmount - activeRecipe.ingredients.Sum(count =>
                    IngredientUtility.RemainingCountForIngredient(ingredientContainer, activeRecipe, count));

                var fraction = currentAmount / (float)totalAmount;
                var imageIndex = 0;
                if (fraction < 1f)
                {
                    imageIndex = 1;
                }

                if (fraction < 0.75f)
                {
                    imageIndex = 2;
                }

                if (fraction < 0.5f)
                {
                    imageIndex = 3;
                }

                if (fraction < 0.25f)
                {
                    break;
                }

                if (usePowered)
                {
                    VatGrowerProps.glowGraphic[imageIndex].Graphic.Draw(drawAltitude, Rotation, this);
                    break;
                }

                VatGrowerProps.glowGraphicUnpowered[imageIndex].Graphic.Draw(drawAltitude, Rotation, this);
                break;
            case CrafterStatus.Crafting:
            case CrafterStatus.Finished:
                if (usePowered)
                {
                    VatGrowerProps.glowGraphic[0].Graphic.Draw(drawAltitude, Rotation, this);
                    break;
                }

                VatGrowerProps.glowGraphicUnpowered[0].Graphic.Draw(drawAltitude, Rotation, this);
                break;
        }
    }

    /// <summary>
    ///     Initializes maintenance, active recipe, and bill variables used for growing. Sets CrafterStatus to 'Filling'.
    ///     Called from WorkGiver_DoBill_Grower.JobOnThing()
    /// </summary>
    /// <param name="billToUse"></param>
    public override void Notify_FillingStarted(Bill billToUse)
    {
        //update Bill Processor
        //IngredientUtility.UpdateBillProcessorDesiredIng(billProc, billToUse);

        //TODO - is this necessary?
        billProc.UpdateDesiredRequests();

        //Initialize maintenance

        activeRecipe = billToUse.recipe;

        status = CrafterStatus.Filling;
    }

    public override void Notify_CraftingStarted()
    {
        billProc.Notify_ContentsChanged();
        scientistMaintenance = 1f;
        doctorMaintenance = 1f;

        status = CrafterStatus.Crafting;
    }

    //now also does a history event. Should work.I think.
    public override void Notify_CraftingFinished()
    {
        Messages.Message("QE_MessageGrowingDone".Translate(activeRecipe.products[0].thingDef.LabelCap),
            new LookTargets(this), MessageTypeDefOf.PositiveEvent, false);
        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(QEHistoryDefOf.OrganGrown));
    }

    public override void Tick_Crafting()
    {
        base.Tick_Crafting();

        if (!(scientistMaintenance < 0f) && !(doctorMaintenance < 0f))
        {
            return;
        }

        //Fail the craft, waste all products.
        StopCrafting(false);
        if (activeRecipe?.products[0]?.thingDef?.defName != null)
        {
            Messages.Message(
                "QE_OrgaVatMaintFailMessage".Translate(activeRecipe.products[0].thingDef.defName
                    .Named("ORGANNAME")),
                new LookTargets(this), MessageTypeDefOf.NegativeEvent);
        }
        else
        {
            Messages.Message("QE_OrgaVatMaintFailFallbackMessage".Translate(), new LookTargets(this),
                MessageTypeDefOf.NegativeEvent);
        }
    }

    /// <summary>
    ///     Called from JobDriver_ExtractProductFromGrower. That Job is initiated by WorkGiver_ExtractProductFromGrower, when
    ///     crafting status is Finished.
    /// </summary>
    /// <param name="actor"></param>
    /// <returns></returns>
    public override void Notify_ProductExtracted(Pawn actor)
    {
        if (status == CrafterStatus.Finished)
        {
            StopCrafting(false);
        }
    }

    public override string TransformStatusLabel(string label)
    {
        string recipeLabel = activeRecipe?.LabelCap ?? "QE_VatGrowerNoRecipe".Translate();

        if (status is CrafterStatus.Filling or CrafterStatus.Finished)
        {
            return $"{label} {recipeLabel.CapitalizeFirst()}";
        }

        if (status != CrafterStatus.Crafting)
        {
            return base.TransformStatusLabel(label);
        }

        var daysRemaining = TicksLeftToCraft.TicksToDays();
        if (daysRemaining > 1.0)
        {
            return $"{recipeLabel.CapitalizeFirst()} ({daysRemaining:0.0} " +
                   "QE_VatGrowerDaysRemaining".Translate() + ")";
        }

        return
            $" {recipeLabel.CapitalizeFirst()} ({TicksLeftToCraft / 2500.0f:0.0} " +
            "QE_VatGrowerHoursRemaining".Translate() + ")";
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        if (status == CrafterStatus.Idle)
        {
            yield break;
        }

        var shouldRefundIngredients = status == CrafterStatus.Filling;

        if (status == CrafterStatus.Finished)
        {
            yield break;
        }

        yield return new Command_Action
        {
            defaultLabel = "QE_VatGrowerStopCraftingGizmoLabel".Translate(),
            defaultDesc = "QE_VatGrowerStopCraftingGizmoDescription".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel"),
            order = -100,
            action = delegate { StopCrafting(shouldRefundIngredients); }
        };
        if (Prefs.DevMode)
        {
            yield return new Command_Action
            {
                defaultLabel = "QE_VatGrowerDebugFinishGrowing".Translate(),
                defaultDesc = "QE_OrganVatDebugFinishGrowingDescription".Translate(),
                action = delegate { craftingProgress = TicksNeededToCraft; }
            };
        }
    }
} //end class Building_OrganVat