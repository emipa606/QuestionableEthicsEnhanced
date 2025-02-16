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
        if (hediffDef.HasModExtension<HediffTemplateProperties>())
        {
            var hediffProp = hediffDef.GetModExtension<HediffTemplateProperties>();
            return hediffProp.blockCreatingBrainTemplate;
        }
        if (excludedHediffs.Any(def => string.Equals(def.defName, hediffDef.defName)))
        {
            return true;
        }
        return false;
    }

    public static bool IsBlockingGenomeTemplateCreation(HediffDef hediffDef)
    {
        if (hediffDef.HasModExtension<HediffTemplateProperties>())
        {
            var hediffProp = hediffDef.GetModExtension<HediffTemplateProperties>();
            return hediffProp.blockCreatingGenomeTemplate;
        }
        if (excludedHediffs.Any(def => string.Equals(def.defName, hediffDef.defName)))
        {
            return true;
        }
        return false;
    }

    public static bool ShouldIncludeInBrainTemplate(HediffDef hediffDef)
    {
        if (hediffDef.HasModExtension<HediffTemplateProperties>())
        {
            var hediffProp = hediffDef.GetModExtension<HediffTemplateProperties>();
            return hediffProp.includeInBrainTemplate;
        }
        return includedBrainTemplateHediffs.Any(def => string.Equals(def.defName, hediffDef.defName));
    }

    public static bool ShouldIncludeInGenomeTemplate(HediffDef hediffDef)
    {
        if (hediffDef.HasModExtension<HediffTemplateProperties>())
        {
            var hediffProp = hediffDef.GetModExtension<HediffTemplateProperties>();
            return hediffProp.includeInGenomeTemplate;
        }
        return includedGenomeTemplateHediffs.Any(def => string.Equals(def.defName, hediffDef.defName));
    }

    public static bool ShouldIncludeSeverityInTemplate(HediffDef hediffDef)
    {
        if (hediffDef.HasModExtension<HediffTemplateProperties>())
        {
            var hediffProp = hediffDef.GetModExtension<HediffTemplateProperties>();
            return hediffProp.includeSeverityInTemplate;
        }
        return false;
    }

    public static bool ShouldKeepHediffWhenCloning(HediffDef hediffDef)
    {
        if (hediffDef.HasModExtension<HediffTemplateProperties>())
        {
            var hediffProp = hediffDef.GetModExtension<HediffTemplateProperties>();
            return hediffProp.pregeneratedInCloning;
        }
        return false;
    }
}