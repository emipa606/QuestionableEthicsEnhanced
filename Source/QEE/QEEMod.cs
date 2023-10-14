using Mlie;
using UnityEngine;
using Verse;

namespace QEthics;

public class QEEMod : Mod
{
    private static string currentVersion;

    /// <summary>
    ///     A mandatory constructor which resolves the reference to our settings.
    /// </summary>
    /// <param name="content"></param>
    public QEEMod(ModContentPack content) : base(content)
    {
        currentVersion =
            VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
        QEESettings.instance = GetSettings<QEESettings>();
    }

    /// <summary>
    ///     The (optional) GUI part to set your settings.
    /// </summary>
    /// <param name="inRect">A Unity Rect with the size of the settings window.</param>
    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listingStandard = new Listing_Standard();
        listingStandard.Begin(inRect);
        listingStandard.SliderLabeled("QE_OrganGrowthDuration".Translate(),
            ref QEESettings.instance.organGrowthRateFloat, QEESettings.instance.organGrowthRateFloat.ToString("0.00"),
            0.00f, 4.0f, "QE_OrganGrowthDurationTooltip".Translate());
        listingStandard.SliderLabeled("QE_OrganIngredientMult".Translate(),
            ref QEESettings.instance.organTotalResourcesFloat,
            QEESettings.instance.organTotalResourcesFloat.ToString("0.00"), 0.00f, 4.0f,
            "QE_OrganIngredientMultTooltip".Translate());
        listingStandard.SliderLabeled("QE_CloneGrowthDuration".Translate(),
            ref QEESettings.instance.cloneGrowthRateFloat, QEESettings.instance.cloneGrowthRateFloat.ToString("0.00"),
            0.00f, 4.0f, "QE_CloneGrowthDurationTooltip".Translate());
        listingStandard.SliderLabeled("QE_MaxCloningTime".Translate(), ref QEESettings.instance.maxCloningTimeDays,
            QEESettings.instance.maxCloningTimeDays.ToString(), 1, 300,
            "QE_MaxCloningTimeTooltip".Translate());
        listingStandard.SliderLabeled("QE_CloneIngredientMult".Translate(),
            ref QEESettings.instance.cloneTotalResourcesFloat,
            QEESettings.instance.cloneTotalResourcesFloat.ToString("0.00"), 0.00f, 4.0f,
            "QE_CloneIngredientMultTooltip".Translate());
        listingStandard.SliderLabeled("QE_IngredientCheckInterval".Translate(),
            ref QEESettings.instance.ingredientCheckIntervalSeconds,
            QEESettings.instance.ingredientCheckIntervalSeconds.ToString(), 1.00f, 20.0f,
            "QE_IngredientCheckIntervalDescription".Translate());
        listingStandard.SliderLabeled("QE_VatMaintTime".Translate(), ref QEESettings.instance.maintRateFloat,
            QEESettings.instance.maintRateFloat.ToString("0.00"), 0.01f, 4.0f, "QE_VatMaintTimeTooltip".Translate());
        listingStandard.SliderLabeled("QE_MaintenanceWorkThreshold".Translate(),
            ref QEESettings.instance.maintWorkThresholdFloat,
            QEESettings.instance.maintWorkThresholdFloat.ToStringPercent(), 0.00f, 1.0f,
            "QE_MaintenanceWorkThresholdTooltip".Translate());
        listingStandard.CheckboxLabeled("QE_UseFillingGraphic".Translate(),
            ref QEESettings.instance.useFillingGraphic, "QE_UseFillingGraphicTooltip".Translate());
        listingStandard.CheckboxLabeled("QE_GiveCloneNegativeThought".Translate(),
            ref QEESettings.instance.giveCloneNegativeThought, "QE_GiveCloneNegativeThoughtTooltip".Translate());
        listingStandard.CheckboxLabeled("QE_BrainTemplatingRequiresClone".Translate(),
            ref QEESettings.instance.brainTemplatingRequiresClone,
            "QE_BrainTemplatingRequiresCloneTooltip".Translate());
        listingStandard.CheckboxLabeled("QE_NeuralDisrupt".Translate(),
            ref QEESettings.instance.neuralDisrupt,
            "QE_NeuralDisruptTooltip".Translate());
        listingStandard.CheckboxLabeled("QE_OldCloningRender".Translate(), ref QEESettings.instance.oldCloningRender,
            "QE_OldCloningRenderTooltip".Translate());

        //if ideology is installed, show those mod settings
        if (ModLister.IdeologyInstalled)
        {
            listingStandard.CheckboxLabeled("QE_DoIdeologyFeatures".Translate(),
                ref QEESettings.instance.doIdeologyFeatures,
                "QE_DoIdeologyFeaturesTooltip".Translate());
        }

        //enables debug logging
        listingStandard.CheckboxLabeled("QE_DebugLogging".Translate(), ref QEESettings.instance.debugLogging,
            "QE_DebugLoggingTooltip".Translate());

        if (currentVersion != null)
        {
            listingStandard.Gap();
            GUI.contentColor = Color.gray;
            listingStandard.Label("QE_CurrentModVersion".Translate(currentVersion));
            GUI.contentColor = Color.white;
        }

        listingStandard.End();
        base.DoSettingsWindowContents(inRect);
    }

    public static void TryLog(string message)
    {
        if (QEESettings.instance.debugLogging)
        {
            Log.Message($"[QEE]: {message}");
        }
    }

    /// <summary>
    ///     Override SettingsCategory to show up in the list of settings.
    ///     Using .Translate() is optional, but does allow for localisation.
    /// </summary>
    /// <returns>The (translated) mod name.</returns>
    public override string SettingsCategory()
    {
        return "Questionable Ethics Enhanced";
    }
}