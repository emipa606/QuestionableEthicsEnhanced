using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace QEthics;

/// <summary>
///     Utility functions for dealing with ingredients.
/// </summary>
public static class IngredientUtility
{
    public static Thing FindClosestRequestForThingOrderProcessor(ThingOrderProcessor orderProcessor, Pawn finder)
    {
        var result = GenClosest.ClosestThingReachable(finder.Position, finder.Map,
            ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.OnCell, TraverseParms.For(finder),
            validator:
            delegate(Thing thing)
            {
                if (thing.IsForbidden(finder))
                {
                    return false;
                }

                foreach (var request in orderProcessor.PendingRequests)
                {
                    if (request.ThingMatches(thing))
                    {
                        return true;
                    }
                }

                return false;
            });

        return result;
    }

    public static string FormatIngredientsInThingOrderProcessor(this ThingOrderProcessor orderProcessor,
        string format = "{0} x {1}", char delimiter = ',')
    {
        var builder = new StringBuilder();

        //Ingredients Wanted
        foreach (var ingredient in orderProcessor.desiredIngredients)
        {
            builder.Append(string.Format(format, ingredient.LabelCap, ingredient.amount));
            if (orderProcessor.desiredIngredients.Count <= 1 || ingredient == orderProcessor.desiredIngredients.Last())
            {
                continue;
            }

            builder.Append(delimiter);
            builder.Append(' ');
        }

        return builder.ToString().TrimEndNewlines();
    }

    public static string FormatCachedIngredientsInThingOrderProcessor(this ThingOrderProcessor orderProcessor,
        string format = "{0} x {1}", char delimiter = ',')
    {
        var builder = new StringBuilder();

        //Ingredients Requested
        foreach (var request in orderProcessor.PendingRequests)
        {
            builder.Append(string.Format(format, request.LabelCap, request.amount));
            if (orderProcessor.PendingRequests.Count() <= 1 || request == orderProcessor.PendingRequests.Last())
            {
                continue;
            }

            builder.Append(delimiter);
            builder.Append(' ');
        }

        return builder.ToString().TrimEndNewlines();
    }

    public static string FormatIngredientsInThingOwner(this ThingOwner thingOwner, string format = "{0} x {1}",
        char delimiter = ',')
    {
        var builder = new StringBuilder();

        //Ingredients Wanted
        foreach (var ingredient in thingOwner)
        {
            builder.Append(string.Format(format, ingredient.LabelCapNoCount, ingredient.stackCount));
            if (thingOwner.Count <= 1 || ingredient == thingOwner.Last())
            {
                continue;
            }

            builder.Append(delimiter);
            builder.Append(' ');
        }

        return builder.ToString().TrimEndNewlines();
    }

    public static void FillOrderProcessorFromVatGrowerRecipe(ThingOrderProcessor orderProcessor,
        GrowerRecipeDef recipeDef)
    {
        foreach (var ingredientCount in recipeDef.ingredients)
        {
            var filterCopy = new ThingFilter();
            filterCopy.CopyAllowancesFrom(ingredientCount.filter);

            var copy = new ThingOrderRequest(filterCopy)
            {
                amount = (int)(ingredientCount.GetBaseCount() * QEESettings.instance.organTotalResourcesFloat)
            };

            orderProcessor.desiredIngredients.Add(copy);
        }
    }

    public static void FillOrderProcessorFromPawnKindDef(ThingOrderProcessor orderProcessor, PawnKindDef pawnKind)
    {
        var pawnThingDef = pawnKind.race;
        var raceProps = pawnThingDef.race;

        //Assemble all required materials for a fully grown adult.
        var meatDef = raceProps.meatDef;
        var meatBaseNutrition = meatDef.ingestible.CachedNutrition;
        var meatAmount = pawnThingDef.GetStatValueAbstract(StatDefOf.MeatAmount);

        var proteinAmount =
            //protein cost = Meat * base Meat nutrition (vanilla is .05) * magic multiplier of 27
            //Minimum protein cost is 25, max is 750. Multiply by mod setting for clone ingredients
            //finally, round up to the nearest number divisible by 5
            (int)(Math.Ceiling((meatAmount * meatBaseNutrition * 27f).Clamp(25, 750)
                * QEESettings.instance.cloneTotalResourcesFloat / 5) * 5);

        var nutritionAmount = 4 * proteinAmount;

        {
            var orderRequest = new ThingOrderRequest(QEThingDefOf.QE_ProteinMash, proteinAmount);
            orderProcessor.desiredIngredients.Add(orderRequest);
        }

        {
            var orderRequest = new ThingOrderRequest(QEThingDefOf.QE_NutrientSolution, nutritionAmount);
            orderProcessor.desiredIngredients.Add(orderRequest);
        }
    }

    public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
    {
        if (val.CompareTo(min) < 0)
        {
            return min;
        }

        return val.CompareTo(max) > 0 ? max : val;
    }

