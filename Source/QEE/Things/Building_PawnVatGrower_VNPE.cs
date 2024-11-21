using System;
using System.Linq;
using PipeSystem;
using QEthics;
using Verse;

namespace BioReactor;

[StaticConstructorOnStartup]
public static class Building_PawnVatGrower_VNPE
{
    public static void VNPE_Check(Building_PawnVatGrower grower)
    {
        if (grower.status != CrafterStatus.Filling)
        {
            return;
        }

        if (!grower.IsHashIntervalTick(60))
        {
            return;
        }

        var compResource = grower.GetComp<CompResource>();

        if (compResource is not { PipeNet: { } net })
        {
            return;
        }

        var stored = net.Stored;

        while (stored > 0 && grower.orderProcessor.PendingRequests.Any())
        {
            var amountToAdd = 33;

            foreach (var request in grower.orderProcessor.GetDesiredRequests())
            {
                if (amountToAdd == 0)
                {
                    break;
                }

                ThingDef thingDef = null;
                if (request.HasThing)
                {
                    thingDef = request.thing.def;
                }

                if (request.HasThingFilter)
                {
                    thingDef = request.thingFilter.BestThingRequest.singleDef;
                }

                if (thingDef == null)
                {
                    continue;
                }

                if (thingDef != QEThingDefOf.QE_NutrientSolution &&
                    thingDef != QEThingDefOf.QE_ProteinMash)
                {
                    continue;
                }

                var thing = ThingMaker.MakeThing(thingDef);
                thing.stackCount = Math.Min(amountToAdd, request.amount);
                amountToAdd -= thing.stackCount;
                grower.innerContainer.TryAddOrTransfer(thing);
                grower.orderProcessor.Notify_ContentsChanged();
            }

            if (amountToAdd == 33)
            {
                break;
            }

            net.DrawAmongStorage(1, net.storages);
            stored--;
        }
    }
}