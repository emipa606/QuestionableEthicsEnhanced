using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace QEthics;

public class RecipeWorker_CreateBrainScan : RecipeWorker
{
    public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
    {
        if (pawn.IsValidBrainScanningTarget())
        {
            yield return null;
        }
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