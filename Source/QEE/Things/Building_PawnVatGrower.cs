using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace QEthics;

/// <summary>
///     THIS IS CLASS WILL BE DEPRECATED SOON. Do not use in new development work.
/// </summary>
public class Building_PawnVatGrower : Building_GrowerBase, IMaintainableGrower
{
    public static SimpleCurve cleanlinessCurve = [];

    /// <summary>
    ///     From 0.0 to 1.0. If the maintenance is below 50% there is a chance for failure.
    /// </summary>
    public float doctorMaintenance;

    private LifeStageAge lastLifestageAge;

    public Pawn pawnBeingGrown;
    public PawnKindDef pawnKindToGrow;
    private Material renderMaterial;

    private RenderTexture renderTexture;

    /// <summary>
    ///     From 0.0 to 1.0. If the maintenance is below 50% there is a chance for failure.
    /// </summary>
    public float scientistMaintenance;

    private VatGrowerProperties vatGrowerPropsInt;

    static Building_PawnVatGrower()
    {
        cleanlinessCurve.Add(-5.0f, 5.00f);
        cleanlinessCurve.Add(-2.0f, 1.75f);
        cleanlinessCurve.Add(0.0f, 1.0f);
        cleanlinessCurve.Add(0.4f, 0.35f);
        cleanlinessCurve.Add(2.0f, 0.1f);
    }

    /// <summary>
    ///     Formula: (2 days + 2 ^ baseBodySize) * TicksPerDay * ModSetting multiplier
    ///     OR Max Cloning Time in days, whichever is less.
    /// </summary>
    public override int TicksNeededToCraft => (int)Math.Min((2 + Math.Pow(2.0f, pawnKindToGrow.RaceProps.baseBodySize) +
                                                             pawnKindToGrow.RaceProps.lifeStageAges.Last().minAge) *
                                                            (float)GenDate.TicksPerDay *
                                                            QEESettings.instance.cloneGrowthRateFloat,
        QEESettings.instance.maxCloningTimeDays * (float)GenDate.TicksPerDay);

    public VatGrowerProperties VatGrowerProps
    {
        get
        {
            if (vatGrowerPropsInt != null)
            {
                return vatGrowerPropsInt;
            }

            vatGrowerPropsInt = def.GetModExtension<VatGrowerProperties>();

            //Fallback; Is defaults.
            if (vatGrowerPropsInt == null)
            {
                vatGrowerPropsInt = new VatGrowerProperties();
            }

            return vatGrowerPropsInt;
        }
    }

    public float ScientistMaintenance
    {
        get => scientistMaintenance;
        set => scientistMaintenance = value;
    }

    public float DoctorMaintenance
    {
        get => doctorMaintenance;
        set => doctorMaintenance = value;
    }

