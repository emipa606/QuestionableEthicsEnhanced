using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.AI;

namespace QEthics
{
    /// <summary>
    /// Building for growing things like organs. Requires constant maintenance in order to not botch the crafting. Dirty rooms increase maintenance drain even more.
    /// </summary>
    public class Building_OrganVat : Building_GrowerBase_WorkTable, IMaintainableGrower
    {
        static Building_OrganVat()
        {
            cleanlinessCurve.Add(-5.0f, 5.00f);
            cleanlinessCurve.Add(-2.0f, 1.75f);
            cleanlinessCurve.Add(0.0f, 1.0f);
            cleanlinessCurve.Add(0.4f, 0.35f);
            cleanlinessCurve.Add(2.0f, 0.1f);
        }

        public static SimpleCurve cleanlinessCurve = new SimpleCurve();

        /// <summary>
        /// Ticks required to craft organ. Takes organGrowthRate ModSetting into account.
        /// </summary>
        public override int TicksNeededToCraft => (int)(activeRecipe?.workAmount * QEESettings.instance.organGrowthRateFloat ?? 0);

        /// <summary>
        /// From 0.0 to 1.0. If the maintenance is below 50% there is a chance for failure.
        /// </summary>
        public float scientistMaintenance;

        /// <summary>
        /// From 0.0 to 1.0. If the maintenance is below 50% there is a chance for failure.
        /// </summary>
        public float doctorMaintenance;

        public float RoomCleanliness
        {
            get
            {
                Room room = this.GetRoom(RegionType.Set_Passable);
                if (room != null)
                {
                    return room.GetStat(RoomStatDefOf.Cleanliness);
                }

                return 0f;
            }
        }

        private VatGrowerProperties vatGrowerPropsInt;

        public VatGrowerProperties VatGrowerProps
        {
            get
            {
                if (vatGrowerPropsInt == null)
                {
                    vatGrowerPropsInt = def.GetModExtension<VatGrowerProperties>();

                    //Fallback; Is defaults.
                    if (vatGrowerPropsInt == null)
                    {
                        vatGrowerPropsInt = new VatGrowerProperties();
                    }
                }

                return vatGrowerPropsInt;
            }
        }

        public float ScientistMaintenance { get => scientistMaintenance; set => scientistMaintenance = value; }

        public float DoctorMaintenance { get => doctorMaintenance; set => doctorMaintenance = value; }

        public Building_OrganVat() : base()
        {
            
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Defs.Look(ref activeRecipe, "activeRecipe");
            Scribe_Values.Look(ref scientistMaintenance, "scientistMaintenance");
            Scribe_Values.Look(ref doctorMaintenance, "doctorMaintenance");
        }

        public override string GetInspectString()
        {
            if(!(ParentHolder is Map))
            {
                return null;
            }

            StringBuilder builder = new StringBuilder(base.GetInspectString());

            if (status == CrafterStatus.Crafting)
            {
                builder.AppendLine();
                builder.AppendLine("QE_VatGrowerMaintenance".Translate(String.Format("{0:0%}", scientistMaintenance), 
                    String.Format("{0:0%}", doctorMaintenance)));

                builder.AppendLine("QE_VatGrowerCleanlinessMult".Translate(cleanlinessCurve.Evaluate(RoomCleanliness).ToString("0.00")));
            }

            return builder.ToString().TrimEndNewlines();
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            //Draw bottom graphic
            Vector3 drawAltitude = drawLoc;
            if(VatGrowerProps.bottomGraphic != null)
            {
                VatGrowerProps.bottomGraphic.Graphic.Draw(drawAltitude, Rotation, this);
            }

            //Draw product
            drawAltitude += new Vector3(0f, 0.005f, 0f);
            if ((status == CrafterStatus.Crafting || status == CrafterStatus.Finished) && activeRecipe != null && 
                activeRecipe.GetModExtension<RecipeGraphicProperties>().productGraphic != null)
            {
                //TODO - BETTER NULL CHECKING AND DEFAULT VALUES
                RecipeGraphicProperties recipeProps = activeRecipe.GetModExtension<RecipeGraphicProperties>();
                Material material = recipeProps.productGraphic.Graphic.MatSingle;
                QEEMod.TryLog("defName: " + activeRecipe.defName + " | material texPath: " + 
                    activeRecipe.GetModExtension<RecipeGraphicProperties>().productGraphic.texPath);

                float scale = (0.2f + (CraftingProgressPercent * 0.8f)) * VatGrowerProps.productScaleModifier * recipeProps.scale;
                Vector3 scaleVector = new Vector3(scale, 1f, scale);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(drawAltitude + VatGrowerProps.productOffset, Quaternion.AngleAxis(0f, Vector3.up), scaleVector);

                Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            }

            //Draw top graphic
            if (VatGrowerProps.topGraphic != null)
            {
                drawAltitude += new Vector3(0f, 0.005f, 0f);
                VatGrowerProps.topGraphic.Graphic.Draw(drawAltitude, Rotation, this);
            }

            //Draw top detail graphic
            if (VatGrowerProps.topDetailGraphic != null && (PowerTrader?.PowerOn ?? false))
            {
                drawAltitude += new Vector3(0f, 0.005f, 0f);
                VatGrowerProps.topDetailGraphic.Graphic.Draw(drawAltitude, Rotation, this);
            }

            //Draw glow graphic
            if ((status == CrafterStatus.Crafting || status == CrafterStatus.Finished) && VatGrowerProps.glowGraphic != null && (PowerTrader?.PowerOn ?? false))
            {
                drawAltitude += new Vector3(0f, 0.005f, 0f);
                VatGrowerProps.glowGraphic.Graphic.Draw(drawAltitude, Rotation, this);
            }
        }

