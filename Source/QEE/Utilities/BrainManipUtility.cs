using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace QEthics;

public static class BrainManipUtility
{
    public static bool IsValidBrainScanningDef(this ThingDef def)
    {
        return !def.race.IsMechanoid && !GeneralCompatibility.IsRaceBlockingTemplateCreation(def);
    }

    public static bool IsValidBrainScanningTarget(this Pawn pawn)
    {
        var def = pawn.def;
        return IsValidBrainScanningDef(def) && !pawn.Dead && !pawn.health.hediffSet.hediffs.Any(hediff =>
            GeneralCompatibility.IsBlockingBrainTemplateCreation(hediff.def));
    }

    public static bool IsValidBrainScanningTarget(Pawn targetPawn, ref string failReason, bool inOperationTab = false)
    {
        var def = targetPawn.def;

        if (!IsValidBrainScanningDef(def))
        {
            if (inOperationTab)
            {
                failReason = "QE_TemplatingRejectExcludedRaceShort".Translate();
                return false;
            }
            failReason = "QE_BrainScanningRejectExcludedRace".Translate(targetPawn.kindDef.race);
            return false;
        }

        if (targetPawn.Dead)
        {
            // Corpse is not allowed to take operation. no need to check inOperationTab
            failReason = "QE_BrainScanningRejectDead".Translate(targetPawn.Named("PAWN"));
            return false;
        }
        //fail if pawn has an excluded hediff

        if (!targetPawn.health.hediffSet.hediffs.Any(hediff =>
                GeneralCompatibility.IsBlockingBrainTemplateCreation(hediff.def)))
        {
            return true;
        }

        if (inOperationTab)
        {
            failReason = "QE_BrainScanningRejectExcludedHediffShort".Translate();
        }
        else
        {
            failReason = "QE_BrainScanningRejectExcludedHediff".Translate(targetPawn.Named("PAWN"));
        }
        return false;
    }

    public static bool IsValidBrainTemplatingTarget(this Pawn targetPawn, ref string failReason,
        BrainScanTemplate template)
    {
        if (!IsValidBrainScanningTarget(targetPawn, ref failReason))
        {
            return false;
        }

        if (template.isAnimal)
        {
            //fail if trying to apply animal template to non-animal
            if (!targetPawn.RaceProps.Animal)
            {
                failReason = "QE_BrainScanningRejectNotAnimal".Translate(targetPawn.LabelShort);
                return false;
            }
            //fail if trying to apply template with different PawnKindDefs. Only do this for animals, because cloned humanlikes have different KindDefs

            if (template.kindDef?.race != null && template.kindDef.defName != targetPawn.kindDef.defName)
            {
                failReason =
                    "QE_BrainScanningRejectWrongKind".Translate(targetPawn.LabelShort, template.kindDef.race.LabelCap);
                return false;
            }
        }

        //fail if trying to apply non-animal template to animal pawn
        else if (targetPawn.RaceProps.Animal)
        {
            failReason = "QE_BrainScanningRejectNotHumanlike".Translate(targetPawn.LabelShort);
            return false;
        }

        //fail if pawn doesn't have the Clone hediff
        if (QEESettings.instance.brainTemplatingRequiresClone &&
            !targetPawn.health.hediffSet.HasHediff(QEHediffDefOf.QE_CloneStatus))
        {
            failReason = "QE_BrainScanningRejectNotClone".Translate(targetPawn.Named("PAWN"));
            return false;
        }

        //fail if they have been templated in the past
        if (!targetPawn.health.hediffSet.HasHediff(QEHediffDefOf.QE_BrainTemplated))
        {
            return true;
        }

        failReason = "QE_BrainScanningRejectAlreadyTemplated".Translate(targetPawn.Named("PAWN"));
        return false;
    }

    public static Thing MakeBrainScan(Pawn pawn, ThingDef genomeDef)
    {
        var brainScanThing = ThingMaker.MakeThing(genomeDef);
        if (brainScanThing is not BrainScanTemplate brainScan)
        {
            return brainScanThing;
        }

        //ideology stuff
        if (pawn.Ideo != null)
        {
            brainScan.scannedIdeology = pawn.Ideo;
        }

        //Standard.
        brainScan.sourceName = pawn.Name?.ToStringFull;
        brainScan.kindDef = pawn.kindDef;

        //Backgrounds
        var story = pawn.story;
        if (story != null)
        {
            brainScan.backStoryChild = story.childhood;
            brainScan.backStoryAdult = story.adulthood;
        }

        //Skills
        var skillTracker = pawn.skills;
        if (skillTracker != null)
        {
            foreach (var skill in skillTracker.skills)
            {
                brainScan.skills.Add(new ComparableSkillRecord
                {
                    def = skill.def,
                    level = skill.Level,
                    passion = skill.passion
                });
            }
        }

        //Animal
        brainScan.isAnimal = pawn.RaceProps.Animal;

        //Training
        var trainingTracker = pawn.training;
        if (trainingTracker != null)
        {
            var learned = (DefMap<TrainableDef, bool>)AccessTools.Field(typeof(Pawn_TrainingTracker), "learned")
                .GetValue(trainingTracker);
            var steps = (DefMap<TrainableDef, int>)AccessTools.Field(typeof(Pawn_TrainingTracker), "steps")
                .GetValue(trainingTracker);

            //Copy
            foreach (var item in learned)
            {
                brainScan.trainingLearned[item.Key] = item.Value;
            }

            foreach (var item in steps)
            {
                brainScan.trainingSteps[item.Key] = item.Value;
            }
        }

        //Hediffs
        if (pawn.health?.hediffSet?.hediffs == null)
        {
            return brainScanThing;
        }

        var pawnHediffs = pawn.health.hediffSet.hediffs;
        if (pawnHediffs.Count <= 0)
        {
            return brainScanThing;
        }

        foreach (var h in pawnHediffs)
        {
            if (!GeneralCompatibility.ShouldIncludeInBrainTemplate(h.def))
            {
                continue;
            }

            QEEMod.TryLog($"Hediff {h.def.defName} will be added to brain template");

            brainScan.hediffInfos.Add(new HediffInfo(h, GeneralCompatibility.ShouldIncludeSeverityInTemplate(h.def)));
        }

        if (CompatibilityTracker.SkillsExpandedActive)
        {
            VanillaSkillsExpandedCompatibility.GetFieldsFromVanillaSkillsExpanded(pawn, brainScan);
        }

        return brainScanThing;
    }

