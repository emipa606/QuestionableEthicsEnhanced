public class QEE_HediffComp_CyberSuppressed : Verse.HediffComp
{
    public override void CompPostTick(ref float var)
    {

        Pawn.needs.TryGetNeed<RimWorld.Need_Suppression>().CurLevelPercentage = Pawn.needs.TryGetNeed<RimWorld.Need_Suppression>().MaxLevel;
    }
}