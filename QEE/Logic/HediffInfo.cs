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
                part == other.part))
            {
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return def.defName.GetHashCode() * 19 + part.GetHashCode();
        }
    }
}
