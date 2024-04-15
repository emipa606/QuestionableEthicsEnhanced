using RimWorld;
using Verse;

namespace QEthics;

/// <summary>
///     Makes TraitEntry exposable.
/// </summary>
public class ExposedTraitEntry : Trait, IExposable
{
    public ExposedTraitEntry()
    {
    }

    public ExposedTraitEntry(Trait trait)
    {
        def = trait.def;
        degree = trait.Degree;
    }

    public ExposedTraitEntry(ExposedTraitEntry traitEntry)
    {
        def = traitEntry.def;
        degree = traitEntry.degree;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_Values.Look(ref degree, "degree");
    }

    public override bool Equals(object obj)
    {
        if (this == obj)
        {
            return true;
        }

        if (obj is not ExposedTraitEntry other)
        {
            return false;
        }

        return def == other.def &&
               degree == other.degree;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode() + (degree.GetHashCode() & 0xFFF0000) + ((def?.GetHashCode() ?? 0) & 0x000FFFF);
    }
}