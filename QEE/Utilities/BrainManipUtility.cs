using System;
using System.Linq;
using Verse;
using RimWorld;
using Harmony;
using System.Collections.Generic;
using System.Collections;

namespace QEthics
{
    public static class BrainManipUtility
    {
        public static bool IsValidBrainScanningDef(this ThingDef def)
        {
            return !def.race.IsMechanoid && !GeneralCompatibility.excludedRaces.Contains(def);
        }

        public static bool IsValidBrainScanningTarget(this Pawn pawn)
        {
            ThingDef def = pawn.def;
            return IsValidBrainScanningDef(def) && !pawn.Dead && !pawn.health.hediffSet.hediffs.Any(hediff => GeneralCompatibility.excludedHediffs.Any(hediffDef => hediff.def == hediffDef));
        }

        public static bool IsValidBrainScanningTarget(Pawn targetPawn, ref string failReason)
        {
            ThingDef def = targetPawn.def;

            if(!IsValidBrainScanningDef(def))
            {
                failReason = "QE_BrainScanningRejectExcludedRace".Translate(targetPawn.kindDef.race);
                return false;
            }

            else if(targetPawn.Dead)
            {
                failReason = "QE_BrainScanningRejectDead".Translate(targetPawn.Named("PAWN"));
                return false;
            }
            //fail if pawn has an excluded hediff
            else if (targetPawn.health.hediffSet.hediffs.Any(hediff => GeneralCompatibility.excludedHediffs.Any(hediffDef => hediff.def == hediffDef)))
            {
                failReason = "QE_BrainScanningRejectExcludedHediff".Translate(targetPawn.Named("PAWN"));
                return false;
            }

            else
            {
                return true;
            }
        }

