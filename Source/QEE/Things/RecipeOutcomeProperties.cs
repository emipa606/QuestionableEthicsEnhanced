using Verse;

namespace QEthics;

/// <summary>
///     Points to the ThingDef for the GenomeSequencer.
/// </summary>
public class RecipeOutcomeProperties : DefModExtension
{
    public HediffDef outcomeHediff;
    public float outcomeHediffPotency = 1f;
    public ThingDef outputThingDef;
}