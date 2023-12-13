using System.Collections.Generic;
using Verse;

namespace QEthics;

/// <summary>
///     Properties for grower derived Buildings.
/// </summary>
public class GrowerProperties : DefModExtension
{
    /// <summary>
    ///     If true it requires a pawn to interact with the grower to extract the product.
    /// </summary>
    public bool productRequireManualExtraction = true;

    /// <summary>
    ///     Recipes that the Grower have.
    /// </summary>
    public List<GrowerRecipeDef> recipes = [];
}