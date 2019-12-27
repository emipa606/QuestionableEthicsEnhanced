using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace QEthics
{
    /// <summary>
    /// Copy of vanilla's Recipe_InstallNaturalBodyPart, with some minor additions to improve availability of surgeries
    /// </summary>
    public class RecipeWorker_InstallNaturalBodyPart : Recipe_Surgery
    {
        /// <summary>
        /// Copy of vanilla's Recipe_InstallNaturalBodyPart.GetPartsToApplyOn(), with the addition of adding a 
        /// BodyPart to the list of recipes if a child BodyPart has hediffs.
        /// Example - If a pawn damages or loses a finger, the recipe to install a new hand will be added to the list.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="recipe"></param>
        /// <returns></returns>
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            for (int j = 0; j < recipe.appliedOnFixedBodyParts.Count; j++)
            {
                BodyPartDef recipePart = recipe.appliedOnFixedBodyParts[j];
                List<BodyPartRecord> bpList = pawn.RaceProps.body.AllParts;
                for (int i = 0; i < bpList.Count; i++)
                {
                    BodyPartRecord record = bpList[i];
                    if (record.def == recipePart &&

                        //true if this body part has hediffs
                        //pawn.health.hediffSet.hediffs.Any((Hediff x) => x.Part == record) &&
                        pawn.health.hediffSet.hediffs.Any((Hediff x) => x.Part == record || 
                            //true if any child parts have hediffs
                            (x.Part?.parent != null && x.Part.parent == record)) &&

                        //true if NOT a finger/toe or true if finger where hand is still attached (for example)
                        (record.parent == null || pawn.health.hediffSet.GetNotMissingParts().Contains(record.parent)) &&

                        //true if part does NOT have prosthetics
                        (!pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(record) || pawn.health.hediffSet.HasDirectlyAddedPartFor(record))
                    )
                    {
                        yield return record;
                    }
                }
            }
        }

        /// <summary>
        /// Copy of vanilla's Recipe_InstallNaturalBodyPart.GetPartsToApplyOn() plus the addition of code that respects the addsHediff node
        /// in the recipe (vanilla does not). The addsHediff node is currently used in the RBSE compatibility patch to add organ rejection.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="part"></param>
        /// <param name="billDoer"></param>
        /// <param name="ingredients"></param>
        /// <param name="bill"></param>
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null && !CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
                MedicalRecipesUtility.RestorePartAndSpawnAllPreviousParts(pawn, part, billDoer.Position, billDoer.Map);
            }

            //add any hediffs in the <addsHediff> element of the recipe after surgery involving natural body parts completes.
            if (bill.recipe.addsHediff != null && RBSECompat.GetOrganRejectionSetting() == true)
            {
                if (part.LabelShort == "shoulder")
                {
                    //add hediff to arm bodypart
                    //foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where(def => def.category == ThingCategory.Pawn))
                    foreach (BodyPartRecord childPart in part.parts)
                    {
                        if (childPart.def == BodyPartDefOf.Arm)
                        {
                            QEEMod.TryLog("Adding hediff to installed BodyPart " + childPart.Label);
                            pawn.health.AddHediff(bill.recipe.addsHediff, childPart);
                            return;
                        }
                    }

                    QEEMod.TryLog("No arm found in shoulder replacement!");
                }
                else
                {
                    QEEMod.TryLog("Adding hediff to installed BodyPart " + part.Label);
                    pawn.health.AddHediff(bill.recipe.addsHediff, part);
                }
                    
            }
        }
    }
}
