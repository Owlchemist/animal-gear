using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AnimalGear
{
	public class AnimalGear_JobDriver_OpenStylingStationDialog : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		public override IEnumerable<Toil> MakeNewToils()
		{
			if (ModLister.CheckIdeology("Styling station"))
			{
				yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOnDespawnedOrNull(TargetIndex.A);
				yield return Toils_General.Do(delegate
				{
					Find.WindowStack.Add(new AnimalGear_Dialog_StylingStation(pawn, job.targetA.Thing));
				});
			}
		}
	}
}
