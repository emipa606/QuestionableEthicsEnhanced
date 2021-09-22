using RimWorld;

public class QEE_HediffComp_CyberSuppressed : Verse.HediffComp
{
    public override void CompPostTick(ref float var)
    {
        //Sets the pawn suppression to max, an rather inelegant, but simple and workable solition to maxing suppression for the 
        Pawn.needs.TryGetNeed<RimWorld.Need_Suppression>().CurLevelPercentage = Pawn.needs.TryGetNeed<RimWorld.Need_Suppression>().MaxLevel;
        //if pawn enters an aggressive mental state, anesthese them. Should work with all mental breaks tagged as aggressive.
        if(Pawn.InAggroMentalState==true ||SlaveRebellionUtility.IsRebelling(Pawn) || PrisonBreakUtility.IsPrisonBreaking(Pawn))
        {
            Pawn.health.AddHediff(QEthics.QEHediffDefOf.QE_NeuralDisruption);
        }
        
    }
}