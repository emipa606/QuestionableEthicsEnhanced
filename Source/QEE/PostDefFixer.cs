using System.Linq;
using Verse;

namespace QEthics;

[StaticConstructorOnStartup]
public static class PostDefFixer
{
    static PostDefFixer()
    {
        //Add recipes to valid Genome Sequencing targets.
        foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(def => def.category == ThingCategory.Pawn))
        {
            if (def.GetModExtension<RaceExclusionProperties>() is { } props)
            {
                if (props.excludeThisRace)
                {
                    GeneralCompatibility.excludedRaces.Add(def);
                    QEEMod.TryLog($"Adding {def.defName} race to be excluded from Genome Sequencing");
                }

                if (props.excludeTheseHediffs.Count > 0)
                {
                    GeneralCompatibility.excludedHediffs.AddRange(props.excludeTheseHediffs);
                    QEEMod.TryLog(
                        $"Adding {string.Join(",", props.excludeTheseHediffs.Select(hediffDef => hediffDef.defName))} hediffs to be excluded from Genome Sequencing");
                }
            }

            if (def.IsValidGenomeSequencingTargetDef())
            {
                if (def.recipes == null)
                {
                    def.recipes = [];
                }

                if (def.recipes.Count > 0)
                {
                    def.recipes.Insert(0, QERecipeDefOf.QE_GenomeSequencing);
                }
                else
                {
                    def.recipes.Add(QERecipeDefOf.QE_GenomeSequencing);
                }
            }

            if (!def.IsValidBrainScanningDef())
            {
                continue;
            }

            if (def.recipes == null)
            {
                def.recipes = [];
            }

            if (def.recipes.Count > 0)
            {
                def.recipes.Insert(0, QERecipeDefOf.QE_BrainScanning);
            }
            else
            {
                def.recipes.Add(QERecipeDefOf.QE_BrainScanning);
            }
        }

        foreach (var def in DefDatabase<HediffDef>.AllDefs)
        {
            if (def.GetModExtension<HediffTemplateProperties>() is not { } props)
            {
                continue;
            }

            if (props.includeInBrainTemplate)
            {
                QEEMod.TryLog($"{def.defName} added to list of Hediffs applied to brain templates");
                GeneralCompatibility.includedBrainTemplateHediffs.Add(def);
            }

            if (!props.includeInGenomeTemplate)
            {
                continue;
            }

            QEEMod.TryLog($"{def.defName} added to list of Hediffs applied to genome templates");
            GeneralCompatibility.includedGenomeTemplateHediffs.Add(def);
        }
    }
}