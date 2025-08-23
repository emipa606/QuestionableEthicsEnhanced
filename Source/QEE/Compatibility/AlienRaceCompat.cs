using Verse;
using static AlienRace.AlienPartGenerator;

namespace QEthics;

public static class AlienRaceCompat
{
    public static void GetFieldsFromAlienComp(Pawn pawn, GenomeSequence genomeSequence)
    {
        var alienComp = pawn.TryGetComp<AlienComp>();
        if (alienComp == null)
        {
            return;
        }

        genomeSequence.isAlien = true;
        genomeSequence.skinColor = alienComp.GetChannel("skin").first;
        genomeSequence.skinColorSecond = alienComp.GetChannel("skin").second;
        genomeSequence.hairColorSecond = alienComp.GetChannel("hair").second;

        if (alienComp.addonVariants is { Count: > 0 })
        {
            genomeSequence.addonVariants = alienComp.addonVariants;
        }
    }

    public static void SetFieldsToAlienComp(Pawn pawn, GenomeSequence genomeSequence)
    {
        var alienComp = pawn.TryGetComp<AlienComp>();
        if (alienComp == null)
        {
            return;
        }

        alienComp.GetChannel("skin").first = genomeSequence.skinColor;
        alienComp.GetChannel("skin").second = genomeSequence.skinColorSecond;
        alienComp.GetChannel("hair").second = genomeSequence.hairColorSecond;

        if (genomeSequence.addonVariants.Count > 0)
        {
            alienComp.addonVariants = genomeSequence.addonVariants;
        }
    }
}