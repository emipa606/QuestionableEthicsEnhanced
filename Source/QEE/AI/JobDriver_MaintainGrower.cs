using System.Collections.Generic;
using RimWorld;
using Verse.AI;

namespace QEthics;

/// <summary>
///     THIS IS CLASS WILL BE DEPRECATED SOON. Do not use in new development work. It is now only used in the Pawn Vat.
/// </summary>
public class JobDriver_MaintainGrower : JobDriver
{
    public MaintainVatProperties VatJobProperties => job.def.GetModExtension<MaintainVatProperties>();

    public IMaintainableGrower Maintainable => TargetThingA as IMaintainableGrower;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.CanReserve(TargetThingA) && pawn.Reserve(TargetThingA, job, errorOnFailed: errorOnFailed);
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() => TargetThingA is Building_GrowerBase vat && vat.status != CrafterStatus.Crafting);
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnDestroyedOrNull(TargetIndex.A);
        yield return Toils_Reserve.Reserve(TargetIndex.A);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
        yield return new Toil
        {
            defaultCompleteMode = ToilCompleteMode.Never,
            tickAction = delegate
            {
                var props = VatJobProperties;
                if (props.maintainingSkill == SkillDefOf.Intellectual)
                {
                    Maintainable.ScientistMaintenance += 1f / 600f * pawn.GetStatValue(StatDefOf.ResearchSpeed);
                    if (Maintainable.ScientistMaintenance >= 1f)
                    {
                        Maintainable.ScientistMaintenance = 1f;
                        EndJobWith(JobCondition.Succeeded);
                    }
                }
                else if (props.maintainingSkill == SkillDefOf.Medicine)
                {
                    Maintainable.DoctorMaintenance += 1f / 600f * pawn.GetStatValue(StatDefOf.MedicalTendSpeed);
                    if (Maintainable.DoctorMaintenance >= 1f)
                    {
                        Maintainable.DoctorMaintenance = 1f;
                        EndJobWith(JobCondition.Succeeded);
                    }
                }

                pawn.skills.Learn(props.maintainingSkill, 200f / 600f);
            }
        }.WithProgressBar(TargetIndex.A,
            delegate
            {
                var props = VatJobProperties;
                if (props.maintainingSkill == SkillDefOf.Intellectual)
                {
                    return Maintainable.ScientistMaintenance;
                }

                return props.maintainingSkill == SkillDefOf.Medicine ? Maintainable.DoctorMaintenance : 0f;
            }).WithEffect(EffecterDefOf.Research, TargetIndex.A).PlaySustainerOrSound(QESoundDefOf.Interact_Research);
    }
}