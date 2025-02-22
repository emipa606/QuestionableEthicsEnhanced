using System.Collections.Generic;
using Verse;

namespace QEthics;

/// <summary>
///     If found on a ThingDef which is a pawn it is added to the exclusion list.
/// </summary>
public class RaceExclusionProperties : DefModExtension
{
    /// <summary>
    ///     Listed hediffs will block creation of brain template and genome template.
    ///     Note that listed hediffs will block all races from templating, but not limited to this race.
    ///     For subtle configurating which templating shoulb be banned,
    ///     use <see cref="HediffTemplateProperties.blockCreatingBrainTemplate" />, or
    ///     <see cref="HediffTemplateProperties.blockCreatingGenomeTemplate" />
    ///     instead.
    /// </summary>
    public readonly List<HediffDef> excludeTheseHediffs = [];

    /// <summary>
    ///     If true, pawns in this race are not eligible for brain scanning and genome sequencing.
    /// </summary>
    public readonly bool excludeThisRace = true;
}