using System.Collections.Generic;
using Verse;

namespace QEthics;

/// <summary>
///     If found on a ThingDef which is a pawn it is added to the exclusion list.
/// </summary>
public class RaceExclusionProperties : DefModExtension
{
    public List<HediffDef> excludeTheseHediffs = [];
    public bool excludeThisRace = true;
}