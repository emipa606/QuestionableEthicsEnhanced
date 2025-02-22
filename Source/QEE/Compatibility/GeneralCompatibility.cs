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

    public static bool IsRaceBlockingTemplateCreation(ThingDef thingDef)
    {
        return excludedRaces.Contains(thingDef);
    }

    public static bool IsBlockingBrainTemplateCreation(HediffDef hediffDef)
    {
        return !hediffDef.HasModExtension<HediffTemplateProperties>()
            ? excludedHediffs.Any(def => string.Equals(def.defName, hediffDef.defName))
            : hediffDef.GetModExtension<HediffTemplateProperties>().blockCreatingBrainTemplate;
    }

    public static bool IsBlockingGenomeTemplateCreation(HediffDef hediffDef)
    {
        return !hediffDef.HasModExtension<HediffTemplateProperties>()
            ? excludedHediffs.Any(def => string.Equals(def.defName, hediffDef.defName))
            : hediffDef.GetModExtension<HediffTemplateProperties>().blockCreatingGenomeTemplate;
    }

    public static bool ShouldIncludeInBrainTemplate(HediffDef hediffDef)
    {
        return !hediffDef.HasModExtension<HediffTemplateProperties>()
            ? includedBrainTemplateHediffs.Any(def => string.Equals(def.defName, hediffDef.defName))
            : hediffDef.GetModExtension<HediffTemplateProperties>().includeInBrainTemplate;
    }

    public static bool ShouldIncludeInGenomeTemplate(HediffDef hediffDef)
    {
        return !hediffDef.HasModExtension<HediffTemplateProperties>()
            ? includedGenomeTemplateHediffs.Any(def => string.Equals(def.defName, hediffDef.defName))
            : hediffDef.GetModExtension<HediffTemplateProperties>().includeInGenomeTemplate;
    }

    public static bool ShouldIncludeSeverityInTemplate(HediffDef hediffDef)
    {
        return hediffDef.HasModExtension<HediffTemplateProperties>() &&
               hediffDef.GetModExtension<HediffTemplateProperties>().includeSeverityInTemplate;
    }

    public static bool ShouldKeepHediffWhenCloning(HediffDef hediffDef)
    {
        return hediffDef.HasModExtension<HediffTemplateProperties>() &&
               hediffDef.GetModExtension<HediffTemplateProperties>().pregeneratedInCloning;
    }
}