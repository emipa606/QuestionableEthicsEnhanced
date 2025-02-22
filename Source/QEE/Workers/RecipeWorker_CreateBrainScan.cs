using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace QEthics;

public class RecipeWorker_CreateBrainScan : RecipeWorker
{
    public override AcceptanceReport AvailableReport(Thing thing, BodyPartRecord part = null)
    {
        var reason = "";
        if (thing is Pawn pawn && BrainManipUtility.IsValidBrainScanningTarget(pawn, ref reason, true))
        {
            return true;
        }

        return reason;
    }

    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        var props = ingredients?.FirstOrDefault()?.def.GetModExtension<RecipeOutcomeProperties>();
        if (props != null)
        {
            var brainScan = BrainManipUtility.MakeBrainScan(pawn, props.outputThingDef);
            GenPlace.TryPlaceThing(brainScan, billDoer.Position, billDoer.Map, ThingPlaceMode.Near);
        }

        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(QEHistoryDefOf.BrainScan,
            billDoer.Named(HistoryEventArgsNames.Doer)));

        //Give headache
        pawn.health.AddHediff(QEHediffDefOf.QE_Headache, pawn.health.hediffSet.GetBrain());
    }
}