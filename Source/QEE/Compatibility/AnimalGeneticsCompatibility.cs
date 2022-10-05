using AnimalGenetics;
using Verse;

namespace QEthics;

public static class AnimalGeneticsCompatibility
{
    public static void GetFieldsFromAnimalGeneticsComps(Pawn pawn, GenomeSequence genomeSequence)
    {
        if (pawn.TryGetComp<BaseGeneticInformation>() is { } animalGeneticInformation)
        {
            genomeSequence.animalGeneticInformation = animalGeneticInformation.GeneticInformation;
        }
    }

    public static void SetFieldsToAnimalGeneticsComps(Pawn pawn, GenomeSequence genomeSequence)
    {
        if (pawn.TryGetComp<BaseGeneticInformation>() is { } animalGeneticInformation)
        {
            animalGeneticInformation.GeneticInformation = (GeneticInformation)genomeSequence.animalGeneticInformation;
        }
    }
}