    /// <summary>
    ///     Finds the closest Thing on the map to a pawn that matches the ThingDef needed by the Bill.
    ///     The available ingredients are cached in the BillProcessor variable in Building_GrowerBase_WorkTable.
    ///     If the cached ThingDef is no longer available, it will look for another ThingDef that's needed.
    /// </summary>
    /// <param name="theBill"></param>
    /// <param name="finder"></param>
    /// <param name="countForVat"></param>
    /// <returns></returns>
    public static Thing ThingPawnShouldRetrieveForBill(Bill theBill, Pawn finder, ref int countForVat)
    {
        var vat = theBill.billStack.billGiver as Building_GrowerBase_WorkTable;
        var unused = vat?.GetDirectlyHeldThings();

        Thing cachedThing = null;
        var unused1 =
            vat != null && vat.billProc.ingredientsAvailableNow.TryGetValue(theBill.GetUniqueLoadID(), out cachedThing);
        if (cachedThing == null)
        {
            QEEMod.TryLog("ThingPawnShouldRetrieveForBill() returning null. Reason - cachedThing is null");
            return null;
        }

        vat.billProc.desiredRequests.TryGetValue(cachedThing.def.defName, out var desiredRequest);

        //check that the vat still needs the cached ingredient before searching the map for the same ThingDef
        if (desiredRequest is not { amount: > 0 })
        {
            QEEMod.TryLog(
                $"Cached ingredient {cachedThing.LabelShort} is already fulfilled in vat. Looking for next ingredient in recipe");

            //this ingredient isn't in desiredIngredients or the vat has the full amount. Refresh desiredIngredients and try once more
            vat.billProc.UpdateDesiredRequests();

            //now get a random item in the dictionary of desired requests
            foreach (var value in vat.billProc.desiredRequests.Values)
            {
                desiredRequest = value;
            }

            //return if there's no thingDef or desiredRequest is null
            if (desiredRequest == null)
            {
                QEEMod.TryLog(
                    "ThingPawnShouldRetrieveForBill() returning null. Reason - Desired ThingOrderRequest is null");
                return null;
            }

            //return if the amount for this request is 0
            if (desiredRequest.amount <= 0)
            {
                QEEMod.TryLog(
                    $"ThingPawnShouldRetrieveForBill() returning null. Reason - Desired Thing {desiredRequest.Label} has 0 amount");
                return null;
            }
        }

        var tRequest = desiredRequest.GetThingRequest();

        if (tRequest.IsUndefined)
        {
            QEEMod.TryLog(
                $"ThingPawnShouldRetrieveForBill() returning null. Reason - ThingRequest for {desiredRequest.Label} returned undefined ThingRequest");
            return null;
        }

        countForVat = desiredRequest.amount;


        QEEMod.TryLog($"Searching map for closest {desiredRequest.Label} to {finder.LabelShort}");

        //search the map for the closest Thing to the pawn that matches the ThingDef in 'tRequest'
        var result = GenClosest.ClosestThingReachable(finder.Position, finder.Map, tRequest,
            PathEndMode.OnCell, TraverseParms.For(finder),
            validator:
            delegate(Thing testThing)
            {
                if (!tRequest.Accepts(testThing))
                {
                    return false;
                }

                return !testThing.IsForbidden(finder) && finder.CanReserve(testThing);
            });

        if (result != null)
        {
            QEEMod.TryLog(
                $"{finder.LabelShort} should retrieve: {result.Label} | stackCount: {result.stackCount} | countForVat: {countForVat}");
        }

        return result;
    } //end FindClosestIngForBill

    public static Thing FindClosestIngToBillGiver(Bill theBill, IngredientCount curIng)
    {
        var billGiver = theBill.billStack.billGiver;
        var holder = billGiver as IThingHolder;
        var building = billGiver as Thing;
        var vatStoredIngredients = holder?.GetDirectlyHeldThings();

        if (billGiver == null || building == null || vatStoredIngredients == null)
        {
            return null;
        }

        var countNeededForCrafting = RemainingCountForIngredient(vatStoredIngredients, theBill.recipe, curIng);

        //only check the map for Things if the vat still needs some of this ingredient
        if (countNeededForCrafting <= 0)
        {
            return null;
        }

        //find the closest accessible Thing of that ThingDef on the map
        var tRequest = ThingRequest.ForDef(curIng.FixedIngredient);

        IEnumerable<Thing> searchSet = billGiver.Map.listerThings.ThingsMatching(tRequest);
        var result = GenClosest.ClosestThing_Global(building.Position, searchSet,
            validator:
            delegate(Thing testThing)
            {
                if (testThing.def.defName != curIng.FixedIngredient.defName)
                {
                    return false;
                }

                return !testThing.IsForbidden(building.Faction);
            });

        //return the Thing, if we found one
        //QEEMod.TryLog("Ingredient found: " + curIng.FixedIngredient.label + " | stackCount: " + result.stackCount + " | recipe: "
        //    + countNeededFromRecipe);
        return result;
    } //end function FindClosestIngToBillGiver

    public static int RemainingCountForIngredient(ThingOwner container, RecipeDef recipe, IngredientCount ingCount)
    {
        var wantedCount = TotalCountForIngredient(recipe, ingCount);

        var haveCount = 0;
        if (container != null && ingCount.filter != null)
        {
            haveCount = ingCount.filter.TotalStackCountForFilterInContainer(container);
        }

        return wantedCount - haveCount < 0 ? 0 : wantedCount - haveCount;
    }

    public static int TotalCountForIngredient(RecipeDef recipe, IngredientCount ingCount)
    {
        return (int)(ingCount.CountRequiredOfFor(ingCount.FixedIngredient, recipe) *
                     QEESettings.instance.organTotalResourcesFloat);
    }
} //end class IngredientUtility