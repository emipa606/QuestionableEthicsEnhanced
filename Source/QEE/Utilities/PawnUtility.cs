using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace QEthics;

[StaticConstructorOnStartup]
public static class PawnUtility
{
    private static List<ThingDef> bedDefsBestToWorst_Medical;

    static PawnUtility()
    {
        Reset();
    }

    public static Building_Bed FindAvailMedicalBed(this Pawn sleeper, Pawn traveler)
    {
        var sleeperWillBePrisoner = sleeper.IsPrisoner;

        if (sleeper.InBed() && sleeper.CurrentBed().Medical)
        {
            QEEMod.TryLog("Sleeper is in medical bed");
            return sleeper.CurrentBed();
        }

        //QEEMod.TryLog("Count of all BedDefs: " + RestUtility.AllBedDefBestToWorst.Count);
        //for (int i = 0; i < RestUtility.AllBedDefBestToWorst.Count; i++)
        QEEMod.TryLog($"Count of all BedDefs: {bedDefsBestToWorst_Medical.Count}");
        foreach (var bedDef in bedDefsBestToWorst_Medical)
        {
            if (!RestUtility.CanUseBedEver(sleeper, bedDef))
            {
                continue;
            }

            var building_Bed = (Building_Bed)GenClosest.ClosestThingReachable(sleeper.Position, sleeper.Map,
                ThingRequest.ForDef(bedDef), PathEndMode.OnCell, TraverseParms.For(traveler), 9999f,
                b => IsValidMedicalBed(b, sleeper, traveler, sleeperWillBePrisoner, false));

            if (building_Bed == null)
            {
                continue;
            }

            QEEMod.TryLog("QE_BedSelected".Translate(building_Bed.def.defName));
            return building_Bed;
        }

        return null;
    }

    //this is a modified version of vanilla's RestUtility.IsValidBedFor() that adds medical bed checks
    public static bool IsValidMedicalBed(Thing bedThing, Pawn sleeper, Pawn traveler, bool sleeperWillBePrisoner,
        bool checkSocialProperness, bool ignoreOtherReservations = false)
    {
        if (bedThing is not Building_Bed building_Bed)
        {
            QEEMod.TryLog("QE_BedInvalid".Translate(bedThing.def.defName));
            return false;
        }

        LocalTargetInfo target = building_Bed;
        var peMode = PathEndMode.OnCell;
        var maxDanger = Danger.Some;
        var sleepingSlotsCount = building_Bed.SleepingSlotsCount;

        if (!building_Bed.Medical && !sleeper.RaceProps.Animal)
        {
            QEEMod.TryLog("QE_BedIsNotMedical".Translate(building_Bed.def.defName));
            return false;
        }

        if (!traveler.CanReserveAndReach(target, peMode, maxDanger, sleepingSlotsCount, -1, null,
                ignoreOtherReservations))
        {
            QEEMod.TryLog("QE_BedUnreachableBySurgeon".Translate(building_Bed.def.defName, traveler.Named("SURGEON")));
            return false;
        }

        if (building_Bed.Position.GetDangerFor(sleeper, sleeper.Map) > maxDanger)
        {
            QEEMod.TryLog("QE_BedTooDangerous".Translate(building_Bed.def.defName, traveler.Named("PATIENT")));
            return false;
        }

        if (traveler.HasReserved<JobDriver_TakeToBed>(building_Bed, sleeper))
        {
            QEEMod.TryLog(
                "QE_BedSurgeonAlreadyReserved".Translate(building_Bed.def.defName, traveler.Named("SURGEON")));
            return false;
        }

        if (!building_Bed.AnyUnoccupiedSleepingSlot && (!sleeper.InBed() || sleeper.CurrentBed() != building_Bed) &&
            !building_Bed.CompAssignableToPawn.AssignedPawns.Contains(sleeper))
        {
            QEEMod.TryLog("QE_BedOccupied".Translate(building_Bed.def.defName));
            return false;
        }

        if (building_Bed.IsForbidden(traveler))
        {
            QEEMod.TryLog("QE_BedForbidden".Translate(building_Bed.def.defName));
            return false;
        }

        if (checkSocialProperness && !building_Bed.IsSociallyProper(sleeper, sleeperWillBePrisoner))
        {
            QEEMod.TryLog("QE_BedFailsSocialChecks".Translate(building_Bed.def.defName));
            return false;
        }

        if (building_Bed.IsBurning())
        {
            QEEMod.TryLog("QE_BedIsBurning".Translate(building_Bed.def.defName));
            return false;
        }

        if (sleeperWillBePrisoner)
        {
            if (!building_Bed.ForPrisoners)
            {
                QEEMod.TryLog(
                    "QE_BedImproperForPrisoner".Translate(building_Bed.def.defName, traveler.Named("PATIENT")));
                return false;
            }

            if (!building_Bed.Position.IsInPrisonCell(building_Bed.Map))
            {
                QEEMod.TryLog("QE_BedNotInPrisonCell".Translate(building_Bed.def.defName, traveler.Named("PATIENT")));
                return false;
            }
        }
        else
        {
            if (building_Bed.Faction != traveler.Faction &&
                (traveler.HostFaction == null || building_Bed.Faction != traveler.HostFaction))
            {
                QEEMod.TryLog("QE_BedImproperFaction".Translate(building_Bed.def.defName, traveler.Named("SURGEON")));
                return false;
            }

            if (building_Bed.ForPrisoners)
            {
                QEEMod.TryLog(
                    "QE_PrisonerBedForNonPrisoner".Translate(building_Bed.def.defName, traveler.Named("PATIENT")));
                return false;
            }
        }

        //fail if bed is owned
        if (!building_Bed.OwnersForReading.Any() || building_Bed.OwnersForReading.Contains(sleeper) ||
            building_Bed.AnyUnownedSleepingSlot)
        {
            return true;
        }

        QEEMod.TryLog("QE_BedHasAnotherOwner".Translate(building_Bed.def.defName));
        return false;
    }

