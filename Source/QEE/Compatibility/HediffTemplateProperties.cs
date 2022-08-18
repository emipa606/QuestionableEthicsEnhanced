using Verse;

namespace QEthics;

/// <summary>
///     If found on a HediffDef, mod hediffs will be preserved in brain or genome templates
/// </summary>
public class HediffTemplateProperties : DefModExtension
{
    public bool includeInBrainTemplate = false;
    public bool includeInGenomeTemplate = false;
}