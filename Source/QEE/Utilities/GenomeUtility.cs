using System.Collections.Generic;
using RimWorld;
using Verse;

namespace QEthics;

public static class GenomeUtility
{
    public static bool CloningInProgress { get; private set; }

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
                genomeSequence.spawnShambler = pawn.IsCreepJoiner;


                if (pawn.health?.hediffSet?.hediffs != null)
                {
                    var pawnHediffs = pawn.health.hediffSet.hediffs;
                    if (pawnHediffs.Count > 0)
                    {
                        foreach (var h in pawnHediffs)
                        {
                            if (!GeneralCompatibility.ShouldIncludeInGenomeTemplate(h.def))
                            {
                                continue;
                            }

                            QEEMod.TryLog($"Hediff {h.def.defName} will be added to genome template");

                            genomeSequence.hediffInfos.Add(new HediffInfo(h,
                                GeneralCompatibility.ShouldIncludeSeverityInTemplate(h.def)));
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
                    genomeSequence.skinColorBase = story.skinColorBase;
                    genomeSequence.skinColorOverride = story.skinColorOverride;
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
                    if (pawn.genes.Endogenes.Any()) //record directly from the pawn's Endogenes and Xenogenes list,
                        //so we don't have to figure it out later (and potentially mess it up)
                    {
                        genomeSequence.endogenes = [];
                        genomeSequence.activeRandomlyChosenEndogenes = [];
                        pawn.genes.Endogenes.ForEach(gene =>
                        {
                            genomeSequence.endogenes.Add(gene.def);
                            if (gene.def.RandomChosen && !gene.Overridden)
                            {
                                genomeSequence.activeRandomlyChosenEndogenes.Add(gene.def);
                            }
                        });
                    }

                    if (pawn.genes.Xenogenes.Any())
                    {
                        genomeSequence.xenogenes = [];
                        genomeSequence.activeRandomlyChosenXenogenes = [];
                        pawn.genes.Xenogenes.ForEach(gene =>
                        {
                            genomeSequence.xenogenes.Add(gene.def);
                            if (gene.def.RandomChosen && !gene.Overridden)
                            {
                                genomeSequence.activeRandomlyChosenXenogenes.Add(gene.def);
                            }
                        });
                    }

                    genomeSequence.xenotype = pawn.genes.xenotype; //this was previously set to (very, very wrongly)
                    //change the pawn's xenotype into that of the empty
                    //genomeSequence's, reverting non-baseliner pawns
                    //into baseliners.
                    genomeSequence.hybrid = pawn.genes.hybrid;
                    genomeSequence.customXenotype = pawn.genes.CustomXenotype;
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
        var minimumAge = genomeSequence.pawnKindDef.RaceProps.lifeStageAges[0].minAge;
        if (genomeSequence.pawnKindDef.RaceProps.lifeStageAges.Count > 1)
        {
            foreach (var racePropsLifeStageAge in
                     genomeSequence.pawnKindDef.RaceProps.lifeStageAges)
            {
                if (racePropsLifeStageAge.def.alwaysDowned)
                {
                    continue;
                }

                minimumAge = racePropsLifeStageAge.minAge;
                break;
            }
        }

        var oldXenotypeDoubleChance = genomeSequence.xenotype?.doubleXenotypeChances;
        var oldXenotypeGenes = genomeSequence.xenotype?.genes;
        var oldGenerateWithXenogermHediffChance =
            genomeSequence.xenotype?.generateWithXenogermReplicatingHediffChance ?? 0;
        if (genomeSequence.xenotype != null)
        {
            // clear anything that could create extra things
            genomeSequence.xenotype.doubleXenotypeChances = null;
            genomeSequence.xenotype.genes = [];
            genomeSequence.xenotype.generateWithXenogermReplicatingHediffChance = 0;
        }

        var customXenotypeGenesTrimmed = genomeSequence.customXenotype != null ? new CustomXenotype() : null;
        if (genomeSequence.customXenotype != null && customXenotypeGenesTrimmed != null)
        {
            // copy a custom xenotype without actual genes
            customXenotypeGenesTrimmed.iconDef = genomeSequence.customXenotype.iconDef;
            customXenotypeGenesTrimmed.name = genomeSequence.customXenotype.name;
            customXenotypeGenesTrimmed.inheritable = genomeSequence.customXenotype.inheritable;
        }

        var xenogeneToGo = genomeSequence.xenogenes?.ListFullCopy();
        xenogeneToGo?.RemoveWhere(x => DefDatabase<GeneDef>.GetNamedSilentFail(x.defName) == null);
        var endogeneToGo = genomeSequence.endogenes?.ListFullCopy();
        endogeneToGo?.RemoveWhere(x => DefDatabase<GeneDef>.GetNamedSilentFail(x.defName) == null);

        var request = new PawnGenerationRequest(
            genomeSequence.pawnKindDef,
            creator.Faction,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false,
            fixedGender: genomeSequence.gender,
            fixedBiologicalAge: minimumAge,
            fixedChronologicalAge: 0,
            allowFood: false,
            forcedXenotype: genomeSequence.xenotype,
            forcedCustomXenotype: customXenotypeGenesTrimmed,
            forcedXenogenes: xenogeneToGo,
            forcedEndogenes: endogeneToGo,
            forceNoGear: true
        )
        {
            ForceBodyType = genomeSequence.bodyType
        };
        Pawn pawn;
        try
        {
            CloningInProgress = true;
            pawn = PawnGenerator.GeneratePawn(request);
        }
        finally
        {
            CloningInProgress = false;
        }

        if (genomeSequence.xenotype != null)
        {
            genomeSequence.xenotype.genes = oldXenotypeGenes;
            genomeSequence.xenotype.doubleXenotypeChances = oldXenotypeDoubleChance;
            genomeSequence.xenotype.generateWithXenogermReplicatingHediffChance = oldGenerateWithXenogermHediffChance;
        }

        //No pregenerated equipment.
        pawn?.equipment?.DestroyAllEquipment();
        pawn?.apparel?.DestroyAll();
        pawn?.inventory?.DestroyAll();

        //No pregenerated hediffs.
        foreach (var hediff in pawn?.health.hediffSet.hediffs.ListFullCopy() ?? [])
        {
            if (GeneralCompatibility.ShouldKeepHediffWhenCloning(hediff.def))
            {
                continue;
            }

            QEEMod.TryLog($"removing pregenerated hediff {hediff?.Label} at {hediff?.Part?.Label}");
            pawn?.health.RemoveHediff(hediff);
        }

        //Add Hediff marking them as a clone.
        QEEMod.TryLog("Adding hediffs to generated pawn");
        pawn?.health.AddHediff(QEHediffDefOf.QE_CloneStatus);

        if (genomeSequence.hediffInfos is { Count: > 0 })
        {
            //add hediffs to pawn from defs in HediffInfo class
            foreach (var h in genomeSequence.hediffInfos)
            {
                var addedHediff = pawn?.health.AddHediff(h.def, h.part);
                if (addedHediff != null && h.severity.HasValue)
                {
                    addedHediff.Severity = h.severity.Value;
                }
            }
        }

        pawn?.health.hediffSet.DirtyCache();
        pawn?.health.CheckForStateChange(null, null);

        // If biotech is not installed, nothing inside this will be made actions.
        if (ModLister.BiotechInstalled && pawn?.genes is { } geneTracker)
        {
            // restore those being trimmed for pawn generation
            if (geneTracker.xenotype != null)
            {
                geneTracker.xenotype.doubleXenotypeChances = oldXenotypeDoubleChance;
                geneTracker.xenotype.genes = oldXenotypeGenes;
                geneTracker.xenotype.generateWithXenogermReplicatingHediffChance = oldGenerateWithXenogermHediffChance;
            }

            if (genomeSequence.activeRandomlyChosenEndogenes?.Any() ?? false)
            {
                foreach (var activeGeneDef in genomeSequence.activeRandomlyChosenEndogenes)
                {
                    foreach (var gene in geneTracker.Endogenes)
                    {
                        if (gene.def.defName != activeGeneDef.defName)
                        {
                            continue;
                        }

                        geneTracker.OverrideAllConflicting(gene);
                        break;
                    }
                }
            }

            if (genomeSequence.activeRandomlyChosenXenogenes?.Any() ?? false)
            {
                foreach (var activeGeneDef in genomeSequence.activeRandomlyChosenXenogenes)
                {
                    foreach (var gene in geneTracker.Xenogenes)
                    {
                        if (gene.def.defName != activeGeneDef.defName)
                        {
                            continue;
                        }

                        geneTracker.OverrideAllConflicting(gene);
                        break;
                    }
                }
            }
            //the logic previously used in this block was both flawed and wrong.
            //  geneTracker.AddGene(geneDef, geneDef.endogeneCategory != EndogeneCategory.None);
            //this checks what the gene's EndogeneCategory is, then if it ISN'T 0 (i.e. no category),
            //then it flags it as a xenogene. this results in hair colours being flagged as xenogenes,
            //while things like Deathless are added as endogenes.
            //however, even if we fix this error by changing the != to a ==, the logic is still fundamentally flawed.
            //whether a gene is an endogene or a xenogene isn't determined like that in the actual game.
            //"Strong Melee Damage" has an EndogeneCategory of 0, so it's treated as a xenogene.
            //however, Yttakin have that as an endogene. if you clone a Yttakin using this logic, it will result
            //in many of the Yttakin's natural features being added as non-hereditary xenogenes.
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
            storyTracker.skinColorBase = genomeSequence.skinColorBase;
            storyTracker.skinColorOverride = genomeSequence.skinColorOverride;

            // properly remove all pregenerated traits
            storyTracker.traits.allTraits.ListFullCopy().ForEach(x => storyTracker.traits.RemoveTrait(x));
            QEEMod.TryLog("Setting traits for generated pawn");
            foreach (var trait in genomeSequence.traits)
            {
                storyTracker.traits.GainTrait(new Trait(trait.def, trait.degree));
                //storyTracker.traits.allTraits.Add(new Trait(trait.def, trait.degree));
                //pawn.workSettings?.Notify_DisabledWorkTypesChanged();

                //pawn.skills?.Notify_SkillDisablesChanged();

                //if (!pawn.Dead && pawn.RaceProps.Humanlike)
                //{
                //    pawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
                //}
            }

            storyTracker.traits.RecalculateSuppression();

            QEEMod.TryLog("Setting backstory for generated pawn");
            //Give random vatgrown backstory.
            storyTracker.childhood = DefDatabase<BackstoryDef>.GetNamed("Backstory_ColonyVatgrown");
            storyTracker.adulthood = null;
        }

        if (pawn?.style is { } styleTracker)
        {
            styleTracker.beardDef = genomeSequence.beard;
            if (ModLister.IdeologyInstalled)
            {
                // need to check if Ideology is installed, or error is thrown
                styleTracker.FaceTattoo = genomeSequence.faceTattoo;
                styleTracker.BodyTattoo = genomeSequence.bodyTattoo;
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

            skillsTracker.Notify_SkillDisablesChanged();
        }

        pawn?.Notify_DisabledWorkTypesChanged();
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

        pawn?.Drawer.renderer.SetAllGraphicsDirty();
        return pawn;
    }

    public static bool IsValidGenomeSequencingTargetDef(this ThingDef def)
    {
        return !def.race.IsMechanoid &&
               def.GetStatValueAbstract(StatDefOf.MeatAmount) > 0f &&
               //def.GetStatValueAbstract(StatDefOf.LeatherAmount) > 0f &&
               !GeneralCompatibility.IsRaceBlockingTemplateCreation(def);
    }

    public static bool IsValidGenomeSequencingTarget(this Pawn pawn)
    {
        return IsValidGenomeSequencingTargetDef(pawn.def) && !pawn.health.hediffSet.hediffs.Any(hediff =>
            GeneralCompatibility.IsBlockingGenomeTemplateCreation(hediff.def));
    }

    public static bool IsValidGenomeSequencingTarget(Pawn pawn, ref string failReason)
    {
        // Currently only used in Operation tab. Short reason is sufficient.
        if (!IsValidGenomeSequencingTargetDef(pawn.def))
        {
            failReason = "QE_TemplatingRejectExcludedRaceShort".Translate();
            return false;
        }

        if (!pawn.health.hediffSet.hediffs.Any(hediff =>
                GeneralCompatibility.IsBlockingGenomeTemplateCreation(hediff.def)))
        {
            return true;
        }

        failReason = "QE_GenomeSequencerRejectExcludedHediffShort".Translate();
        return false;
    }

    public static void TryFixSequenceGenes(GenomeSequence genomeSequence)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        if (genomeSequence?.genes == null)
        {
            return;
        }

        genomeSequence.xenogenes = [];
        genomeSequence.endogenes = [];
        genomeSequence.activeRandomlyChosenXenogenes = [];
        genomeSequence.activeRandomlyChosenEndogenes = [];
        // first stage: if we can find the source pawn in the world, use it as a reference
        // note that the xenotype cannot be fully recovered as the unfixed version had already wiped it out
        Pawn refPawn = null;
        foreach (var pawn in PawnsFinder.All_AliveOrDead)
        {
            if (pawn.Name == null)
            {
                continue;
            }

            //if (pawn.story == null) continue;
            QEEMod.TryLog($"Check if {pawn.Name?.ToStringFull ?? "(Unnamed)"} is {genomeSequence.sourceName}");
            if (!string.Equals(genomeSequence.sourceName, pawn.Name?.ToStringFull))
            {
                continue;
            }

            refPawn = pawn;
            break;
        }

        if (refPawn?.genes != null)
        {
            List<GeneDef> melaninGenes = [];
            foreach (var geneDefName in genomeSequence.genes)
            {
                var geneDef = DefDatabase<GeneDef>.GetNamedSilentFail(geneDefName);
                if (geneDef is null)
                {
                    continue;
                }

                if (geneDef.defName.StartsWith("Skin_Melanin"))
                {
                    melaninGenes.Add(geneDef);
                    continue;
                }

                var maybeGene = refPawn.genes.GetGene(geneDef);
                if (maybeGene is null)
                {
                    // maybe it was xenogene but overwritten after sequencing genome.
                    // assume it as a xenogene
                    genomeSequence.xenogenes.Add(geneDef);
                }
                else if (refPawn.genes.IsXenogene(maybeGene))
                {
                    genomeSequence.xenogenes.Add(maybeGene.def);
                    if (maybeGene.def.RandomChosen && !maybeGene.Overridden)
                    {
                        genomeSequence.activeRandomlyChosenXenogenes.Add(maybeGene.def);
                    }
                }
                else
                {
                    genomeSequence.endogenes.Add(maybeGene.def);
                    if (maybeGene.def.RandomChosen && !maybeGene.Overridden)
                    {
                        genomeSequence.activeRandomlyChosenEndogenes.Add(maybeGene.def);
                    }
                }
            }

            if (!genomeSequence.endogenes.Any(gene =>
                    gene.skinColorOverride != null || gene.skinColorBase != null) &&
                !genomeSequence.xenogenes.Any(gene => gene.skinColorOverride != null || gene.skinColorBase != null))
            {
                if (melaninGenes.Count > 0)
                {
                    genomeSequence.endogenes.Add(melaninGenes[0]);
                }
            }


            if (refPawn.genes.xenotype != XenotypeDefOf.Baseliner || refPawn.genes.CustomXenotype != null)
            {
                // somehow player has fixed the xenotype on their own. Use the actual value
                genomeSequence.xenotype = refPawn.genes.xenotype;
                genomeSequence.customXenotype = refPawn.genes.CustomXenotype;
            }
            else
            {
                // guess which xenotype the pawn should be
                XenotypeDef maybeXenotype = null;
                var minDistance = int.MaxValue;
                foreach (var xenotypeDef in DefDatabase<XenotypeDef>.AllDefs)
                {
                    var sequenceGenesToCheck = new HashSet<GeneDef>(xenotypeDef.inheritable
                        ? genomeSequence.endogenes
                        : genomeSequence.xenogenes);
                    var xenotypeGenes = new HashSet<GeneDef>(xenotypeDef.AllGenes);
                    sequenceGenesToCheck.SymmetricExceptWith(xenotypeGenes);
                    var distance = sequenceGenesToCheck.Count;
                    if (distance >= minDistance)
                    {
                        continue;
                    }

                    maybeXenotype = xenotypeDef;
                    minDistance = distance;
                }

                // we accept the most similar xenotype with missing or extra genes less than a set value.
                // such value is arbitrary.
                if (maybeXenotype != null && minDistance < 5)
                {
                    genomeSequence.xenotype = maybeXenotype;
                }
            }
        }

        // the source pawn is gone. Simply assign every genes as endogene.
        foreach (var geneDefName in genomeSequence.genes)
        {
            var geneDef = DefDatabase<GeneDef>.GetNamedSilentFail(geneDefName);
            if (geneDef != null)
            {
                genomeSequence.endogenes.Add(geneDef);
            }
        }
#pragma warning restore CS0618
    }
}