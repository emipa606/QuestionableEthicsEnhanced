using System;
using System.Text;
using RimWorld;
using Verse;

namespace QEthics;

public class ComparableSkillRecord : IExposable, IEquatable<ComparableSkillRecord>
{
    public SkillDef def;
    public int level;
    public Passion passion;

    public ComparableSkillRecord()
    {
    }

    public ComparableSkillRecord(SkillRecord record)
    {
        def = record.def;
        level = record.levelInt;
        passion = record.passion;
    }

    public bool Equals(ComparableSkillRecord other)
    {
        if (other == null)
        {
            return false;
        }

        //compare the defName instead of the SkillDef, because SkillDef doesn't implement IEquatable
        if (def.defName != null &&
            def.defName == other.def.defName &&
            level == other.level &&
            passion == other.passion)
        {
            return true;
        }

        return false;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_Values.Look(ref level, "level");
        Scribe_Values.Look(ref passion, "passion");
    }

    public override bool Equals(object obj)
    {
        if (obj is ComparableSkillRecord template)
        {
            return Equals(template);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return (def.defName.GetHashCode() * 17) + level.GetHashCode() + passion.GetHashCode();
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append("    " + def.LabelCap + ": " + level);

        switch (passion)
        {
            case Passion.None:
                builder.Append("");
                break;
            case Passion.Minor:
                builder.Append("*");
                break;
            case Passion.Major:
                builder.Append("**");
                break;
            default:
                builder.Append("");
                break;
        }

        return builder.ToString().TrimEndNewlines();
    }
}