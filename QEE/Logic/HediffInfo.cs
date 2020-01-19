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
    public class HediffInfo : IExposable
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
    }
}
