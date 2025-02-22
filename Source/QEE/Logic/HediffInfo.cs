using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace QEthics;

/// <summary>
///     Used to save hediff information of a pawn for use in cloning. An instance of this class is saved in the Genome
///     Template or Brain Template
///     and the hediffs within are then applied to the clone.
/// </summary>
public class HediffInfo : IExposable, IEquatable<HediffInfo>
{
    public HediffDef def;
    public BodyPartRecord part;
    public float? severity;
    public List<string> psychicAwakeningPowersKnownDefNames;

    public bool Equals(HediffInfo other)
    {
        if (other == null)
        {
            return false;
        }

        return (def?.defName == null && other.def?.defName == null ||
                def?.defName == other.def?.defName) &&
               (part == null && other.part == null ||
                part?.LabelCap == other.part.LabelCap) &&
                (severity == null && other.severity == null || severity == other.severity) &&
               (psychicAwakeningPowersKnownDefNames == null && other.psychicAwakeningPowersKnownDefNames == null ||
                psychicAwakeningPowersKnownDefNames != null && other.psychicAwakeningPowersKnownDefNames != null &&
                psychicAwakeningPowersKnownDefNames.OrderBy(a => a)
                    .SequenceEqual(other.psychicAwakeningPowersKnownDefNames.OrderBy(a => a)));
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, "hediffDef");
        Scribe_BodyParts.Look(ref part, "bodyPart");
        Scribe_Values.Look(ref severity, "severity");
        Scribe_Collections.Look(ref psychicAwakeningPowersKnownDefNames, "psychicAwakeningPowersKnownDefNames");
    }

    public override int GetHashCode()
    {
        var hash = 19;
        hash = (hash * 23) + (def?.GetHashCode() ?? 0);
        //the LabelCap field should be unique enough for the hash
        hash = (hash * 23) + (part?.LabelCap.GetHashCode() ?? 0);
        hash = (hash * 23) + severity.GetHashCode();
        hash = (hash * 23) + (psychicAwakeningPowersKnownDefNames?.GetHashCode() ?? 0);
        return hash;
    }

    public static void GenerateDescForHediffList(ref StringBuilder builder, List<HediffInfo> hediffs)
    {
        var hediffsNonNull = hediffs?.Where(h => h.def != null);

        if (hediffsNonNull == null)
        {
            return;
        }

        if (!hediffsNonNull.Any())
        {
            return;
        }

        builder.AppendLine("QE_GenomeSequencerDescription_Hediffs".Translate());

        //sort hediffs in alphabetical order
        var ordered = hediffsNonNull.OrderBy(h => h.def.LabelCap.RawText);

        //loop through hediffs and add line to StringBuilder for each
        foreach (var h in ordered)
        {
            if (h.part != null)
            {
                builder.AppendLine("    " + h.def.LabelCap + " [" + h.part.LabelCap + "]");

                //Psychic Awakening compatibility
                if (h.psychicAwakeningPowersKnownDefNames is not { Count: > 0 })
                {
                    continue;
                }

                foreach (var defName in h.psychicAwakeningPowersKnownDefNames.OrderBy(a => a))
                {
                    var psychicPowerDef = GenDefDatabase.GetDef(PsychicAwakeningCompat.PsychicPowerDefType,
                        defName, false);

                    if (psychicPowerDef != null)
                    {
                        builder.AppendLine("        " + psychicPowerDef.LabelCap);
                    }
                    else
                    {
                        builder.AppendLine($"        {defName}");
                    }
                }
            }
            else
            {
                builder.AppendLine("    " + h.def.LabelCap);
            }
        }
    }

    #region Constructors

    public HediffInfo()
    {
    }

    public HediffInfo(Hediff h, bool shouldRecordSeverity = false)
    {
        def = h.def;
        part = h.Part;
        if (shouldRecordSeverity)
        {
            severity = h.Severity;
        }

        if (!CompatibilityTracker.PsychicAwakeningActive)
        {
            return;
        }

        //check if this is a Psychic Awakened hediff
        if (!PsychicAwakeningCompat.HediffPsychicAwakenedType.IsInstanceOfType(h))
        {
            return;
        }

        //get the value of the 'powersKnown' field from the Hediff
        var powersValue = PsychicAwakeningCompat.powersKnownField.GetValue(h);

        if (powersValue != null)
        {
            if (powersValue is IEnumerable value)
            {
                psychicAwakeningPowersKnownDefNames = [];

                //loop through each object in the 'powersKnown' List
                foreach (var o in value)
                {
                    //PsychicPowerDef is derived from class Def, so this cast should work
                    if (o is not Def theDef)
                    {
                        continue;
                    }

                    QEEMod.TryLog(
                        $"Adding PsychicPower {theDef.defName} to psychicAwakeningPowersKnownDefNames");
                    psychicAwakeningPowersKnownDefNames.Add(theDef.defName);
                }
            }
            else
            {
                QEEMod.TryLog("'powersKnown' field from Psychic hediff is not System.Collections.IEnumerable");
            }
        }
        else
        {
            QEEMod.TryLog("Unable to retrieve value of 'powersKnown' field from Psychic hediff");
        }
    }

    #endregion
}