    public float RoomCleanliness
    {
        get
        {
            var room = this.GetRoom(RegionType.Set_Passable);
            return room?.GetStat(RoomStatDefOf.Cleanliness) ?? 0f;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Defs.Look(ref pawnKindToGrow, "pawnKindToGrow");
        Scribe_Values.Look(ref scientistMaintenance, "scientistMaintenance");
        Scribe_Values.Look(ref doctorMaintenance, "doctorMaintenance");
        Scribe_Deep.Look(ref pawnBeingGrown, "pawnBeingGrown");
    }

    /// <summary>
    ///     Begin the crafting process. Called when the player selects a genome template in the 'Start Growing' gizmo .
    /// </summary>
    /// <param name="genome"></param>
    public void StartCrafting(GenomeSequence genome)
    {
        //Setup recipe order
        orderProcessor.Reset();
        orderProcessor.desiredIngredients.Add(new ThingOrderRequest(genome));
        IngredientUtility.FillOrderProcessorFromPawnKindDef(orderProcessor, genome.pawnKindDef);
        orderProcessor.Notify_ContentsChanged();
        craftingProgress = 0;

        //Reset stuff
        pawnBeingGrown = null;

        pawnKindToGrow = genome.pawnKindDef;
        status = CrafterStatus.Filling;
    }

    public override void Notify_CraftingStarted()
    {
        //Remove all except for the GenomeSequence.
        orderProcessor.Reset();

        //innerContainer.RemoveAll(thing => !(thing is GenomeSequence));
        //Initialize maintenance
        scientistMaintenance = 1f;
        doctorMaintenance = 1f;

        TryMakeClone();
    }

    public bool TryMakeClone()
    {
        if (!innerContainer.Any(thing => thing is GenomeSequence))
        {
            return false;
        }

        if (innerContainer.FirstOrDefault(thing => thing is GenomeSequence) is not GenomeSequence genome)
        {
            return false;
        }

        pawnBeingGrown = GenomeUtility.MakePawnFromGenomeSequence(genome, this);

        if (pawnBeingGrown.Rotation != Rotation.Opposite)
        {
            pawnBeingGrown.Rotation = Rotation.Opposite;
        }

        return true;
    }

    public override void Tick_Crafting()
    {
        base.Tick_Crafting();

        //attempt to re-make the clone, if it somehow became null
        if (pawnBeingGrown == null && TryMakeClone() == false)
        {
            Messages.Message("QE_CloningPawnNullMessage".Translate(), new LookTargets(this),
                MessageTypeDefOf.NegativeEvent);
            StopCrafting();
        }
        else
        {
            if (pawnBeingGrown != null && pawnBeingGrown.Rotation != Rotation.Opposite)
            {
                pawnBeingGrown.Rotation = Rotation.Opposite;
            }

            //QEEMod.TryLog("CraftingProgressPercent: " + CraftingProgressPercent);

            var minAge = pawnKindToGrow.RaceProps.lifeStageAges.Last().minAge;
            var calculatedAgeTicks = (long)(minAge * CraftingProgressPercent * GenDate.TicksPerYear);
            if (pawnBeingGrown != null)
            {
                pawnBeingGrown.ageTracker.AgeBiologicalTicks = calculatedAgeTicks;
                pawnBeingGrown.ageTracker.AgeChronologicalTicks = calculatedAgeTicks;
                if (lastLifestageAge != pawnBeingGrown.ageTracker.CurLifeStageRace)
                {
                    QEEMod.TryLog($"Drawing updated texture for clone {pawnBeingGrown.LabelShort}");
                    PortraitsCache.SetDirty(pawnBeingGrown);
                    PortraitsCache.PortraitsCacheUpdate();
                    lastLifestageAge = pawnBeingGrown.ageTracker.CurLifeStageRace;
                    renderTexture = null;
                }
            }

            //QEEMod.TryLog("BioAge of clone " + pawnBeingGrown.LabelShort + ": " + 
            //    pawnBeingGrown.ageTracker.AgeBiologicalTicks);

            //Deduct maintenance, fail if any of them go below 0%.
            var powerModifier = 1f;
            if (PowerTrader is { PowerOn: false })
            {
                powerModifier = 15f;
            }

            var cleanlinessModifer = cleanlinessCurve.Evaluate(RoomCleanliness);
            var decayRate = .0012f * cleanlinessModifer * powerModifier / QEESettings.instance.maintRateFloat;

            scientistMaintenance -= decayRate;
            doctorMaintenance -= decayRate;

            if (!(scientistMaintenance < 0f) && !(doctorMaintenance < 0f))
            {
                return;
            }
            //Fail the cloning process and return only the genome template

            if (innerContainer.FirstOrDefault(thing => thing is GenomeSequence) is GenomeSequence
                {
                    sourceName: not null
                } genome)
            {
                Messages.Message("QE_CloningMaintFailMessage".Translate(genome.sourceName.Named("SOURCEPAWNNAME")),
                    new LookTargets(this), MessageTypeDefOf.NegativeEvent);
            }
            else
            {
                Messages.Message("QE_CloningMaintFailFallbackMessage".Translate(), new LookTargets(this),
                    MessageTypeDefOf.NegativeEvent);
            }

            StopCrafting(false);
        }
    }

    public override void Notify_CraftingFinished()
    {
        //attempt to re-make the clone, if it somehow became null
        if (pawnBeingGrown == null && TryMakeClone() == false)
        {
            Messages.Message("QE_CloningPawnNullMessage".Translate(), new LookTargets(this),
                MessageTypeDefOf.NegativeEvent);
            StopCrafting();
        }
        else
        {
            Messages.Message("QE_MessageGrowingDone".Translate(pawnBeingGrown?.LabelCap), new LookTargets(this),
                MessageTypeDefOf.PositiveEvent);

            if (pawnBeingGrown != null && pawnBeingGrown.RaceProps.Humanlike && pawnBeingGrown.story != null)
            {
                //HairDef hairDef = PawnHairChooser.RandomHairDefFor(pawnBeingGrown, Faction.def);
                //pawnBeingGrown.story.hairDef = hairDef;
                pawnBeingGrown.Drawer.renderer.graphics.ResolveAllGraphics();
                PortraitsCache.SetDirty(pawnBeingGrown);
                PortraitsCache.PortraitsCacheUpdate();
            }

            //set minimum age manually as an extra check against negative ages
            var minAge = pawnKindToGrow.RaceProps.lifeStageAges.Last().minAge;
            var calculatedAgeTicks = (long)(minAge * GenDate.TicksPerYear);
            if (pawnBeingGrown == null)
            {
                return;
            }

            pawnBeingGrown.ageTracker.AgeBiologicalTicks = calculatedAgeTicks;
            pawnBeingGrown.ageTracker.AgeChronologicalTicks = calculatedAgeTicks;
        }
    }

    /// <summary>
    ///     Stops the cloning process, resets the vat, and refunds any ingredients used. Will always return genome template.
    ///     This function is called if cloning fails, succeeds, or is manually stopped via the Stop gizmo.
    /// </summary>
    /// <param name="keepIngredients"></param>
    public void StopCrafting(bool keepIngredients = true)
    {
        QEEMod.TryLog($"Stopping cloning process. Keep Ingredients: {keepIngredients}");

        craftingProgress = 0;
        orderProcessor.Reset();
        renderTexture = null;
        renderMaterial = null;
        lastLifestageAge = null;
        status = CrafterStatus.Idle;
        pawnKindToGrow = null;

        //destroy pawn if cloning process is not complete. pawnBeingGrown will be set to null in TryExtractProduct()
        if (pawnBeingGrown is { Destroyed: false })
        {
            pawnBeingGrown.Destroy();
        }

        pawnBeingGrown = null;

        if (innerContainer is { Count: > 0 })
        {
            QEEMod.TryLog($"Inner container count: {innerContainer.Count}");

            if (keepIngredients)
            {
                var unused = innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
                //QEEMod.TryLog("TryDropAll() success: " + wasSuccess);
            }

            else if (innerContainer.Any(thing => thing is GenomeSequence))
            {
                //QEEMod.TryLog("Dropping genome template only");
                //drop only the genome template
                innerContainer.TryDrop(innerContainer.FirstOrDefault(thing => thing is GenomeSequence), InteractionCell,
                    Map, ThingPlaceMode.Near, out var _);
            }
        }

        innerContainer.ClearAndDestroyContents();
    }

    /// <summary>
    ///     This function is called when a Pawn goes to extract the clone from the vat. Called from the
    ///     JobDriver_ExtractGrowerProduct class.
    /// </summary>
    /// <param name="actor"></param>
    /// <returns></returns>
    public override bool TryExtractProduct(Pawn actor)
    {
        var wasSuccess = true;

        if (pawnBeingGrown == null && TryMakeClone() == false)
        {
            Messages.Message("QE_CloningPawnNullMessage".Translate(), new LookTargets(this),
                MessageTypeDefOf.NegativeEvent);
            wasSuccess = false;
            StopCrafting();
        }
        else
        {
            var tempPawn = pawnBeingGrown;
            pawnBeingGrown = null;

            if (tempPawn != null && tempPawn.RaceProps.Humanlike)
            {
                Find.LetterStack.ReceiveLetter("QE_LetterHumanlikeGrownLabel".Translate(),
                    "QE_LetterHumanlikeGrownDescription".Translate(tempPawn.Named("PAWN")), LetterDefOf.PositiveEvent,
                    new LookTargets(tempPawn));
                TaleRecorder.RecordTale(QETaleDefOf.QE_Vatgrown, tempPawn);

                if (QEESettings.instance.giveCloneNegativeThought)
                {
                    tempPawn.needs?.mood?.thoughts?.memories?.TryGainMemory(QEThoughtDefOf.QE_VatGrownCloneConfusion);
                }

                //Adds history event used by precepts, done hear since its easier than patching it just for ideology
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(QEHistoryDefOf.PawnCloned));
            }

            //Place new clone on the vat's Interaction Cell
            GenPlace.TryPlaceThing(tempPawn, InteractionCell, Map, ThingPlaceMode.Near);

            //remove any ingredients used to create the Pawn
            innerContainer.RemoveAll(thing => thing is not GenomeSequence);
            StopCrafting(false);
        }

        return wasSuccess;
    }


