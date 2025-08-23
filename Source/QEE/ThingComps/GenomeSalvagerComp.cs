using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace QEthics;

public class GenomeSalvagerComp : ThingComp
{
    private CompProperties_GenomeSalvager SalvagerProps => props as CompProperties_GenomeSalvager;

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        //Start targeter
        if (selPawn.skills.GetSkill(SkillDefOf.Medicine).TotallyDisabled)
        {
            yield break;
        }

        var chance = 0.6f * selPawn.GetStatValue(StatDefOf.MedicalSurgerySuccessChance);
        yield return new FloatMenuOption("QE_GenomeSequencerSalvage".Translate(chance.ToStringPercent()),
            delegate
            {
                var targetParams =
                    new TargetingParameters
                    {
                        canTargetPawns = false,
                        canTargetBuildings = false,
                        canTargetItems = true,
                        mapObjectTargetsMustBeAutoAttackable = false,
                        validator = target =>
                            target is { HasThing: true, Thing: Corpse corpse } &&
                            corpse.InnerPawn.IsValidGenomeSequencingTarget()
                    };

                Find.Targeter.BeginTargeting(targetParams,
                    delegate(LocalTargetInfo target)
                    {
                        if (target.Thing is Corpse corpse &&
                            selPawn.CanReserveAndReach(parent, PathEndMode.OnCell, Danger.Deadly) &&
                            selPawn.CanReserveAndReach(target, PathEndMode.OnCell, Danger.Deadly))
                        {
                            selPawn.jobs.TryTakeOrderedJob(new Job(SalvagerProps.salvagingJob, corpse, parent)
                            {
                                count = 1
                            });
                        }
                    },
                    selPawn);
            });
    }
}