using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace QEthics;

/// <summary>
///     A medical bed that automatically perform surgeries and healing.
/// </summary>
public class Building_AutoDoctor : Building_Bed
{
    /// <summary>
    ///     This is never saved. Destroyed upon on despawn.
    /// </summary>
    public Pawn tempSurgeonPawn;

    public CompRefuelable RefueableComp => this.TryGetComp<CompRefuelable>();

    public override void Tick()
    {
        base.Tick();

        //Check all pawns in the bed.
        for (var i = 0; i < SleepingSlotsCount; i++)
        {
            var sleepingSpot = GetSleepingSlotPos(i);
            var pawn = sleepingSpot.GetFirstPawn(Map);
            if (pawn == null || !pawn.CurrentlyUsableForBills())
            {
                continue;
            }

            //Go through the Bills and find one we can do.
            if (!pawn.BillStack.AnyShouldDoNow)
            {
                continue;
            }

            var searchRadius = def.specialDisplayRadius;
            var foundIngredients = new List<Thing>();

            foreach (var bill in pawn.BillStack.Bills)
            {
                foundIngredients.Clear();

                foreach (var ingredient in bill.recipe.ingredients)
                {
                    if (ingredient.filter.Allows(RefueableComp.Props.fuelFilter.AnyAllowedDef))
                    {
                        continue;
                    }

                    //Look for ingredient in radius.
                    var ingredientThing = GenClosest.ClosestThingReachable(Position, Map,
                        ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.OnCell,
                        TraverseParms.For(TraverseMode.NoPassClosedDoors),
                        searchRadius,
                        thing => !thing.IsForbidden(Faction) && ingredient.filter.Allows(thing) &&
                                 thing.stackCount >= ingredient.CountRequiredOfFor(thing.def, bill.recipe));

                    if (ingredientThing == null)
                    {
                        continue;
                    }

                    //Found a valid ingredient to use.
                    foundIngredients.Add(ingredientThing);
                    //If its medicine, skip it.
                }
            }
        }
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        //Create temporary surgeon.
        if (tempSurgeonPawn != null)
        {
            return;
        }

        var request = new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction,
            canGeneratePawnRelations: false,
            forceGenerateNewPawn: true,
            allowFood: false,
            fixedBiologicalAge: 20,
            fixedChronologicalAge: 20);
        tempSurgeonPawn = PawnGenerator.GeneratePawn(request);
        tempSurgeonPawn.Name = new NameTriple(LabelCap, LabelCap, "");
        tempSurgeonPawn.story.traits.allTraits.Clear();
        tempSurgeonPawn.skills.Notify_SkillDisablesChanged();
        tempSurgeonPawn.story.childhood =
            DefDatabase<BackstoryDef>.GetNamed("Backstory_ColonyVatgrown").GetFromDatabase();
        tempSurgeonPawn.story.adulthood = null;
        AccessTools.Field(typeof(Pawn_StoryTracker), "cachedDisabledWorkTypes")
            .SetValue(tempSurgeonPawn.story, null);
        tempSurgeonPawn.skills.GetSkill(SkillDefOf.Medicine).Level = 20;
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        base.DeSpawn(mode);

        //Destroy and remove temporary surgeon.
        if (tempSurgeonPawn == null)
        {
            return;
        }

        tempSurgeonPawn.Destroy();
        tempSurgeonPawn = null;
    }
}