        public static bool IsValidBrainTemplatingTarget(this Pawn targetPawn, ref string failReason, BrainScanTemplate template)
        {           
            if (!IsValidBrainScanningTarget(targetPawn, ref failReason))
            {
                return false;
            }

            if(template.isAnimal)
            {
                //fail if trying to apply animal template to non-animal
                if (!targetPawn.RaceProps.Animal)
                {
                    failReason = "QE_BrainScanningRejectNotAnimal".Translate(targetPawn.LabelShort);
                    return false;
                }
                //fail if trying to apply template with different PawnKindDefs. Only do this for animals, because cloned humanlikes have different KindDefs
                else if (template.kindDef?.race != null && template.kindDef.defName != targetPawn.kindDef.defName)
                {
                    failReason = "QE_BrainScanningRejectWrongKind".Translate(targetPawn.LabelShort, template.kindDef.race.LabelCap);
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
            if (QEESettings.instance.brainTemplatingRequiresClone && !targetPawn.health.hediffSet.HasHediff(QEHediffDefOf.QE_CloneStatus, false))
            {
                failReason = "QE_BrainScanningRejectNotClone".Translate(targetPawn.Named("PAWN"));
                return false;
            }
            //fail if they have been templated in the past
            if (targetPawn.health.hediffSet.HasHediff(QEHediffDefOf.QE_BrainTemplated, false))
            {
                failReason = "QE_BrainScanningRejectAlreadyTemplated".Translate(targetPawn.Named("PAWN"));
                return false;
            }

            return true;
        }

        public static Thing MakeBrainScan(Pawn pawn, ThingDef genomeDef)
        {
            Thing brainScanThing = ThingMaker.MakeThing(genomeDef);
            BrainScanTemplate brainScan = brainScanThing as BrainScanTemplate;
            if (brainScan != null)
            {
                //Standard.
                brainScan.sourceName = pawn?.Name?.ToStringFull ?? null;
                brainScan.kindDef = pawn?.kindDef ?? null;

                //Backgrounds
                Pawn_StoryTracker story = pawn.story;
                if (story != null)
                {
                    brainScan.backStoryChild = story.childhood;
                    brainScan.backStoryAdult = story.adulthood;
                }

                //Skills
                Pawn_SkillTracker skillTracker = pawn.skills;
                if(skillTracker != null)
                {
                    foreach (SkillRecord skill in skillTracker.skills)
                    {
                        brainScan.skills.Add(new ComparableSkillRecord()
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
                Pawn_TrainingTracker trainingTracker = pawn.training;
                if(trainingTracker != null)
                {
                    DefMap<TrainableDef, bool> learned = (DefMap<TrainableDef, bool>)AccessTools.Field(typeof(Pawn_TrainingTracker), "learned").GetValue(trainingTracker);
                    DefMap<TrainableDef, int> steps = (DefMap<TrainableDef, int>)AccessTools.Field(typeof(Pawn_TrainingTracker), "steps").GetValue(trainingTracker);

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
                if (pawn?.health?.hediffSet?.hediffs != null)
                {
                    List<Hediff> pawnHediffs = pawn.health.hediffSet.hediffs;
                    if (pawnHediffs.Count > 0)
                    {
                        foreach (Hediff h in pawnHediffs)
                        {
                            if (GeneralCompatibility.includedBrainTemplateHediffs.Any(hediffDef => h.def.defName == hediffDef.defName))
                            {
                                QEEMod.TryLog("Hediff " + h.def.defName + " will be added to brain template");

                                brainScan.hediffInfos.Add(new HediffInfo(h));
                            }
                        }
                    }
                }
            }

            return brainScanThing;
        }

        public static void ApplyBrainScanTemplateOnPawn(Pawn thePawn, BrainScanTemplate brainScan, float efficency = 1f)
        {
            if(thePawn.IsValidBrainScanningTarget())
            {
                //Backgrounds
                Pawn_StoryTracker storyTracker = thePawn.story;
                if (storyTracker != null)
                {
                    //story.childhood = brainScan.backStoryChild;
                    storyTracker.adulthood = brainScan.backStoryAdult;
                }

                //Skills
                Pawn_SkillTracker skillTracker = thePawn.skills;
                if (skillTracker != null)
                {
                    foreach (ComparableSkillRecord skill in brainScan.skills)
                    {
                        SkillRecord pawnSkill = skillTracker.GetSkill(skill.def);
                        pawnSkill.Level = (int)Math.Floor((float)skill.level * efficency);
                        pawnSkill.passion = skill.passion;
                        pawnSkill.Notify_SkillDisablesChanged();
                    }
                }

                //Dirty hack ahoy!
                if(storyTracker != null)
                {
                    AccessTools.Field(typeof(Pawn_StoryTracker), "cachedDisabledWorkTypes").SetValue(storyTracker, null);
                }

                //Training
                Pawn_TrainingTracker trainingTracker = thePawn.training;
                if (trainingTracker != null)
                {
                    DefMap<TrainableDef, bool> learned = (DefMap<TrainableDef, bool>)AccessTools.Field(typeof(Pawn_TrainingTracker), "learned").GetValue(trainingTracker);
                    DefMap<TrainableDef, int> steps = (DefMap<TrainableDef, int>)AccessTools.Field(typeof(Pawn_TrainingTracker), "steps").GetValue(trainingTracker);

                    //Copy
                    foreach (var item in brainScan.trainingLearned)
                    {
                        learned[item.Key] = item.Value;
                    }
                    foreach (var item in brainScan.trainingSteps)
                    {
                        steps[item.Key] = (int)Math.Floor((float)item.Value * efficency);
                    }
                }

                //Add Hediffs
                thePawn.health.AddHediff(QEHediffDefOf.QE_BrainTemplated);
                if (brainScan.hediffInfos != null && brainScan.hediffInfos?.Count > 0)
                {
                    //add hediffs to pawn from defs in HediffInfo class
                    foreach (HediffInfo h in brainScan.hediffInfos)
                    {
                        Hediff addedHediff = thePawn.health.AddHediff(h.def, h.part);

                        //Psychic Awakened compatibility
                        if(h.psychicAwakeningPowersKnownDefNames != null && h.psychicAwakeningPowersKnownDefNames?.Count > 0)
                        {
                            //create a List of the type PsychicPowerDef via Reflection. Cast it as IList to interact with it.
                            var listType = typeof(List<>).MakeGenericType(PsychicAwakeningCompat.PsychicPowerDefType);
                            var powers = Activator.CreateInstance(listType);
                            IList powersInterface = (IList)powers;

                            //iterate through the defNames saved in the list inside HediffInfo
                            foreach (string defName in h.psychicAwakeningPowersKnownDefNames)
                            {
                                //look for this PsychicPowerDef in the DefDatabase
                                var psychicPowerDef = GenDefDatabase.GetDef(PsychicAwakeningCompat.PsychicPowerDefType, defName, false);

                                if (psychicPowerDef != null)
                                {
                                    //add this to the list
                                    powersInterface.Add(psychicPowerDef);
                                }
                                else
                                {
                                    QEEMod.TryLog("Psychic Power def " + defName + " not loaded in database of Rimworld Defs. This power will not be applied.");
                                }
                            }

                            if(powersInterface.Count > 0)
                            {
                                QEEMod.TryLog("assigning " + powersInterface.Count + " psychic powers to " + thePawn.LabelCap + " from brain template");

                                PsychicAwakeningCompat.powersKnownField.SetValue(addedHediff, powers);
                            }
                        }                       
                    }
                }

                Messages.Message("QE_BrainTemplatingComplete".Translate(thePawn.Named("PAWN")), MessageTypeDefOf.PositiveEvent, false);
            }
        }
    }
}
