using System.Collections.Generic;
using RimWorld;
using Verse;

namespace QEthics.Ideology;

//new nerve stapling behavior
public class RecipeWorker_NerveStapling_Ideology : Recipe_InstallImplant
{
    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);

        //Check if the Hediff got applied.
        if (!pawn.health.hediffSet.HasHediff(recipe.addsHediff))
        {
            return;
        }

        //Convert to billDoer faction.
        var isColonist = pawn.Faction == billDoer.Faction;


        //Apply thoughts.
        pawn.needs.mood.thoughts.memories.TryGainMemory(QEThoughtDefOf.QE_RecentlyNerveStapled);
        pawn.needs.mood.thoughts.memories.TryGainMemory(QEThoughtDefOf.QE_NerveStapledMe, billDoer);
        //deprecated for loop that is no longer neccessary. Code now decides whether pawn is colonist or not and sends out the appropiate history event.
        //foreach (Pawn thoughtReciever in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners)
        //{
        //if (thoughtReciever != pawn)
        //{
        if (isColonist)
        {
            Find.HistoryEventsManager.RecordEvent(new HistoryEvent(QEHistoryDefOf.NerveStapledColonist,
                billDoer.Named(HistoryEventArgsNames.Doer)));
        }
        else
        {
            Find.HistoryEventsManager.RecordEvent(new HistoryEvent(QEHistoryDefOf.NerveStapledPawn,
                billDoer.Named(HistoryEventArgsNames.Doer)));
        }

        //}
        //}
        CyberEnslave(billDoer, pawn);
    }

    public static void CyberEnslave(Pawn surgeon, Pawn stapledPawn)
    {
        if (stapledPawn.IsSlave)
        {
            return;
        }

        var unused = stapledPawn.guest.EverEnslaved;
        stapledPawn.guest.SetGuestStatus(surgeon.Faction, GuestStatus.Slave);
        Messages.Message("MessagestapledPawnEnslaved".Translate(stapledPawn, surgeon),
            new LookTargets(stapledPawn, surgeon), MessageTypeDefOf.NeutralEvent);
    }
}