    public override string TransformStatusLabel(string label)
    {
        string pawnLabel = pawnKindToGrow?.race.LabelCap ?? "QE_VatGrowerNoLivingBeing".Translate();

        if (status is CrafterStatus.Filling or CrafterStatus.Finished)
        {
            return $"{label} {pawnLabel.CapitalizeFirst()}";
        }

        if (status != CrafterStatus.Crafting)
        {
            return base.TransformStatusLabel(label);
        }

        var daysRemaining = TicksLeftToCraft.TicksToDays();
        if (daysRemaining > 1.0)
        {
            return $"{pawnLabel.CapitalizeFirst()} ({daysRemaining:0.0} " +
                   "QE_VatGrowerDaysRemaining".Translate() + ")";
        }

        return
            $" {pawnLabel.CapitalizeFirst()} ({TicksLeftToCraft / 2500.0f:0.0} " +
            "QE_VatGrowerHoursRemaining".Translate() + ")";
    }

    public override string GetInspectString()
    {
        if (ParentHolder is not Verse.Map)
        {
            return null;
        }

        var builder = new StringBuilder(base.GetInspectString());

        if (status != CrafterStatus.Crafting)
        {
            return builder.ToString().TrimEndNewlines();
        }

        builder.AppendLine();
        builder.AppendLine("QE_VatGrowerMaintenance".Translate($"{scientistMaintenance:0%}",
            $"{doctorMaintenance:0%}"));

        builder.AppendLine(
            "QE_VatGrowerCleanlinessMult".Translate(cleanlinessCurve.Evaluate(RoomCleanliness).ToString("0.00")));

        return builder.ToString().TrimEndNewlines();
    }

