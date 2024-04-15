using System.Collections.Generic;
using RimWorld;
using Verse;

namespace QEthics;

public static class GenomeUtility
{
    public static Thing MakeGenomeSequence(Pawn pawn, ThingDef genomeDef)
    {
        var genomeThing = ThingMaker.MakeThing(genomeDef);
        if (genomeThing is GenomeSequence genomeSequence)
        {
            //Standard.
            genomeSequence.sourceName = pawn?.Name?.ToStringFull;
            if (genomeSequence.sourceName == null)
            {
                if (pawn != null)
                {
                    genomeSequence.sourceName = pawn.def.LabelCap;
                }
            }

            genomeSequence.pawnKindDef = pawn?.kindDef;
            if (pawn != null)
            {
                genomeSequence.gender = pawn.gender;

                if (pawn.health?.hediffSet?.hediffs != null)
                {
                    var pawnHediffs = pawn.health.hediffSet.hediffs;
                    if (pawnHediffs.Count > 0)
                    {
                        foreach (var h in pawnHediffs)
                        {
                            if (!GeneralCompatibility.includedGenomeTemplateHediffs.Any(hediffDef =>
                                    h.def.defName == hediffDef.defName))
                            {
                                continue;
                            }

                            QEEMod.TryLog($"Hediff {h.def.defName} will be added to genome template");

                            genomeSequence.hediffInfos.Add(new HediffInfo(h));
                        }
                    }
                }

                //Humanoid only.
                var story = pawn.story;
                var style = pawn.style;
                if (story != null)
                {
                    genomeSequence.bodyType = story.bodyType;
                    genomeSequence.crownType = story.headType;
                    genomeSequence.hairColor = story.hairColor;
                    genomeSequence.skinMelanin = story.melanin;
                    genomeSequence.hair = story.hairDef;
                    genomeSequence.beard = style.beardDef;
                    genomeSequence.faceTattoo = style.FaceTattoo;
                    genomeSequence.bodyTattoo = style.BodyTattoo;
                    genomeSequence.favoriteColor = story.favoriteColor;

                    foreach (var trait in story.traits.allTraits)
                    {
                        genomeSequence.traits.Add(new ExposedTraitEntry(trait));
                    }
                }

                // Biotech
                if (pawn.genes != null)
                {
                    if (pawn.genes.GenesListForReading.Any())
                    {
                        genomeSequence.genes = [];
                        pawn.genes.GenesListForReading.ForEach(gene => genomeSequence.genes.Add(gene.def.defName));
                    }

                    pawn.genes.SetXenotypeDirect(genomeSequence.xenotype);
                }

                //Alien Races compatibility.
                if (CompatibilityTracker.AlienRacesActive)
                {
                    AlienRaceCompat.GetFieldsFromAlienComp(pawn, genomeSequence);
                }

                //Facial animation compatibility.
                if (CompatibilityTracker.FacialAnimationActive)
                {
                    FacialAnimationCompatibility.GetFieldsFromFacialAnimationComps(pawn, genomeSequence);
                }

                //Animal Genetics compatibility.
                if (CompatibilityTracker.AnimalGeneticsActive)
                {
                    AnimalGeneticsCompatibility.GetFieldsFromAnimalGeneticsComps(pawn, genomeSequence);
                }
            }
        }

        //Creates a history event.
        if (pawn?.health is { Dead: false })
        {
            Find.HistoryEventsManager.RecordEvent(new HistoryEvent(QEHistoryDefOf.GenomeSequenced));
        }

        return genomeThing;
    }

