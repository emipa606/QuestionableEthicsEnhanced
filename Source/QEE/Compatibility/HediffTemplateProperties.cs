using Verse;

namespace QEthics;

/// <summary>
///     If found on a HediffDef, mod hediffs will be preserved in brain or genome templates
/// </summary>
public class HediffTemplateProperties : DefModExtension
{
    public readonly bool includeInBrainTemplate = false;
    public readonly bool includeInGenomeTemplate = false;
}