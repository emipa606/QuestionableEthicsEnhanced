using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace QEthics;

public class RecipeWorker_CreateBrainScan : RecipeWorker
{
    //public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
    //{
    //    if (pawn.IsValidBrainScanningTarget())
    //    {
    //        QEEMod.TryLog($"Pawn {pawn.Name?.ToStringSafe() ?? pawn.LabelCap} is valid brain scanning target");
    //        yield return null;
    //    }
    //    else
    //    {
    //        QEEMod.TryLog($"Pawn {pawn.Name?.ToStringSafe() ?? pawn.LabelCap} is not valid brain scanning target");
    //    }
    //}

    public override AcceptanceReport AvailableReport(Thing thing, BodyPartRecord part = null)
    {
        string reason = "";
        if(thing is Pawn pawn && BrainManipUtility.IsValidBrainScanningTarget(pawn, ref reason, true))
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