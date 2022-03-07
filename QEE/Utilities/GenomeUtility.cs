using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace QEthics
{
    public static class GenomeUtility
    {
        public static Thing MakeGenomeSequence(Pawn pawn, ThingDef genomeDef)
        {
            Thing genomeThing = ThingMaker.MakeThing(genomeDef);
            GenomeSequence genomeSequence = genomeThing as GenomeSequence;
            if(genomeSequence != null)
            {
                //Standard.
                genomeSequence.sourceName = pawn?.Name?.ToStringFull ?? null;
                if(genomeSequence.sourceName == null)
                {
                    genomeSequence.sourceName = pawn.def.LabelCap;
                }
                genomeSequence.pawnKindDef = pawn.kindDef;
                genomeSequence.gender = pawn.gender;

                if (pawn?.health?.hediffSet?.hediffs != null)
                {
                    List<Hediff> pawnHediffs = pawn.health.hediffSet.hediffs;
                    if (pawnHediffs.Count > 0)
                    {
                        foreach(Hediff h in pawnHediffs)
                        {
                            if (GeneralCompatibility.includedGenomeTemplateHediffs.Any(hediffDef => h.def.defName == hediffDef.defName) )
                            {
                                QEEMod.TryLog("Hediff " + h.def.defName + " will be added to genome template");

                                genomeSequence.hediffInfos.Add(new HediffInfo(h));
                            }
                        }
                    }
                }

                //Humanoid only.
                Pawn_StoryTracker story = pawn.story;
                if(story != null)
                {
                    genomeSequence.bodyType = story.bodyType;
                    genomeSequence.crownType = story.crownType;
                    genomeSequence.hairColor = story.hairColor;
                    genomeSequence.skinMelanin = story.melanin;
                    genomeSequence.hair = story.hairDef;
                    genomeSequence.headGraphicPath = story.HeadGraphicPath;

                    foreach (Trait trait in story.traits.allTraits)
                    {
                        genomeSequence.traits.Add(new ExposedTraitEntry(trait));
                    }
                }

                //Alien Races compatibility.
                if(CompatibilityTracker.AlienRacesActive)
                {
                    AlienRaceCompat.GetFieldsFromAlienComp(pawn, genomeSequence);
                }
            }
            //Creates a history event.
            if (!pawn.health.Dead)
            {
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(QEHistoryDefOf.GenomeSequenced));
            }
                return genomeThing;
        }

        public static Pawn MakePawnFromGenomeSequence(GenomeSequence genomeSequence, Thing creator)
        {
            //int adultAge = (int)genome.pawnKindDef.RaceProps.lifeStageAges.Last().minAge;

            QEEMod.TryLog("Generating pawn...");
            PawnGenerationRequest request = new PawnGenerationRequest(
                genomeSequence.pawnKindDef,
                faction: creator.Faction,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                fixedGender: genomeSequence.gender,
                fixedBiologicalAge: 0,
                fixedChronologicalAge: 0,
                allowFood: false);
            Pawn pawn = PawnGenerator.GeneratePawn(request);

            //No pregenerated equipment.
            pawn?.equipment?.DestroyAllEquipment();
            pawn?.apparel?.DestroyAll();
            pawn?.inventory?.DestroyAll();

            //No pregenerated hediffs.
            pawn.health.hediffSet.Clear();

            //Add Hediff marking them as a clone.
            QEEMod.TryLog("Adding hediffs to generated pawn");
            pawn.health.AddHediff(QEHediffDefOf.QE_CloneStatus);

            if (genomeSequence.hediffInfos != null && genomeSequence.hediffInfos.Count > 0)
            {
                //add hediffs to pawn from defs in HediffInfo class
                foreach(HediffInfo h in genomeSequence.hediffInfos)
                {
                    pawn.health.AddHediff(h.def, h.part);
                }
            }

            //Set everything else.
            if (pawn.story is Pawn_StoryTracker storyTracker)
            {
                QEEMod.TryLog("Setting Pawn_StoryTracker attributes for generated pawn...");
                storyTracker.bodyType = genomeSequence.bodyType;
                //sanity check to remove possibility of an Undefined crownType
                if (genomeSequence.crownType == CrownType.Undefined)
                {
                    storyTracker.crownType = CrownType.Average;
                }
                else
                {
                    storyTracker.crownType = genomeSequence.crownType;
                }

                storyTracker.hairColor = genomeSequence.hairColor;
                storyTracker.hairDef = genomeSequence.hair ?? storyTracker.hairDef;
                storyTracker.melanin = genomeSequence.skinMelanin;

                //headGraphicPath is private, so we need Harmony to set its value
                if (genomeSequence.headGraphicPath != null)
                {
                    QEEMod.TryLog("Setting headGraphicPath for generated pawn");
                    AccessTools.Field(typeof(Pawn_StoryTracker), "headGraphicPath").SetValue(storyTracker, genomeSequence.headGraphicPath);
                }
                else
                {
                    //could use this code to make a random head, instead of the static graphic paths.
                    //AccessTools.Field(typeof(Pawn_StoryTracker), "headGraphicPath").SetValue(storyTracker,
                    //GraphicDatabaseHeadRecords.GetHeadRandom(genomeSequence.gender, PawnSkinColors.GetSkinColor(genomeSequence.skinMelanin), genomeSequence.crownType).GraphicPath);
                    QEEMod.TryLog("No headGraphicPath in genome template, setting to default head");
                    string path = genomeSequence.gender == Gender.Male ? "Things/Pawn/Humanlike/Heads/Male/Male_Average_Normal" :
                            "Things/Pawn/Humanlike/Heads/Female/Female_Narrow_Normal";
                    AccessTools.Field(typeof(Pawn_StoryTracker), "headGraphicPath").SetValue(storyTracker, path);
                }

                storyTracker.traits.allTraits.Clear();
                QEEMod.TryLog("Setting traits for generated pawn");
                foreach (ExposedTraitEntry trait in genomeSequence.traits)
                {
                    //storyTracker.traits.GainTrait(new Trait(trait.def, trait.degree));
                    storyTracker.traits.allTraits.Add(new Trait(trait.def, trait.degree));
                    if (pawn.workSettings != null)
                    {
                        pawn.workSettings.Notify_DisabledWorkTypesChanged();
                    }
                    if (pawn.skills != null)
                    {
                        pawn.skills.Notify_SkillDisablesChanged();
                    }
                    if (!pawn.Dead && pawn.RaceProps.Humanlike)
                    {
                        pawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
                    }
                }

                QEEMod.TryLog("Setting backstory for generated pawn");
                //Give random vatgrown backstory.
                storyTracker.childhood = DefDatabase<BackstoryDef>.GetNamed("Backstory_ColonyVatgrown").GetFromDatabase();
                storyTracker.adulthood = null;
            }

            if(pawn.skills is Pawn_SkillTracker skillsTracker)
            {
                foreach (SkillRecord skill in skillsTracker.skills)
                {
                    skill.Level = 0;
                    skill.passion = Passion.None;
                    skill.Notify_SkillDisablesChanged();
                }

                
                List<SkillRecord> skillPassions = new List<SkillRecord>();
                int skillsPicked = 0;
                int iterations = 0;
                //Pick 4 random skills to give minor passions to.
                while (skillsPicked < 4 && iterations < 1000)
                {
                    SkillRecord randomSkill = skillsTracker.skills.RandomElement();
                    if(!skillPassions.Contains(randomSkill))
                    {
                        skillPassions.Add(randomSkill);
                        randomSkill.passion = Passion.Minor;
                        skillsPicked++;
                    }

                    iterations++;
                }

                skillsPicked = 0;
                iterations = 0;
                //Pick 2 random skills to give major passions to.
                while (skillsPicked < 2 && iterations < 1000)
                {
                    SkillRecord randomSkill = skillsTracker.skills.RandomElement();
                    if (!skillPassions.Contains(randomSkill))
                    {
                        skillPassions.Add(randomSkill);
                        randomSkill.passion = Passion.Major;
                        skillsPicked++;
                    }

                    iterations++;
                }
            }

            if(pawn.workSettings is Pawn_WorkSettings workSettings)
            {
                workSettings.EnableAndInitialize();
            }

            //Alien Races compatibility.
            if (CompatibilityTracker.AlienRacesActive)
            {
                AlienRaceCompat.SetFieldsToAlienComp(pawn, genomeSequence);
            }

            PortraitsCache.SetDirty(pawn);
            PortraitsCache.PortraitsCacheUpdate();

            return pawn;
        }

        public static bool IsValidGenomeSequencingTargetDef(this ThingDef def)
        {
            return !def.race.IsMechanoid &&
                def.GetStatValueAbstract(StatDefOf.MeatAmount) > 0f &&
                //def.GetStatValueAbstract(StatDefOf.LeatherAmount) > 0f &&
                !GeneralCompatibility.excludedRaces.Contains(def);
        }

        public static bool IsValidGenomeSequencingTarget(this Pawn pawn)
        {
            return IsValidGenomeSequencingTargetDef(pawn.def) && !pawn.health.hediffSet.hediffs.Any(hediff => GeneralCompatibility.excludedHediffs.Any(hediffDef => hediff.def == hediffDef));
        }
    }
}
