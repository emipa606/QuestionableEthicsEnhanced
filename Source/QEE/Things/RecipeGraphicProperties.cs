using Verse;

namespace QEthics;

public class RecipeGraphicProperties : DefModExtension
{
    /// <summary>
    ///     Percentage to scale the original graphic
    /// </summary>
    public readonly float scale = 1.0f;

    /// <summary>
    ///     Graphic to draw while its being crafted.
    /// </summary>
    public GraphicData productGraphic;
}