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
                        pawn.genes.Endogenes.ForEach(gene => genomeSequence.endogenes.Add(gene.def));
                    }
                    if (pawn.genes.Xenogenes.Any())
                    {
                        genomeSequence.xenogenes = [];
                        pawn.genes.Xenogenes.ForEach(gene => genomeSequence.xenogenes.Add(gene.def));
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
        var oldGenerateWithXenogermHediffChance = genomeSequence.xenotype?.generateWithXenogermReplicatingHediffChance ?? 0; 
        if (genomeSequence.xenotype != null)
        {
            // clear anything that could create extra things
            genomeSequence.xenotype.doubleXenotypeChances = null;
            genomeSequence.xenotype.genes = [];
            genomeSequence.xenotype.generateWithXenogermReplicatingHediffChance = 0;
        }
        var customXenotypeGenesTrimmed = genomeSequence.customXenotype != null ? new CustomXenotype() : null;
        if (genomeSequence.customXenotype != null)
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
            );
        request.ForceBodyType = genomeSequence.bodyType;
        Pawn pawn = null;
        try
        {
            CloningInProgress = true;
            pawn = PawnGenerator.GeneratePawn(request);
        }
        finally
        {
            CloningInProgress = false;
        }

        if(genomeSequence.xenotype != null)
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
                pawn?.health.AddHediff(h.def, h.part);
            }
        }
        pawn?.health.hediffSet.DirtyCache();
        pawn?.health.CheckForStateChange(null, null);

        if (pawn?.genes is { } geneTracker)
        {
            // restore those being trimmed for pawn generation
            geneTracker.xenotype.doubleXenotypeChances = oldXenotypeDoubleChance;
            geneTracker.xenotype.genes = oldXenotypeGenes;
            geneTracker.xenotype.generateWithXenogermReplicatingHediffChance = oldGenerateWithXenogermHediffChance;
            // Handle gene first to ensure that the actual pawn style surpass gene-given style 
            //if (genomeSequence.xenotype != null)
            //{
            //    geneTracker.xenotype = genomeSequence.xenotype;
            //    geneTracker.hybrid = genomeSequence.hybrid;
            //    //geneTracker.CustomXenotype = genomeSequence.customXenotype;
            //    //geneTracker.xenotypeName = genomeSequence.xenotypeName;
            //    //geneTracker.iconDef = genomeSequence.xenotypeIcon;
            //}
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
            //if (genomeSequence.endogenes?.Any() == true)
            //{
            //    pawn.genes.Endogenes.Clear(); //clear generated pawn's endogenes, to avoid incorrect hair/skin colours
            //    foreach (var gene in genomeSequence.endogenes)
            //    {
            //        var geneDef = DefDatabase<GeneDef>.GetNamedSilentFail(gene.defName);
            //        if (geneDef == null)
            //        {
            //            continue;
            //        }

            //        geneTracker.AddGene(geneDef, false);
            //    }
            //}
            //if (genomeSequence.xenogenes?.Any() == true)
            //{
            //    pawn.genes.Xenogenes.Clear(); //clear generated pawn's default xenogenes (from their xenotype), to be replaced with the list from the genome sequence
            //    foreach (var gene in genomeSequence.xenogenes)
            //    {
            //        var geneDef = DefDatabase<GeneDef>.GetNamedSilentFail(gene.defName);
            //        if (geneDef == null)
            //        {
            //            continue;
            //        }

            //        geneTracker.AddGene(geneDef, true);
            //    }
            //}
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
               !GeneralCompatibility.excludedRaces.Contains(def);
    }

    public static bool IsValidGenomeSequencingTarget(this Pawn pawn)
    {
        return IsValidGenomeSequencingTargetDef(pawn.def) && !pawn.health.hediffSet.hediffs.Any(hediff =>
            GeneralCompatibility.excludedHediffs.Any(hediffDef => hediff.def == hediffDef));
    }

    public static void TryFixSequenceGenes(GenomeSequence genomeSequence)
    {
        if (genomeSequence == null) return;
#pragma warning disable CS0618 // disableobsolete field warning
        if (genomeSequence.genes == null) return;
        genomeSequence.xenogenes = [];
        genomeSequence.endogenes = [];
        // first stage: if we can find the source pawn in the world, use it as a reference
        // note that the xenotype cannot be fully recovered as the unfixed version had already wiped it out
        Pawn refPawn = null;
        foreach (var pawn in PawnsFinder.All_AliveOrDead)
        {
            if(pawn.Name == null) continue;
            //if (pawn.story == null) continue;
            QEEMod.TryLog($"Check if {pawn.Name?.ToStringFull ?? "(Unnamed)"} is {genomeSequence.sourceName}");
            if (string.Equals(genomeSequence.sourceName, pawn.Name?.ToStringFull))
            {
                refPawn = pawn;
                break;
            }
        }

        if (refPawn is not null)
        {
            if (refPawn.genes != null)
            {
                List<GeneDef> melaninGenes = [];
                foreach (var geneDefName in genomeSequence.genes)
                {
                    var geneDef = DefDatabase<GeneDef>.GetNamedSilentFail(geneDefName);
                    if (geneDef is not null)
                    {
                        if (geneDef.defName.StartsWith("Skin_Melanin")){
                            melaninGenes.Add(geneDef);
                            continue;
                        }
                        var maybeGene = refPawn.genes.GetGene(geneDef);
                        if (maybeGene is null)
                        {

                            // maybe it was xenogene but overwritten after sequencing genome.
                            // assume it as an xenogene
                            genomeSequence.xenogenes.Add(geneDef);
                        }
                        else if (refPawn.genes.IsXenogene(maybeGene))
                        {
                            genomeSequence.xenogenes.Add(maybeGene.def);
                        }
                        else
                        {
                            genomeSequence.endogenes.Add(maybeGene.def);
                        }
                    }
                }
                if (!genomeSequence.endogenes.Any(gene => gene.skinColorOverride != null || gene.skinColorBase != null) &&
                    !genomeSequence.xenogenes.Any(gene => gene.skinColorOverride != null || gene.skinColorBase != null)){
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
                    int minDistance = int.MaxValue;
                    foreach (var xenotypeDef in DefDatabase<XenotypeDef>.AllDefs)
                    {
                        var sequenceGenesToCheck = new HashSet<GeneDef>((xenotypeDef.inheritable ? genomeSequence.endogenes : genomeSequence.xenogenes));
                        var xenotypeGenes = new HashSet<GeneDef>(xenotypeDef.AllGenes);
                        sequenceGenesToCheck.SymmetricExceptWith(xenotypeGenes);
                        var distance = sequenceGenesToCheck.Count;
                        if (distance < minDistance)
                        {
                            maybeXenotype = xenotypeDef;
                            minDistance = distance;
                        }
                    }
                    // we accept the most similar xenotype with missing or extra genes less than a set value.
                    // such value is arbitrary.
                    if (maybeXenotype != null && minDistance < 5) 
                    {
                        genomeSequence.xenotype = maybeXenotype;
                    }
                }
            }
        }

        // the source pawn is gone. Simply assign every genes as endogene.
        foreach (var geneDefName in genomeSequence.genes)
        {
            var geneDef = DefDatabase<GeneDef>.GetNamedSilentFail(geneDefName);
            if (geneDef != null) {
                genomeSequence.endogenes.Add(geneDef);
            }
        }
#pragma warning restore CS0618
    }
}
