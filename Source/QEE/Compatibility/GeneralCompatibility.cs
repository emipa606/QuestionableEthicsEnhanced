using System.Collections.Generic;
using Verse;

namespace QEthics;

/// <summary>
///     Stores general compatibility information for the Genome sequencer and Brain scanner.
/// </summary>
public static class GeneralCompatibility
{
    public static readonly List<ThingDef> excludedRaces = [];
    public static readonly List<HediffDef> excludedHediffs = [];
    public static readonly List<HediffDef> includedGenomeTemplateHediffs = [];
    public static readonly List<HediffDef> includedBrainTemplateHediffs = [];
}