using System.Collections.Generic;
using Verse;

namespace QEthics;

/// <summary>
///     Stores general compatibility information for the Genome sequencer and Brain scanner.
/// </summary>
public static class GeneralCompatibility
{
    public static List<ThingDef> excludedRaces = [];
    public static List<HediffDef> excludedHediffs = [];
    public static List<HediffDef> includedGenomeTemplateHediffs = [];
    public static List<HediffDef> includedBrainTemplateHediffs = [];
}