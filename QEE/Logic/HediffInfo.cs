using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace QEthics
{
    /// <summary>
    /// Used to save hediff information of a pawn for use in cloning. An instance of this class is saved in the Genome Template or Brain Template
    /// and the hediffs within are then applied to the clone.
    /// </summary>
    public class HediffInfo : IExposable, IEquatable<HediffInfo>
    {
        public HediffDef def;
        public BodyPartRecord part;

        #region Constructors

        public HediffInfo()
        {
        }

        public HediffInfo(Hediff h)
        {
            def = h.def;
            part = h.Part;
        }
        #endregion

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "hediffDef");
            Scribe_BodyParts.Look(ref part, "bodyPart");
        }

        public bool Equals(HediffInfo other)
        {
            if (other == null) return false;

            if ((def?.defName == null && other.def?.defName == null ||
                def?.defName == other.def?.defName) &&
                
                (part == null && other.part == null ||
                part.LabelCap == other.part.LabelCap))
            {
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hash = 19;
            hash = hash * 23 + (def?.GetHashCode() ?? 0);
            //the LabelCap field should be unique enough for the hash
            hash = hash * 23 + (part?.LabelCap.GetHashCode() ?? 0);
            return hash;
        }

        public static void GenerateDescForHediffList(ref StringBuilder builder, List<HediffInfo> hediffs)
        {
            var hediffsNonNull = hediffs?.Where(h => h.def != null);
            
            if (hediffsNonNull != null)
            {
                if (hediffsNonNull.Any())
                {
                    builder.AppendLine("QE_GenomeSequencerDescription_Hediffs".Translate());

                    //sort hediffs in alphabetical order
                    var ordered = hediffsNonNull.OrderBy(h => h.def.LabelCap);

                    //loop through hediffs and add line to StringBuilder for each
                    foreach (HediffInfo h in ordered)
                    {
                        if (h.part != null)
                        {
                            builder.AppendLine("    " + h.def.LabelCap + " [" + h.part.LabelCap + "]");
                        }
                        else
                        {
                            builder.AppendLine("    " + h.def.LabelCap);
                        }
                    }
                }
            }
          
        }
    }
}