    public static Pawn MakePawnFromGenomeSequence(GenomeSequence genomeSequence, Thing creator)
    {
        //int adultAge = (int)genome.pawnKindDef.RaceProps.lifeStageAges.Last().minAge;

        QEEMod.TryLog("Generating pawn.");
        var request = new PawnGenerationRequest(
            genomeSequence.pawnKindDef,
            creator.Faction,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false,
            fixedGender: genomeSequence.gender,
            fixedBiologicalAge: 3,
            fixedChronologicalAge: 0,
            allowFood: false);
        var pawn = PawnGenerator.GeneratePawn(request);

        //No pregenerated equipment.
        pawn?.equipment?.DestroyAllEquipment();
        pawn?.apparel?.DestroyAll();
        pawn?.inventory?.DestroyAll();

        //No pregenerated hediffs.
        pawn?.health.hediffSet.Clear();

        //Add Hediff marking them as a clone.
        QEEMod.TryLog("Adding hediffs to generated pawn");
        pawn?.health.AddHediff(QEHediffDefOf.QE_CloneStatus);

        if (genomeSequence.hediffInfos is { Count: > 0 })
        {
            //add hediffs to pawn from defs in HediffInfo class
            foreach (var h in genomeSequence.hediffInfos)
            {
                pawn?.health.AddHediff(h.def, h.part);
            }
        }

        //Set everything else.
        if (pawn?.story is { } storyTracker)
        {
            QEEMod.TryLog("Setting Pawn_StoryTracker attributes for generated pawn.");
            storyTracker.bodyType = genomeSequence.bodyType;
            //sanity check to remove possibility of an Undefined crownType
            if (genomeSequence.crownType == null)
            {
                storyTracker.headType = DefDatabase<HeadTypeDef>.GetNamedSilentFail(pawn.gender == Gender.Female
                    ? "Female_AverageNormal"
                    : "Male_AverageNormal");
            }
            else
            {
                storyTracker.headType = genomeSequence.crownType;
            }

            storyTracker.hairColor = genomeSequence.hairColor;
            storyTracker.hairDef = genomeSequence.hair ?? storyTracker.hairDef;
            storyTracker.favoriteColor = genomeSequence.favoriteColor;
            storyTracker.melanin = genomeSequence.skinMelanin;

            storyTracker.traits.allTraits.Clear();
            QEEMod.TryLog("Setting traits for generated pawn");
            foreach (var trait in genomeSequence.traits)
            {
                //storyTracker.traits.GainTrait(new Trait(trait.def, trait.degree));
                storyTracker.traits.allTraits.Add(new Trait(trait.def, trait.degree));
                pawn.workSettings?.Notify_DisabledWorkTypesChanged();

                pawn.skills?.Notify_SkillDisablesChanged();

                if (!pawn.Dead && pawn.RaceProps.Humanlike)
                {
                    pawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
                }
            }

            QEEMod.TryLog("Setting backstory for generated pawn");
            //Give random vatgrown backstory.
            storyTracker.childhood = DefDatabase<BackstoryDef>.GetNamed("Backstory_ColonyVatgrown");
            storyTracker.adulthood = null;
        }

        if (pawn?.style is { } styleTracker)
        {
            styleTracker.beardDef = genomeSequence.beard;
            styleTracker.FaceTattoo = genomeSequence.faceTattoo;
            styleTracker.BodyTattoo = genomeSequence.bodyTattoo;
        }

        if (pawn?.genes is { } geneTracker)
        {
            if (genomeSequence.xenotype != null)
            {
                geneTracker.xenotype = genomeSequence.xenotype;
            }

            if (genomeSequence.genes?.Any() == true)
            {
                foreach (var gene in genomeSequence.genes)
                {
                    var geneDef = DefDatabase<GeneDef>.GetNamedSilentFail(gene);
                    if (geneDef == null)
                    {
                        continue;
                    }

                    geneTracker.AddGene(geneDef, geneDef.endogeneCategory != EndogeneCategory.None);
                }
            }
        }

        if (pawn?.skills is { } skillsTracker)
        {
            foreach (var skill in skillsTracker.skills)
            {
                skill.Level = 0;
                skill.passion = Passion.None;
                skill.Notify_SkillDisablesChanged();
            }


            var skillPassions = new List<SkillRecord>();
            var skillsPicked = 0;
            var iterations = 0;
            //Pick 4 random skills to give minor passions to.
            while (skillsPicked < 4 && iterations < 1000)
            {
                var randomSkill = skillsTracker.skills.RandomElement();
                if (!skillPassions.Contains(randomSkill))
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
                var randomSkill = skillsTracker.skills.RandomElement();
                if (!skillPassions.Contains(randomSkill))
                {
                    skillPassions.Add(randomSkill);
                    randomSkill.passion = Passion.Major;
                    skillsPicked++;
                }

                iterations++;
            }
        }

        if (pawn?.workSettings is { } workSettings)
        {
            workSettings.EnableAndInitialize();
        }

        //Alien Races compatibility.
        if (CompatibilityTracker.AlienRacesActive)
        {
            AlienRaceCompat.SetFieldsToAlienComp(pawn, genomeSequence);
        }

        //Facial animation compatibility.
        if (CompatibilityTracker.FacialAnimationActive)
        {
            FacialAnimationCompatibility.SetFieldsToFacialAnimationComps(pawn, genomeSequence);
        }

        //Facial animation compatibility.
        if (CompatibilityTracker.AnimalGeneticsActive)
        {
            AnimalGeneticsCompatibility.SetFieldsToAnimalGeneticsComps(pawn, genomeSequence);
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
        return IsValidGenomeSequencingTargetDef(pawn.def) && !pawn.health.hediffSet.hediffs.Any(hediff =>
            GeneralCompatibility.excludedHediffs.Any(hediffDef => hediff.def == hediffDef));
    }
}