    public override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        //Draw bottom graphic
        var drawAltitude = drawLoc;
        VatGrowerProps.bottomGraphic?.Graphic.Draw(drawAltitude, Rotation, this);

        //Draw product
        drawAltitude += new Vector3(0f, 0.005f, 0f);
        if (status is CrafterStatus.Crafting or CrafterStatus.Finished && pawnBeingGrown != null)
        {
            //code has been commented out as a seemingly nonworking pawn renderer? Legacy switch has been added
            if (QEESettings.instance.oldCloningRender)
            {
                if (pawnBeingGrown.RaceProps.Humanlike)
                {
                    QEEMod.TryLog("Pawn being grown is humanoid");
                    if (renderTexture == null)
                    {
                        QEEMod.TryLog("Pawn being grown is nulltexture");
                        var size = 256;
                        var renderTextureTemp = new RenderTexture(size, size, 24);
                        //Find.PortraitRenderer.RenderPortrait(pawnBeingGrown, renderTextureTemp, default(Vector3), 1f);

                        renderTexture = renderTextureTemp;
                        var tempTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                        RenderTexture.active = renderTexture;
                        tempTexture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                        tempTexture.Apply();
                        RenderTexture.active = null;

                        var req2 = new MaterialRequest(tempTexture)
                        {
                            shader = ShaderDatabase.Mote
                        };
                        renderMaterial = MaterialPool.MatFrom(req2);

                        //QEEMod.TryLog("DrawAt: New render texture");
                    }

                    var scale = (0.2f + (CraftingProgressPercent * 0.8f)) * 1.75f;

                    var scaleVector = new Vector3(scale, 1f, scale);
                    var matrix = default(Matrix4x4);
                    matrix.SetTRS(drawAltitude + VatGrowerProps.productOffset, Quaternion.AngleAxis(0f, Vector3.up),
                        scaleVector);

                    Graphics.DrawMesh(MeshPool.plane10, matrix, renderMaterial, 0);
                    //pawnBeingGrown.DrawAt(drawAltitude);
                }
                else
                {
                    pawnBeingGrown.DrawAt(drawAltitude + VatGrowerProps.productOffset);
                }
            }
            else
            {
                pawnBeingGrown.DrawAt(drawAltitude + VatGrowerProps.productOffset);
            }
        }

        //Draw top graphic
        if (VatGrowerProps.topGraphic != null)
        {
            drawAltitude += new Vector3(0f, 0.020f, 0f);
            VatGrowerProps.topGraphic.Graphic.Draw(drawAltitude, Rotation, this);
        }

        //Draw top detail graphic
        if (VatGrowerProps.topDetailGraphic != null && (PowerTrader?.PowerOn ?? false))
        {
            drawAltitude += new Vector3(0f, 0.005f, 0f);
            VatGrowerProps.topDetailGraphic.Graphic.Draw(drawAltitude, Rotation, this);
        }

        //Draw glow graphic
        var usePowered = PowerTrader?.PowerOn == true;
        drawAltitude += new Vector3(0f, 0.005f, 0f);

