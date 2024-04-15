using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace QEthics;

/// <summary>
///     Properties specific to the VatGrower.
/// </summary>
public class VatGrowerProperties : GrowerProperties
{
    /// <summary>
    ///     Scale for the product.
    /// </summary>
    public readonly float productScaleModifier = 1f;

    /// <summary>
    ///     Graphic for the base.
    /// </summary>
    public GraphicData bottomGraphic;

    /// <summary>
    ///     Graphic for the glow.
    /// </summary>
    public List<GraphicData> glowGraphic;

    public List<GraphicData> glowGraphicUnpowered;

    /// <summary>
    ///     Offset for where the product is rendered.
    /// </summary>
    public Vector3 productOffset = new Vector3();

    /// <summary>
    ///     Graphic for the top detail.
    /// </summary>
    public GraphicData topDetailGraphic;

    /// <summary>
    ///     Graphic for the "lid".
    /// </summary>
    public GraphicData topGraphic;

    public override IEnumerable<string> ConfigErrors()
    {
        ResolveAll();
        return base.ConfigErrors();
    }

    public void ResolveAll()
    {
        topGraphic?.ResolveReferencesSpecial();
        var transparentShader = DefDatabase<ShaderTypeDef>.GetNamedSilentFail("Transparent");
        bottomGraphic?.ResolveReferencesSpecial();
        glowGraphicUnpowered = [];
        foreach (var graphicData in glowGraphic)
        {
            if (graphicData == null)
            {
                continue;
            }

            var unpoweredVersion = new GraphicData();
            unpoweredVersion.CopyFrom(graphicData);
            unpoweredVersion.shaderType = transparentShader;
            unpoweredVersion.ResolveReferencesSpecial();
            glowGraphicUnpowered.Add(unpoweredVersion);
            graphicData.ResolveReferencesSpecial();
        }
    }
}