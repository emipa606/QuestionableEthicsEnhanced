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

    public new void ExposeData()
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
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + degree;
            hash = (hash * 31) + (def != null ? def.GetHashCode() : 0);
            return hash;
        }
    }
}