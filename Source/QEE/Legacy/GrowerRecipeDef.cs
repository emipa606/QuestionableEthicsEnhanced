using System.Collections.Generic;
using Verse;

namespace QEthics;

/// <summary>
///     Recipe for a Grower.
/// </summary>
public class GrowerRecipeDef : Def
{
    /// <summary>
    ///     How long it take to craft the recipe.
    /// </summary>
    public int craftingTime = 1;

    //public ThingFilter fixedIngredientFilter = new ThingFilter();

    /// <summary>
    ///     What ingredients the recipe need in order to start being produced.
    /// </summary>
    public List<IngredientCount> ingredients = [];

    /// <summary>
    ///     How much maintenance is drained per tick during crafting.
    /// </summary>
    public float maintenanceDrain = 0.001f;

    /// <summary>
    ///     Useful when sorting by order.
    /// </summary>
    public int orderID = 0;

    /// <summary>
    ///     Grower amount.
    /// </summary>
    public int productAmount = 1;

    /// <summary>
    ///     Grower product.
    /// </summary>
    public ThingDef productDef;

    /// <summary>
    ///     Graphic to draw while its being crafted.
    /// </summary>
    public GraphicData productGraphic;

    /// <summary>
    ///     Users that can use this recipe.
    /// </summary>
    public List<ThingDef> recipeUsers = [];

    /// <summary>
    ///     What research is needed to be completed in order to make this.
    /// </summary>
    public ResearchProjectDef requiredResearch;

    public override void ResolveReferences()
    {
        base.ResolveReferences();

        //fixedIngredientFilter.ResolveReferences();

        foreach (var ingredient in ingredients)
        {
            ingredient.ResolveReferences();
        }

        productGraphic?.ResolveReferencesSpecial();

        if (recipeUsers.Count <= 0)
        {
            return;
        }

        foreach (var def in recipeUsers)
        {
            if (def.GetModExtension<GrowerProperties>() is { } properties)
            {
                properties.recipes.Add(this);
            }
        }
    }

    /*public bool IsIngredient(ThingDef th)
    {
        for (int i = 0; i < ingredients.Count; i++)
        {
            if (ingredients[i].filter.Allows(th) && (ingredients[i].IsFixedIngredient))
            {
                return true;
            }
        }
        return false;
    }*/

    /*public void FillOrderProcessor(ThingOrderProcessor orderProcessor)
    {
        foreach(IngredientCount ingredientCount in ingredients)
        {
            ThingFilter filterCopy = new ThingFilter();
            filterCopy.CopyAllowancesFrom(ingredientCount.filter);

            ThingOrderRequest copy = new ThingOrderRequest(filterCopy);
            copy.amount = (int)ingredientCount.GetBaseCount();

            orderProcessor.desiredIngredients.Add(copy);
        }
    }*/
}