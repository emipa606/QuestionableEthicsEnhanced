using Verse;

namespace QEthics;

/// <summary>
///     If found on a HediffDef, mod hediffs will be preserved in brain or genome templates
/// </summary>
public class HediffTemplateProperties : DefModExtension
{
    /// <summary>
    ///     If true, when a pawn has this hediff, brain scanning cannot be done on this pawn.
    /// </summary>
    public readonly bool blockCreatingBrainTemplate = false;

    /// <summary>
    ///     If true, when a pawn has this hediff, genome sequencing cannot be done on this pawn.
    /// </summary>
    public readonly bool blockCreatingGenomeTemplate = false;

    /// <summary>
    ///     If true, the hediff will be record in brain template. Useful when copying mental hediffs.
    /// </summary>
    public readonly bool includeInBrainTemplate = false;

    /// <summary>
    ///     If true, the hediff will be record in genome template. Useful for organs and damages that need to bring forward to
    ///     the clone.
    /// </summary>
    public readonly bool includeInGenomeTemplate = false;

    /// <summary>
    ///     If true, the severity will also be recorded. No effect if the hediff itself will not include in any of templates.
    ///     Useful if hediff has real severity and severity matters.
    /// </summary>
    public readonly bool includeSeverityInTemplate = false;

    /// <summary>
    ///     If true, when a clone is generated, this pre-generated hediff is kept instead of being removed.
    ///     Useful when this organ will be added during <c>PawnGenerator.GeneratePawn</c>, but should not be brought forward
    ///     via
    ///     genome template.
    /// </summary>
    public readonly bool pregeneratedInCloning = false;
}