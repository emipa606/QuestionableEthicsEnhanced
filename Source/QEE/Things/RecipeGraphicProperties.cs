using Verse;

namespace QEthics;

public class RecipeGraphicProperties : DefModExtension
{
    /// <summary>
    ///     Graphic to draw while its being crafted.
    /// </summary>
    public GraphicData productGraphic;

    /// <summary>
    ///     Percentage to scale the original graphic
    /// </summary>
    public float scale = 1.0f;
}