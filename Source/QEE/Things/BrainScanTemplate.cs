using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace QEthics;

/// <summary>
///     A template of a pawns brain. Stores skills and backstories
/// </summary>
public class BrainScanTemplate : ThingWithComps
{
    public BackstoryDef backStoryAdult;

    //Humanoid only
    public BackstoryDef backStoryChild;

    //VanillaSkillsExpanded compat
    public List<ComparableExpertiseRecord> expertises = [];

    /// <summary>
    ///     List containing all hediff def information that should be saved and applied to clones
    /// </summary>
    public List<HediffInfo> hediffInfos = [];

    //Animals only
    public bool isAnimal;
    public PawnKindDef kindDef;
    public Ideo scannedIdeology = null;
    public List<ComparableSkillRecord> skills = [];
    public string sourceName;
    public DefMap<TrainableDef, bool> trainingLearned = new DefMap<TrainableDef, bool>();
    public DefMap<TrainableDef, int> trainingSteps = new DefMap<TrainableDef, int>();

    public override string LabelNoCount
    {
        get
        {
            if (GetComp<CustomNameComp>() is not { } nameComp || !nameComp.customName.NullOrEmpty())
            {
                return base.LabelNoCount;
            }

            return sourceName != null ? $"{sourceName} {base.LabelNoCount}" : base.LabelNoCount;
        }
    }

    public override string DescriptionDetailed => CustomDescriptionString(base.DescriptionDetailed);

