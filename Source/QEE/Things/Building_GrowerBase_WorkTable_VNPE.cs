using System;
using System.Linq;
using PipeSystem;
using QEthics;
using RimWorld;
using Verse;

namespace BioReactor;

[StaticConstructorOnStartup]
public static class Building_GrowerBase_WorkTable_VNPE
{
    public static void VNPE_Check(Building_GrowerBase_WorkTable grower)
    {
        if (!grower.BillStack.Bills.Any())
        {
            return;
        }

        var activeBills = grower.BillStack.Bills?.Where(bill => bill.ShouldDoNow());
        if (!activeBills.Any())
        {
            return;
        }

        var compResource = grower.GetComp<CompResource>();

        if (compResource is not { PipeNet: { } net })
        {
            return;
        }

        var stored = net.Stored;
        var bill = activeBills.First();
        var recipe = bill.recipe;

        var setActive = false;

        while (stored > 0 && anyIngredientsNeeded(grower, recipe))
        {
            var amountToAdd = 33;

            foreach (var ingredient in recipe.ingredients)
            {
                if (ingredient.FixedIngredient != QEThingDefOf.QE_NutrientSolution &&
                    ingredient.FixedIngredient != QEThingDefOf.QE_ProteinMash)
                {
                    continue;
                }

                var remainingToAdd =
                    IngredientUtility.RemainingCountForIngredient(grower.ingredientContainer, recipe, ingredient);

                if (remainingToAdd == 0)
                {
                    continue;
                }

                var thing = ThingMaker.MakeThing(ingredient.FixedIngredient);
                thing.stackCount = Math.Min(amountToAdd, remainingToAdd);
                amountToAdd -= thing.stackCount;
                grower.ingredientContainer.TryAddOrTransfer(thing);
                grower.billProc.Notify_ContentsChanged();
                grower.status = CrafterStatus.Filling;
                setActive = true;
            }

            if (amountToAdd == 33)
            {
                break;
            }

            net.DrawAmongStorage(1, net.storages);
            stored--;
        }

        if (!setActive)
        {
            return;
        }


        if (grower.billProc.ActiveBill == null)
        {
            grower.billProc.ActiveBill = bill as Bill_Production;
        }

        if (grower.activeRecipe == null)
        {
            grower.activeRecipe = recipe;
        }

        grower.billProc.UpdateAvailIngredientsCache();
        grower.billProc.UpdateDesiredRequests();
        if (!anyIngredientsNeeded(grower, recipe, true))
        {
            grower.status = CrafterStatus.Crafting;
        }
    }

    private static bool anyIngredientsNeeded(Building_GrowerBase_WorkTable grower, RecipeDef recipe, bool any = false)
    {
        foreach (var ingredient in recipe.ingredients)
        {
            if (!ingredient.IsFixedIngredient)
            {
                continue;
            }

            if (!any && ingredient.FixedIngredient != QEThingDefOf.QE_NutrientSolution &&
                ingredient.FixedIngredient != QEThingDefOf.QE_ProteinMash)
            {
                continue;
            }

            if (IngredientUtility.RemainingCountForIngredient(grower.ingredientContainer, recipe,
                    ingredient) > 0)
            {
                return true;
            }
        }

        return false;
    }
}