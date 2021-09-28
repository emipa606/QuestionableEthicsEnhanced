using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QEthics
{
	//new nerve stapling behavior
	public class RecipeWorker_NerveStapling_Ideology : Recipe_InstallImplant
	{
		public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
		{

			base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);

			//Check if the Hediff got applied.
			if (!pawn.health.hediffSet.HasHediff(recipe.addsHediff))
			{
				return;
			}

			//Convert to billDoer faction.
			bool isColonist = pawn.Faction == billDoer.Faction;
			
				CyberEnslave(billDoer, pawn);

			//Apply thoughts.
			pawn.needs.mood.thoughts.memories.TryGainMemory(QEThoughtDefOf.QE_RecentlyNerveStapled);
			pawn.needs.mood.thoughts.memories.TryGainMemory(QEThoughtDefOf.QE_NerveStapledMe, billDoer);

			foreach (Pawn thoughtReciever in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonistsAndPrisoners)
			{
				if (thoughtReciever != pawn)
				{
					if (isColonist)
					{
						Find.HistoryEventsManager.RecordEvent(new HistoryEvent(QEHistoryDefOf.NerveStapledColonist, billDoer.Named(HistoryEventArgsNames.Doer)));
						//thoughtReciever.needs.mood.thoughts.memories.TryGainMemory(QEThoughtDefOf.QE_NerveStapledColonist);
					}
					else
					{
						Find.HistoryEventsManager.RecordEvent(new HistoryEvent(QEHistoryDefOf.NerveStapledPawn, billDoer.Named(HistoryEventArgsNames.Doer)));
						//thoughtReciever.needs.mood.thoughts.memories.TryGainMemory(QEThoughtDefOf.QE_NerveStapledPawn);
					}
				}
			}
		}
		public static void CyberEnslave(Pawn surgeon, Pawn stapledPawn)
		{ 
			if (!stapledPawn.IsSlave)
			{
				bool everEnslaved = stapledPawn.guest.EverEnslaved;
				stapledPawn.guest.SetGuestStatus(surgeon.Faction, GuestStatus.Slave);
				Messages.Message("MessagestapledPawnEnslaved".Translate(stapledPawn, surgeon), new LookTargets(stapledPawn, surgeon), MessageTypeDefOf.NeutralEvent);
				
				//HistoryEventDefOf.EnslavedPrisoner
				/**if (!everEnslaved)
				{
					Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.EnslavedPrisonerNotPreviouslyEnslaved, surgeon.Named(HistoryEventArgsNames.Doer)));
				}*/
			}
		}
	}
}

	