        /// <summary>
        /// Called from WorkGiver_DoBill_Grower.TryStartNewDoBillJob()
        /// </summary>
        /// <param name="recipeDef"></param>
        public override void Notify_FillingStarted(RecipeDef recipeDef)
        {
            //Initialize maintenance
            scientistMaintenance = 0.25f;
            doctorMaintenance = 0.25f;

            activeRecipe = recipeDef;
            status = CrafterStatus.Filling;
        }

        public override void Notify_CraftingStarted()
        {
            status = CrafterStatus.Crafting;
        }

        public override void Notify_CraftingFinished()
        {
            Messages.Message("QE_MessageGrowingDone".Translate(activeRecipe.products[0].thingDef.LabelCap), new LookTargets(this), MessageTypeDefOf.PositiveEvent, false);
        }

        public override void Tick_Crafting()
        {
            base.Tick_Crafting();

            //Deduct maintenance, fail if any of them go below 0%.
            float powerModifier = 1f;
            if (PowerTrader != null && !PowerTrader.PowerOn)
            {
                powerModifier = 15f;
            }
            float cleanlinessModifer = cleanlinessCurve.Evaluate(RoomCleanliness);
            float decayRate = 0.0012f * cleanlinessModifer * powerModifier / (QEESettings.instance.maintRateFloat);

            scientistMaintenance -= decayRate;
            doctorMaintenance -= decayRate;

            if(scientistMaintenance < 0f || doctorMaintenance < 0f)
            {
                //Fail the craft, waste all products.
                StopCrafting(false);
                if (activeRecipe?.products[0]?.thingDef?.defName != null)
                {
                    Messages.Message("QE_OrgaVatMaintFailMessage".Translate(activeRecipe.products[0].thingDef.defName.Named("ORGANNAME")), 
                        new LookTargets(this), MessageTypeDefOf.NegativeEvent);
                }
                else
                {
                    Messages.Message("QE_OrgaVatMaintFailFallbackMessage".Translate(), new LookTargets(this), MessageTypeDefOf.NegativeEvent);
                }
            }
        }

        /// <summary>
        /// Called from JobDriver_ExtractProductFromGrower. That Job is initiated by WorkGiver_ExtractProductFromGrower, when crafting status is Finished.
        /// </summary>
        /// <param name="actor"></param>
        /// <returns></returns>
        public override bool TryExtractProduct(Pawn actor)
        {
            Thing product = ThingMaker.MakeThing(activeRecipe.products[0].thingDef);
            product.stackCount = activeRecipe.products[0].count;

            //place product on the interaction cell of the grower
            GenPlace.TryPlaceThing(product, this.InteractionCell, this.Map, ThingPlaceMode.Near);

            //Pawn extracting product hauls product back to storage
            IntVec3 storeCell;
            IHaulDestination haulDestination;
            if (StoreUtility.TryFindBestBetterStorageFor(product, actor, product.Map, StoragePriority.Unstored, actor.Faction, out storeCell, out haulDestination, false))
            {
                if (storeCell.IsValid || haulDestination != null)
                {
                    actor.jobs.StartJob(HaulAIUtility.HaulToStorageJob(actor, product), JobCondition.Succeeded);
                }
            }

            if (status == CrafterStatus.Finished)
            {
                StopCrafting(false);
            }

            return true;
        }

        public override string TransformStatusLabel(string label)
        {
            string recipeLabel = activeRecipe?.LabelCap ?? "QE_VatGrowerNoRecipe".Translate();

            if (status == CrafterStatus.Filling || status == CrafterStatus.Finished)
            {
                return label + " " + recipeLabel.CapitalizeFirst();
            }
            if (status == CrafterStatus.Crafting)
            {
                float daysRemaining = GenDate.TicksToDays(TicksLeftToCraft);
                if (daysRemaining > 1.0)
                {
                    return recipeLabel.CapitalizeFirst() + " (" + String.Format("{0:0.0}", daysRemaining) +
                        " " + "QE_VatGrowerDaysRemaining".Translate() + ")";
                }
                else
                {
                    return " " + recipeLabel.CapitalizeFirst() + " (" + String.Format("{0:0.0}", (TicksLeftToCraft / 2500.0f)) +
                        " " + "QE_VatGrowerHoursRemaining".Translate() + ")";
                }
            }

            return base.TransformStatusLabel(label);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (status != CrafterStatus.Idle)
            {

                bool shouldRefundIngredients = false;
                if (status == CrafterStatus.Filling)
                {
                    shouldRefundIngredients = true;
                }
                if (status != CrafterStatus.Finished)
                {
                    yield return new Command_Action()
                    {
                        defaultLabel = "QE_VatGrowerStopCraftingGizmoLabel".Translate(),
                        defaultDesc = "QE_VatGrowerStopCraftingGizmoDescription".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true),
                        order = -100,
                        action = delegate ()
                        {
                            StopCrafting(shouldRefundIngredients);
                        }
                    };
                    if (Prefs.DevMode)
                    {
                        yield return new Command_Action()
                        {
                            defaultLabel = "QE_VatGrowerDebugFinishGrowing".Translate(),
                            defaultDesc = "QE_OrganVatDebugFinishGrowingDescription".Translate(),
                            action = delegate ()
                            {
                                craftingProgress = TicksNeededToCraft;
                            }
                        };
                    }
                }
            }
        }
    } //end class Building_OrganVat
}
