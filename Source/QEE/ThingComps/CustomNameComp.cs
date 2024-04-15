using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace QEthics;

public class CustomNameComp : ThingComp
{
    public string customName;

    public override string TransformLabel(string label)
    {
        return customName != null ? $"\"{customName}\" {label}" : base.TransformLabel(label);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        yield return new Command_Action
        {
            defaultLabel = "QE_CustomNameCompGizmoLabel".Translate(),
            defaultDesc = "QE_CustomNameCompGizmoDescription".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Buttons/Rename"),
            order = 100,
            action = delegate
            {
                var dialog = new Dialog_RenameCustomNameComp
                {
                    optionalTitle = "QE_CustomNameCompDialogTitle".Translate(parent.LabelCapNoCount),
                    nameComp = this
                };
                Find.WindowStack.Add(dialog);
            }
        };
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref customName, "customName");
    }

    public override bool AllowStackWith(Thing other)
    {
        return other.TryGetComp<CustomNameComp>() is { } otherComp && otherComp.customName == customName;
    }

    public override void PostSplitOff(Thing piece)
    {
        ((ThingWithComps)piece).GetComp<CustomNameComp>().customName = customName;
    }
}