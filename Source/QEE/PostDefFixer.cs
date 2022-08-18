using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
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
                }

                if (props.excludeTheseHediffs.Count > 0)
                {
                    GeneralCompatibility.excludedHediffs.AddRange(props.excludeTheseHediffs);
                }
            }

            if (def.IsValidGenomeSequencingTargetDef())
            {
                if (def.recipes == null)
                {
                    def.recipes = new List<RecipeDef>();
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
                def.recipes = new List<RecipeDef>();
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

        //Inject our own backstories.
        foreach (var def in DefDatabase<BackstoryDef>.AllDefs)
        {
            var backstory = new Backstory
            {
                slot = def.slot,
                title = def.title,
                titleShort = def.titleShort,
                titleFemale = def.titleFemale,
                titleShortFemale = def.titleShortFemale,
                baseDesc = def.baseDesc
            };
            AccessTools.Field(typeof(Backstory), "bodyTypeFemale").SetValue(backstory, def.bodyTypeFemale);
            AccessTools.Field(typeof(Backstory), "bodyTypeMale").SetValue(backstory, def.bodyTypeMale);
            AccessTools.Field(typeof(Backstory), "bodyTypeGlobal").SetValue(backstory, def.bodyTypeGlobal);
            backstory.spawnCategories.AddRange(def.spawnCategories);
            backstory.PostLoad();
            backstory.ResolveReferences();

            BackstoryDatabase.AddBackstory(backstory);

            def.identifier = backstory.identifier;
            //QEEMod.TryLog("'" + def.defName + "' identifier is '" + backstory.identifier + "'");
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