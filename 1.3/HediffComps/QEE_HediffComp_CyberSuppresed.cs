using RimWorld;

namespace QEthics
{
    public class QEE_HediffComp_CyberSuppressed : Verse.HediffComp
    {
        //caches the should disrupt, for hopeful minor improvements.
        public bool shouldDisrupt = QEESettings.instance.neuralDisrupt;
        public int i = 1;
        
        public override void CompPostTick(ref float var)
        {

            if (i % 4 == 0)
            {
                if ((Pawn.InAggroMentalState == true || SlaveRebellionUtility.IsRebelling(Pawn) || PrisonBreakUtility.IsPrisonBreaking(Pawn)) && shouldDisrupt)
                {
                    Pawn.health.AddHediff(QEthics.QEHediffDefOf.QE_NeuralDisruption);
                }
                
            }

            if (i % 256 == 0)
            {
                Need_Suppression sup = Pawn.needs.TryGetNeed<RimWorld.Need_Suppression>();
                //Sets the pawn suppression to max, an rather inelegant, but simple and workable solition to maxing suppression for the pawn. (null check suggested by bratwurstinator to allow mod to not need patch to run with ideology)
                if (sup != null)
                {
                    sup.CurLevelPercentage = sup.MaxLevel;
                }
                //if pawn enters an aggressive mental state, anesthese them. Should work with all mental breaks tagged as aggressive.
               
            }
            else { i += 1; }
            

        }
    }
}