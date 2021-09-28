using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
namespace QEthics
{
	public class PreceptComp_NerveStaple : PreceptComp_Thought
	{
		public HistoryEventDef eventDef;

		public bool? doerMustBeMyFaction;

		public ThoughtDef removesThought;

		public bool onlyForNonSlaves;

		public bool doerMustBeMyIdeo;

		public override IEnumerable<TraitRequirement> TraitsAffecting => ThoughtUtility.GetNullifyingTraits(thought);

		public override void Notify_MemberWitnessedAction(HistoryEvent ev, Precept precept, Pawn member)
		{
			if (ev.def != eventDef)
			{
				return;
			}
			Pawn arg;
			bool flag = ev.args.TryGetArg(HistoryEventArgsNames.Doer, out arg);
			bool flag2 = false;
			if (doerMustBeMyFaction.HasValue)
			{
				flag2 = doerMustBeMyFaction.Value;
			}
			else if (flag)
			{
				foreach (ThoughtStage stage in thought.stages)
				{
					if (stage.baseMoodEffect != 0f)
					{
						flag2 = true;
						break;
					}
				}
			}
			else
			{
				flag2 = false;
			}
			if (member.needs != null && member.needs.mood != null && (!flag2 || (flag && arg.Faction == member.Faction)) && (!doerMustBeMyIdeo || arg.Ideo == member.Ideo) && (!onlyForNonSlaves || !member.IsSlave))
			{
				Thought_Memory thought_Memory = ThoughtMaker.MakeThought(thought, precept);
				if (ev.args.TryGetArg(HistoryEventArgsNames.ExecutionThoughtStage, out int arg2))
				{
					thought_Memory.SetForcedStage(Math.Min(arg2, thought.stages.Count - 1));
				}
				Thought_KilledInnocentAnimal thought_KilledInnocentAnimal;
				if ((thought_KilledInnocentAnimal = thought_Memory as Thought_KilledInnocentAnimal) != null && ev.args.TryGetArg(HistoryEventArgsNames.Victim, out Pawn arg3))
				{
					thought_KilledInnocentAnimal.SetAnimal(arg3);
				}
				member.needs.mood.thoughts.memories.TryGainMemory(thought_Memory, arg);
				if (removesThought != null)
				{
					member.needs.mood.thoughts.memories.RemoveMemoriesOfDef(removesThought);
				}
			}
		}
	}
}
