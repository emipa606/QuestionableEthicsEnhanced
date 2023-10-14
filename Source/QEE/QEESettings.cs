using Verse;

namespace QEthics;

public class QEESettings : ModSettings
{
    public static QEESettings instance;
    public bool brainTemplatingRequiresClone = true;
    public float cloneGrowthRateFloat = 1.0f;
    public float cloneTotalResourcesFloat = 1.0f;
    public bool debugLogging;
    public bool doIdeologyFeatures = true;
    public bool giveCloneNegativeThought = true;
    public int ingredientCheckIntervalSeconds = 3;
    public float maintRateFloat = 1.0f;
    public float maintWorkThresholdFloat = 0.40f;
    public int maxCloningTimeDays = 60;
    public bool neuralDisrupt = true;
    public bool oldCloningRender;
    public float organGrowthRateFloat = 1.0f;
    public float organTotalResourcesFloat = 1.0f;
    public bool useFillingGraphic = true;

    public QEESettings()
    {
        instance = this;
    }

    /// <summary>
    ///     The part that writes our settings to file. Note that saving is by ref.
    /// </summary>
    public override void ExposeData()
    {
        Scribe_Values.Look(ref useFillingGraphic, "useFillingGraphic", true);
        Scribe_Values.Look(ref maintRateFloat, "maintRateFloat", 1.0f);
        Scribe_Values.Look(ref organGrowthRateFloat, "organGrowthRateFloat", 1.0f);
        Scribe_Values.Look(ref cloneGrowthRateFloat, "cloneGrowthRateFloat", 1.0f);
        Scribe_Values.Look(ref organTotalResourcesFloat, "organTotalResourcesFloat", 1.0f);
        Scribe_Values.Look(ref cloneTotalResourcesFloat, "cloneTotalResourcesFloat", 1.0f);
        Scribe_Values.Look(ref debugLogging, "debugLogging");
        Scribe_Values.Look(ref maintWorkThresholdFloat, "maintWorkGiverThresholdFloat", 0.40f);
        Scribe_Values.Look(ref giveCloneNegativeThought, "giveCloneNegativeThought", true);
        Scribe_Values.Look(ref maxCloningTimeDays, "maxCloningTimeDays", 60);
        Scribe_Values.Look(ref ingredientCheckIntervalSeconds, "ingredientCheckIntervalSeconds", 3);
        Scribe_Values.Look(ref brainTemplatingRequiresClone, "brainTemplatingRequiresClone", true);
        Scribe_Values.Look(ref doIdeologyFeatures, "doIdeologyFeatures", true);
        Scribe_Values.Look(ref oldCloningRender, "oldCloningRender");
        Scribe_Values.Look(ref neuralDisrupt, "neuralDisrupt", true);
        base.ExposeData();
    }
}