    public override string DescriptionFlavor => CustomDescriptionString(base.DescriptionFlavor);

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref sourceName, "sourceName");
        Scribe_Defs.Look(ref kindDef, "kindDef");

        var childhoodIdentifier = backStoryChild?.defName;
        Scribe_Values.Look(ref childhoodIdentifier, "backStoryChild");
        if (Scribe.mode == LoadSaveMode.LoadingVars && !childhoodIdentifier.NullOrEmpty())
        {
            backStoryChild = DefDatabase<BackstoryDef>.GetNamedSilentFail(childhoodIdentifier);
            if (backStoryChild == null)
            {
                //removed the booleans as it appears to be obsolete
                Log.Error($"Couldn't load child backstory with identifier {childhoodIdentifier}. Giving random.");
                backStoryChild = DefDatabase<BackstoryDef>.AllDefsListForReading
                    .Where(backstoryDef => backstoryDef.slot == BackstorySlot.Childhood).RandomElement();
            }
        }

        var adulthoodIdentifier = backStoryAdult?.defName;
        Scribe_Values.Look(ref adulthoodIdentifier, "backStoryAdult");
        if (Scribe.mode == LoadSaveMode.LoadingVars && !adulthoodIdentifier.NullOrEmpty())
        {
            backStoryAdult = DefDatabase<BackstoryDef>.GetNamedSilentFail(adulthoodIdentifier);
            if (backStoryAdult == null)
            {
                //removed boolean as it appeared to be obsolete
                Log.Error($"Couldn't load adult backstory with identifier {adulthoodIdentifier}. Giving random.");
                backStoryAdult = DefDatabase<BackstoryDef>.AllDefsListForReading
                    .Where(backstoryDef => backstoryDef.slot == BackstorySlot.Adulthood).RandomElement();
            }
        }

        Scribe_Collections.Look(ref skills, "skills", LookMode.Deep);
        Scribe_Values.Look(ref isAnimal, "isAnimal");
        Scribe_Deep.Look(ref trainingLearned, "trainingLearned");
        Scribe_Deep.Look(ref trainingSteps, "trainingSteps");
        Scribe_Collections.Look(ref hediffInfos, "hediffInfos", LookMode.Deep);
        Scribe_Collections.Look(ref expertises, "expertises", LookMode.Deep);
        // Scribe_Collections will nullify if node not found, but we expect non-null
        if (Scribe.mode == LoadSaveMode.LoadingVars && expertises == null)
        {
            expertises = [];
        }

        if (Scribe.mode != LoadSaveMode.LoadingVars || hediffInfos == null)
        {
            return;
        }

        //remove any hediffs where the def is missing. Most commonly occurs when a mod is removed from a save.
        var removed = hediffInfos.RemoveAll(h => h.def == null);
        if (removed > 0)
        {
            QEEMod.TryLog(
                $"Removed {removed} null hediffs from hediffInfo list for {sourceName}'s brain template ");
        }
    }

    public string CustomDescriptionString(string baseDescription)
    {
        var builder = new StringBuilder(baseDescription);

        builder.AppendLine();
        builder.AppendLine();
        if (sourceName != null)
        {
            builder.AppendLine("QE_GenomeSequencerDescription_Name".Translate() + ": " + sourceName);
        }

        if (kindDef?.race != null)
        {
            builder.AppendLine("QE_GenomeSequencerDescription_Race".Translate() + ": " + kindDef.race.LabelCap);
        }

        if (backStoryChild != null)
        {
            builder.AppendLine("QE_BrainScanDescription_BackshortChild".Translate() + ": " +
                               backStoryChild.title.CapitalizeFirst());
        }

        if (backStoryAdult != null)
        {
            builder.AppendLine("QE_BrainScanDescription_BackshortAdult".Translate() + ": " +
                               backStoryAdult.title.CapitalizeFirst());
        }

        //ideo
        if (scannedIdeology != null)
        {
            builder.AppendLine($"Scanned Ideology: {scannedIdeology}");
        }

        //Skills
        if (!isAnimal && skills.Count > 0)
        {
            builder.AppendLine("QE_BrainScanDescription_Skills".Translate());
            foreach (var skill in skills.OrderBy(skillRecord => skillRecord.def.index))
            {
                builder.AppendLine(skill.ToString());
            }
        }

        if (expertises.Count > 0)
        {
            builder.AppendLine("QE_BrainScanDescription_Expertises".Translate());
            foreach (var expertise in expertises)
            {
                builder.AppendLine(expertise.ToString());
            }
        }

        if (isAnimal)
        {
            builder.AppendLine("QE_BrainScanDescription_Training".Translate());
            foreach (var training in trainingSteps.OrderBy(trainingPair => trainingPair.Key.index))
            {
                builder.AppendLine("    " + training.Key.LabelCap + ": " + training.Value);
            }
        }

        //Hediffs
        HediffInfo.GenerateDescForHediffList(ref builder, hediffInfos);

        return builder.ToString().TrimEndNewlines();
    }

    //this changes the text displayed in the bottom-left info panel when you select the item
    public override string GetInspectString()
    {
        var builder = new StringBuilder(base.GetInspectString());

        if (kindDef != null)
        {
            builder.AppendLine("QE_GenomeSequencerDescription_Race".Translate() + ": " + kindDef.race.LabelCap);
        }

        if (backStoryChild != null)
        {
            builder.AppendLine("QE_BrainScanDescription_BackshortChild".Translate() + ": " +
                               backStoryChild.title.CapitalizeFirst());
        }

        if (backStoryAdult != null)
        {
            builder.AppendLine("QE_BrainScanDescription_BackshortAdult".Translate() + ": " +
                               backStoryAdult.title.CapitalizeFirst());
        }

        if (hediffInfos is { Count: > 0 })
        {
            builder.AppendLine("QE_GenomeSequencerDescription_Hediffs".Translate() + ": " + hediffInfos.Count);
        }

        return builder.ToString().TrimEndNewlines();
    }

    public override Thing SplitOff(int count)
    {
        var splitThing = base.SplitOff(count);

        if (splitThing == this || splitThing is not BrainScanTemplate brainScan)
        {
            return splitThing;
        }

        //Shared
        brainScan.sourceName = sourceName;
        brainScan.kindDef = kindDef;

        //Humanoid
        brainScan.backStoryChild = backStoryChild;
        brainScan.backStoryAdult = backStoryAdult;
        foreach (var skill in skills)
        {
            brainScan.skills.Add(new ComparableSkillRecord
            {
                def = skill.def,
                level = skill.level,
                passion = skill.passion
            });
        }

        if (hediffInfos != null)
        {
            foreach (var h in hediffInfos)
            {
                brainScan.hediffInfos.Add(h);
            }
        }

        foreach (var expertise in expertises)
        {
            brainScan.expertises.Add(new ComparableExpertiseRecord
            {
                defName = expertise.defName,
                totalXp = expertise.totalXp
            });
        }

        //Animal
        foreach (var item in trainingLearned)
        {
            brainScan.trainingLearned[item.Key] = item.Value;
        }

        foreach (var item in trainingSteps)
        {
            brainScan.trainingSteps[item.Key] = item.Value;
        }

        return splitThing;
    }

    /// <summary>
    ///     Determines whether a list of ComparableSkillRecords is equivalent to another.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool SkillsEqual(List<ComparableSkillRecord> other)
    {
        return skills.Count == other.Count &&
               //UNCOMMENT BELOW FOR ACTIVE DEBUGGING - too spammy even when debugLogging is true
               //bool seqEqual = false;
               //seqEqual = skills.SequenceEqual(other);
               //QEEMod.TryLog("Skills equivalent: " + seqEqual.ToString().ToUpper());
               //return seqEqual;
               skills.SequenceEqual(other);
    }

    public bool ExpertiseEqual(List<ComparableExpertiseRecord> other)
    {
        return expertises.Count == other.Count && expertises.SequenceEqual(other);
    }

    public override bool CanStackWith(Thing other)
    {
        if (other is BrainScanTemplate brainScan &&
            backStoryChild == brainScan.backStoryChild &&
            backStoryAdult == brainScan.backStoryAdult &&
            DefMapsEqual(trainingLearned, brainScan.trainingLearned) &&
            DefMapsEqual(trainingSteps, brainScan.trainingSteps)
            && SkillsEqual(brainScan.skills)
            && ExpertiseEqual(brainScan.expertises)
            && sourceName == brainScan.sourceName &&
            (kindDef?.defName != null && brainScan.kindDef?.defName != null &&
             kindDef.defName == brainScan.kindDef.defName
             || kindDef == null && brainScan.kindDef == null) &&
            (hediffInfos != null && brainScan.hediffInfos != null &&
             hediffInfos.OrderBy(h => h.def.LabelCap.ToString())
                 .SequenceEqual(brainScan.hediffInfos.OrderBy(h => h.def.LabelCap.ToString()))
             || hediffInfos == null && brainScan.hediffInfos == null))
        {
            return base.CanStackWith(other);
        }

        return false;
    }

    public bool DefMapsEqual<T>(DefMap<TrainableDef, T> mapA, DefMap<TrainableDef, T> mapB) where T : new()
    {
        if (mapA.Count != mapB.Count)
        {
            return false;
        }

        foreach (var pair in mapA)
        {
            var validPairs = mapB.Where(pairB => pairB.Key == pair.Key);
            if (validPairs.Any())
            {
                var pairB = validPairs.First();
                if (!pair.Value.Equals(pairB.Value))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
    {
        foreach (var option in base.GetFloatMenuOptions(selPawn))
        {
            yield return option;
        }

        //Start targeter
        yield return new FloatMenuOption("QE_BrainScanningApplyTemplate".Translate(),
            delegate
            {
                var failReason = "";
                var targetParams =
                    new TargetingParameters
                    {
                        canTargetPawns = true,

                        //initial target is valid as long as it's a pawn, it's not the one selected
                        //other checks are less obvious to the player, so inform them with a message in IsValidBrainTemplatingTarget()
                        validator = target => target is { HasThing: true, Thing: Pawn pawn } && pawn != selPawn
                    };

                //do all validation here instead of the validator predicate above, because here we 
                //can write messages to the player if they select an invalid pawn, telling them *why*
                //it's the wrong target
                Find.Targeter.BeginTargeting(targetParams,
                    delegate(LocalTargetInfo target)
                    {
                        if (target.Thing is not Pawn targetPawn)
                        {
                            return;
                        }

                        //begin validation
                        if (targetPawn.IsValidBrainTemplatingTarget(ref failReason, this))
                        {
                            //valid target established, time to find a bed.
                            //Healthy pawns will get up from medical beds immediately, so skip med beds in search
                            var validBed = targetPawn.FindAvailMedicalBed(selPawn);

                            var whyFailed = "";
                            if (validBed == null)
                            {
                                whyFailed = targetPawn.RaceProps.Animal
                                    ? "No animal beds are available"
                                    : "No medical beds are available";
                            }
                            else if (!selPawn.CanReserveAndReach(targetPawn, PathEndMode.OnCell, Danger.Deadly))
                            {
                                whyFailed = $"{selPawn.LabelShort} can't reach/reserve {targetPawn.LabelShort}";
                            }
                            else if (!selPawn.CanReserveAndReach(this, PathEndMode.OnCell, Danger.Deadly))
                            {
                                whyFailed = $"{selPawn.LabelShort} can't reach/reserve the brain template";
                            }
                            //check if bed can be reserved, if patient is not already there
                            else if (targetPawn.CurrentBed() != validBed &&
                                     !selPawn.CanReserveAndReach(validBed, PathEndMode.OnCell, Danger.Deadly))
                            {
                                whyFailed =
                                    $"{selPawn.LabelShort} can't reach/reserve the {validBed.def.defName}";
                            }

                            if (!string.IsNullOrEmpty(whyFailed))
                            {
                                Messages.Message(whyFailed, MessageTypeDefOf.RejectInput, false);
                                SoundDefOf.ClickReject.PlayOneShot(SoundInfo.OnCamera());
                            }
                            else
                            {
                                selPawn.jobs.TryTakeOrderedJob(
                                    new Job(QEJobDefOf.QE_ApplyBrainScanTemplate, targetPawn, this, validBed)
                                    {
                                        count = 1
                                    });
                            }
                        }
                        else
                        {
                            Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                            SoundDefOf.ClickReject.PlayOneShot(SoundInfo.OnCamera());
                        }
                    },
                    selPawn);
            });
    }
}