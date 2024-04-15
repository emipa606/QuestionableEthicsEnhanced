using RimWorld;
using Verse;

namespace QEthics;

public class QEE_HediffComp_CyberSuppressed : HediffComp
{
    //caches they should disrupt, for hopeful minor improvements.
    public readonly bool shouldDisrupt = QEESettings.instance.neuralDisrupt;
    public int i = 1;

    public override void CompPostTick(ref float var)
    {
        if (i % 4 == 0)
        {
            if ((Pawn.InAggroMentalState || SlaveRebellionUtility.IsRebelling(Pawn) ||
                 PrisonBreakUtility.IsPrisonBreaking(Pawn)) && shouldDisrupt)
            {
                Pawn.health.AddHediff(QEHediffDefOf.QE_NeuralDisruption);
            }
        }

        if (i % 256 == 0)
        {
            var sup = Pawn.needs.TryGetNeed<Need_Suppression>();
            //Sets the pawn suppression to max, a rather inelegant, but simple and workable solition to maxing suppression for the pawn. (null check suggested by bratwurstinator to allow mod to not need patch to run with ideology)
            if (sup != null)
            {
                sup.CurLevelPercentage = sup.MaxLevel;
            }
            //if pawn enters an aggressive mental state, anesthese them. Should work with all mental breaks tagged as aggressive.
        }
        else
        {
            i += 1;
        }
    }
}