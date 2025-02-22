using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QEthics
{
    public class ComparableExpertiseRecord : IExposable, IEquatable<ComparableExpertiseRecord>
    {
        public string defName;
        public float totalXp;

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is ComparableExpertiseRecord))
                return false;
            return Equals((ComparableExpertiseRecord)obj);
        }

        public override int GetHashCode()
        {
            return (defName.GetHashCode() * 17) + totalXp.GetHashCode();
        }

        public bool Equals(ComparableExpertiseRecord other)
        {
            return defName == other.defName && totalXp == other.totalXp;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref totalXp, "totalXp");
        }

        public override string ToString()
        {
            return "    " + VanillaSkillsExpandedCompatibility.ExpertiseDefLabelCap(defName) + ": " + totalXp.ToString() + " xp";
        }
    }
}
