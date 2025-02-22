using System;
using Verse;

namespace QEthics;

public class ComparableExpertiseRecord : IExposable, IEquatable<ComparableExpertiseRecord>
{
    public string defName;
    public float totalXp;

    public bool Equals(ComparableExpertiseRecord other)
    {
        return other != null && defName == other.defName && totalXp == other.totalXp;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref defName, "defName");
        Scribe_Values.Look(ref totalXp, "totalXp");
    }

    public override bool Equals(object obj)
    {
        return obj is ComparableExpertiseRecord record && Equals(record);
    }

    public override int GetHashCode()
    {
        return (defName.GetHashCode() * 17) + totalXp.GetHashCode();
    }

    public override string ToString()
    {
        return "    " + VanillaSkillsExpandedCompatibility.ExpertiseDefLabelCap(defName) + ": " + totalXp.ToString() +
               " xp";
    }
}