    public static void Reset()
    {
        bedDefsBestToWorst_Medical = (from d in DefDatabase<ThingDef>.AllDefs
            where d.IsBed
            orderby d.building.bed_maxBodySize, d.GetStatValueAbstract(StatDefOf.MedicalTendQualityOffset) descending,
                d.GetStatValueAbstract(StatDefOf.BedRestEffectiveness) descending
            select d).ToList();
    }

    public static bool bodyPartOrChildHasHediffs(Pawn aPawn, BodyPartRecord part, out BodyPartRecord damagedChildPart)
    {
        damagedChildPart = null;

        foreach (var curHediff in aPawn.health.hediffSet.hediffs)
        {
            //skip the Organ Rejection hediff
            if (curHediff.def == QEHediffDefOf.QE_OrganRejection)
            {
                continue;
            }

            //check if the hediff is attached to any body part
            if (curHediff.Part == null)
            {
                continue;
            }

            //is this hediff attached to this body part?
            if (curHediff.Part == part)
            {
                damagedChildPart = curHediff.Part;
                break;
            }

            //is this the hediff's parent BodyPart? Example: bodypart is hand and hediff is missing finger
            if (curHediff.Part?.parent == null || curHediff.Part.parent.Label == "torso")
            {
                continue;
            }

            if (curHediff.Part.parent == part)
            {
                damagedChildPart = curHediff.Part.parent;
                break;
            }

            //is this the hediff's grandparent BodyPart? Example: bodypart is arm and hediff is missing finger
            if (curHediff.Part.parent?.parent == null || curHediff.Part.parent.parent.Label == "torso")
            {
                continue;
            }

            if (curHediff.Part.parent.parent == part)
            {
                damagedChildPart = curHediff.Part.parent.parent;
                break;
            }

            //is this the hediff's great-grandparent BodyPart? Example: Nothing I can think of. In here for good measure
            if (curHediff.Part?.parent?.parent?.parent == null ||
                curHediff.Part.parent.parent.parent.Label == "torso")
            {
                continue;
            }

            if (curHediff.Part.parent.parent.parent != part)
            {
                continue;
            }

            damagedChildPart = curHediff.Part.parent.parent.parent;
            break;
        }

        return damagedChildPart != null;
    }
}