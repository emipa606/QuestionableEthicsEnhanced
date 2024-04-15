using FacialAnimation;
using UnityEngine;
using Verse;
using HeadTypeDef = FacialAnimation.HeadTypeDef;

namespace QEthics;

public static class FacialAnimationCompatibility
{
    public static void GetFieldsFromFacialAnimationComps(Pawn pawn, GenomeSequence genomeSequence)
    {
        if (pawn.TryGetComp<HeadControllerComp>() is { } headControllerComp)
        {
            genomeSequence.headType = ((Def)CompatibilityTracker.HeadTypeField.GetValue(headControllerComp)).defName;
        }

        if (pawn.TryGetComp<EyeballControllerComp>() is { } eyeballControllerComp)
        {
            genomeSequence.eyeColor = (Color)CompatibilityTracker.EyeColorField.GetValue(eyeballControllerComp);
            genomeSequence.eyeType = ((Def)CompatibilityTracker.EyeTypeField.GetValue(eyeballControllerComp)).defName;
        }

        if (pawn.TryGetComp<LidControllerComp>() is { } lidControllerComp)
        {
            genomeSequence.lidType = ((Def)CompatibilityTracker.LidTypeField.GetValue(lidControllerComp)).defName;
        }

        if (pawn.TryGetComp<BrowControllerComp>() is { } browControllerComp)
        {
            genomeSequence.browType = ((Def)CompatibilityTracker.BrowTypeField.GetValue(browControllerComp)).defName;
        }

        if (pawn.TryGetComp<MouthControllerComp>() is { } mouthControllerComp)
        {
            genomeSequence.mouthType = ((Def)CompatibilityTracker.MouthTypeField.GetValue(mouthControllerComp)).defName;
        }

        if (pawn.TryGetComp<SkinControllerComp>() is { } skinControllerComp)
        {
            genomeSequence.skinType = ((Def)CompatibilityTracker.SkinTypeField.GetValue(skinControllerComp)).defName;
        }
    }

    public static void SetFieldsToFacialAnimationComps(Pawn pawn, GenomeSequence genomeSequence)
    {
        if (pawn.TryGetComp<HeadControllerComp>() is { } headControllerComp)
        {
            CompatibilityTracker.HeadTypeField.SetValue(headControllerComp,
                DefDatabase<HeadTypeDef>.GetNamedSilentFail(genomeSequence.headType));
        }

        if (pawn.TryGetComp<EyeballControllerComp>() is { } eyeballControllerComp)
        {
            CompatibilityTracker.EyeColorField.SetValue(eyeballControllerComp, genomeSequence.eyeColor);
            CompatibilityTracker.EyeTypeField.SetValue(eyeballControllerComp,
                DefDatabase<EyeballTypeDef>.GetNamedSilentFail(genomeSequence.eyeType));
        }

        if (pawn.TryGetComp<LidControllerComp>() is { } lidControllerComp)
        {
            CompatibilityTracker.LidTypeField.SetValue(lidControllerComp,
                DefDatabase<LidTypeDef>.GetNamedSilentFail(genomeSequence.lidType));
        }

        if (pawn.TryGetComp<BrowControllerComp>() is { } browControllerComp)
        {
            CompatibilityTracker.BrowTypeField.SetValue(browControllerComp,
                DefDatabase<BrowTypeDef>.GetNamedSilentFail(genomeSequence.browType));
        }

        if (pawn.TryGetComp<MouthControllerComp>() is { } mouthControllerComp)
        {
            CompatibilityTracker.MouthTypeField.SetValue(mouthControllerComp,
                DefDatabase<MouthTypeDef>.GetNamedSilentFail(genomeSequence.mouthType));
        }

        if (pawn.TryGetComp<SkinControllerComp>() is { } skinControllerComp)
        {
            CompatibilityTracker.SkinTypeField.SetValue(skinControllerComp,
                DefDatabase<SkinTypeDef>.GetNamedSilentFail(genomeSequence.skinType));
        }
    }
}