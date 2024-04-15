using UnityEngine;
using Verse;

namespace QEthics;

//thanks to AlexTD for the below code!
internal static class SettingsHelper
{
    public static void SliderLabeled(this Listing_Standard ls, string label, ref int val, string format, float min = 0f,
        float max = 100f, string tooltip = null)
    {
        float fVal = val;
        ls.SliderLabeled(label, ref fVal, format, min, max, tooltip);
        val = (int)fVal;
    }

    public static void SliderLabeled(this Listing_Standard ls, string label, ref float val, string format,
        float min = 0f, float max = 1f, string tooltip = null)
    {
        var fullRect = ls.GetRect(Text.LineHeight);
        var descLabelRect = fullRect.LeftPart(.50f).Rounded();
        var sliderRect = fullRect.RightPart(.50f).Rounded().LeftPart(.90f).Rounded();
        var percentLabelRect = fullRect.RightPart(.10f).Rounded();

        var anchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(descLabelRect, label);

        var result = Widgets.HorizontalSlider(sliderRect, val, min, max, true);
        val = result;
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(percentLabelRect, string.Format(format, val));
        if (!tooltip.NullOrEmpty())
        {
            TooltipHandler.TipRegion(fullRect, tooltip);
        }

        Text.Anchor = anchor;
        ls.Gap(ls.verticalSpacing);
    }
}