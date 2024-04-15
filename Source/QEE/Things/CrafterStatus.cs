namespace QEthics;

/// <summary>
///     THIS IS CLASS WILL BE DEPRECATED SOON. Do not use in new development work. It is now only used in the Pawn Vat.
///     Status for crafters deriving from QEthics.Building_GrowerBase.
/// </summary>
public enum CrafterStatus
{
    /// <summary>
    ///     Waiting for player input.
    /// </summary>
    Idle,

    /// <summary>
    ///     Colonists fill up the crafter with the desired things.
    /// </summary>
    Filling,

    /// <summary>
    ///     The crafter is now crafting until its finished and revert back to Idle status.
    /// </summary>
    Crafting,

    /// <summary>
    ///     The crafter is now finished. Awaiting the product to be ejected.
    /// </summary>
    Finished
}