        switch (status)
        {
            case CrafterStatus.Filling when QEESettings.instance.useFillingGraphic:
                var totalAmount = orderProcessor.desiredIngredients.Sum(request => request.amount);
                var currentAmount = totalAmount - orderProcessor.PendingRequests.Sum(request => request.amount);
                QEEMod.TryLog($"Totalamount: {totalAmount}, currentAmount {currentAmount}");
                var fraction = currentAmount / (float)totalAmount;
                var imageIndex = 0;
                if (fraction < 1f)
                {
                    imageIndex = 1;
                }

                if (fraction < 0.75f)
                {
                    imageIndex = 2;
                }

                if (fraction < 0.5f)
                {
                    imageIndex = 3;
                }

                if (fraction < 0.25f)
                {
                    break;
                }

                if (usePowered)
                {
                    VatGrowerProps.glowGraphic[imageIndex].Graphic.Draw(drawAltitude, Rotation, this);
                    break;
                }

                VatGrowerProps.glowGraphicUnpowered[imageIndex].Graphic.Draw(drawAltitude, Rotation, this);
                break;
            case CrafterStatus.Crafting:
            case CrafterStatus.Finished:
                if (usePowered)
                {
                    VatGrowerProps.glowGraphic[0].Graphic.Draw(drawAltitude, Rotation, this);
                    break;
                }

                VatGrowerProps.glowGraphicUnpowered[0].Graphic.Draw(drawAltitude, Rotation, this);
                break;
        }
    }

    public override void Notify_StartedCarryThing(Pawn pawn)
    {
        if (pawn.carryTracker.CarriedThing is not GenomeSequence genome)
        {
            return;
        }

        var genomeRequest =
            orderProcessor.desiredIngredients.FirstOrDefault(req => req.HasThing && req.thing is GenomeSequence);
        if (genomeRequest != null)
        {
            //Update Thing we got a request for.
            genomeRequest.thing = genome;
            //orderProcessor.Notify_ContentsChanged();
        }
    }

    public override void Notify_ThingLostInOrderProcessor()
    {
        StopCrafting();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        if (status == CrafterStatus.Idle)
        {
            yield return new Command_Action
            {
                defaultLabel = "QE_VatGrowerStartCraftingGizmoLabel".Translate(),
                defaultDesc = "QE_VatGrowerStartCraftingGizmoDescription".Translate(),
                icon = ContentFinder<Texture2D>.Get("Things/Item/Health/HealthItem"),
                order = -100,
                action = delegate
                {
                    var options = new List<FloatMenuOption>();

                    var otherVats = new List<Building_PawnVatGrower>();
                    var otherVatGrowers = Map.listerBuildings.allBuildingsColonist
                        .OfType<Building_PawnVatGrower>();
                    otherVats.AddRange(otherVatGrowers);

                    var validGenomes = Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver)?.Where(thing =>
                        !thing.Position.Fogged(Map) && thing is GenomeSequence &&
                        !thing.IsForbidden(Faction.OfPlayer) &&
                        thing.stackCount - otherVats.Count(vat =>
                            vat.status == CrafterStatus.Filling &&
                            vat.orderProcessor.desiredIngredients.FirstOrDefault(req =>
                                req.HasThing && req.thing == thing) != null) > 0);
                    if (validGenomes != null)
                    {
                        foreach (var genomeThing in validGenomes)
                        {
                            var genome = genomeThing as GenomeSequence;

                            //don't add blank genome templates to the list
                            if (genome != null && !genome.IsValidTemplate())
                            {
                                continue;
                            }

                            if (genome == null)
                            {
                                continue;
                            }

                            string label = genome.pawnKindDef.race.LabelCap + " <- " + genome.sourceName;

                            var option = new FloatMenuOption(label, delegate { StartCrafting(genome); });

                            options.Add(option);
                        }
                    }

                    if (options.Count > 0)
                    {
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                    else
                    {
                        //Give a hint.
                        var option = new FloatMenuOption("QE_VatGrowerGenomesHint".Translate(), null)
                        {
                            Disabled = true
                        };
                        options.Add(option);

                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                }
            };
        }
        else
        {
            var shouldRefundIngredients = status == CrafterStatus.Filling;

            if (status == CrafterStatus.Finished)
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "QE_VatGrowerStopCraftingGizmoLabel".Translate(),
                defaultDesc = "QE_VatGrowerStopCraftingGizmoDescription".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel"),
                order = -100,
                action = delegate { StopCrafting(shouldRefundIngredients); }
            };

            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "QE_VatGrowerDebugFinishGrowing".Translate(),
                    defaultDesc = "QE_VatGrowerDebugFinishGrowingDescription".Translate(),
                    //icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true),
                    action = delegate { craftingProgress = TicksNeededToCraft; }
                };
            }
        }
    }
}