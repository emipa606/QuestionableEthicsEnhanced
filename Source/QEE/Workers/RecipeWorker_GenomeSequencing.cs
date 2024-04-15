using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace QEthics;

public class RecipeWorker_GenomeSequencing : RecipeWorker
{
    public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
    {
        if (pawn.IsValidGenomeSequencingTarget())
        {
            yield return null;
        }
    }

    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        var props = ingredients?.FirstOrDefault()?.def.GetModExtension<RecipeOutcomeProperties>();
        if (props == null)
        {
            return;
        }

        var genomeSequence = GenomeUtility.MakeGenomeSequence(pawn, props.outputThingDef);
        GenPlace.TryPlaceThing(genomeSequence, billDoer.Position, billDoer.Map, ThingPlaceMode.Near);
    }
}