    public static void ApplyBrainScanTemplateOnPawn(Pawn thePawn, BrainScanTemplate brainScan, float efficency = 1f)
    {
        if (!thePawn.IsValidBrainScanningTarget())
        {
            return;
        }

        //ideo
        if (brainScan.scannedIdeology != null)
        {
            thePawn.ideo.SetIdeo(brainScan.scannedIdeology);
            // brainScan.scannedIdeology;
        }

        //Backgrounds
        var storyTracker = thePawn.story;
        if (storyTracker != null)
        {
            //story.childhood = brainScan.backStoryChild;
            storyTracker.Adulthood = brainScan.backStoryAdult;
        }

        //Skills
        var skillTracker = thePawn.skills;
        if (skillTracker != null)
        {
            foreach (var skill in brainScan.skills)
            {
                var pawnSkill = skillTracker.GetSkill(skill.def);
                pawnSkill.Level = (int)Math.Floor(skill.level * efficency);
                pawnSkill.passion = skill.passion;
            }
        }
        thePawn.Notify_DisabledWorkTypesChanged();

        //Training
        var trainingTracker = thePawn.training;
        if (trainingTracker != null)
        {
            var learned = (DefMap<TrainableDef, bool>)AccessTools.Field(typeof(Pawn_TrainingTracker), "learned")
                .GetValue(trainingTracker);
            var steps = (DefMap<TrainableDef, int>)AccessTools.Field(typeof(Pawn_TrainingTracker), "steps")
                .GetValue(trainingTracker);

            //Copy
            foreach (var item in brainScan.trainingLearned)
            {
                learned[item.Key] = item.Value;
            }

            foreach (var item in brainScan.trainingSteps)
            {
                steps[item.Key] = (int)Math.Floor(item.Value * efficency);
            }
        }

        //Add Hediffs
        thePawn.health.AddHediff(QEHediffDefOf.QE_BrainTemplated);
        if (brainScan.hediffInfos is { Count: > 0 })
        {
            //add hediffs to pawn from defs in HediffInfo class
            foreach (var h in brainScan.hediffInfos)
            {
                var addedHediff = thePawn.health.AddHediff(h.def, h.part);
                if(addedHediff != null && h.severity.HasValue)
                {
                    addedHediff.Severity = h.severity.Value;
                }

                //Psychic Awakened compatibility
                if (h.psychicAwakeningPowersKnownDefNames is not { Count: > 0 })
                {
                    continue;
                }

                //create a List of the type PsychicPowerDef via Reflection. Cast it as IList to interact with it.
                var listType = typeof(List<>).MakeGenericType(PsychicAwakeningCompat.PsychicPowerDefType);
                var powers = Activator.CreateInstance(listType);
                var powersInterface = (IList)powers;

                //iterate through the defNames saved in the list inside HediffInfo
                foreach (var defName in h.psychicAwakeningPowersKnownDefNames)
                {
                    //look for this PsychicPowerDef in the DefDatabase
                    var psychicPowerDef = GenDefDatabase.GetDef(PsychicAwakeningCompat.PsychicPowerDefType,
                        defName, false);

                    if (psychicPowerDef != null)
                    {
                        //add this to the list
                        powersInterface.Add(psychicPowerDef);
                    }
                    else
                    {
                        QEEMod.TryLog(
                            $"Psychic Power def {defName} not loaded in database of Rimworld Defs. This power will not be applied.");
                    }
                }

                if (powersInterface.Count <= 0)
                {
                    continue;
                }

                QEEMod.TryLog(
                    $"assigning {powersInterface.Count} psychic powers to {thePawn.LabelCap} from brain template");

                PsychicAwakeningCompat.powersKnownField.SetValue(addedHediff, powers);
            }
        }

        if (CompatibilityTracker.SkillsExpandedActive)
        {
            VanillaSkillsExpandedCompatibility.SetFieldsToVanillaSkillsExpanded(thePawn, brainScan);
        }

        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(QEHistoryDefOf.BrainUploaded));
        Messages.Message("QE_BrainTemplatingComplete".Translate(thePawn.Named("PAWN")),
            MessageTypeDefOf.PositiveEvent, false);
    }
}