using System;
using System.Reflection;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace AnimalGear
{
	internal class AnimalGearHelper
	{
		public static bool IsAnimal(Pawn pawn)
		{
			return (pawn.def.race != null && pawn.def.race.intelligence == Intelligence.Animal);
		}

		public static bool IsAnimalOfColony(Pawn pawn)
		{
			var race = pawn.def.race;
			return (pawn.factionInt != null && pawn.factionInt.def.isPlayer && race != null && race.intelligence == Intelligence.Animal && race.fleshType != FleshTypeDefOf.Mechanoid);
		}

		public static bool IsAnimalOfAFaction(Pawn pawn)
		{
			var race = pawn.def.race;
			return (pawn.factionInt != null && race != null && race.intelligence == Intelligence.Animal && race.fleshType != FleshTypeDefOf.Mechanoid);
		}

		public static void InitAllAnimalTracker(Pawn pawn)
		{
			if (pawn.outfits == null)
			{
				pawn.outfits = new Pawn_OutfitTracker(pawn);
			}
			if (pawn.equipment == null)
			{
				pawn.equipment = new Pawn_EquipmentTracker(pawn);
			}
			if (pawn.apparel == null)
			{
				pawn.apparel = new Pawn_ApparelTracker(pawn);
			}
		}

		public static void CheckAnimalAndReplaceLegacyGear(Pawn pawn, List<ThingDef> allAnimalApparel, bool loading)
		{
			try
			{
				if (pawn == null || pawn.kindDef == null || allAnimalApparel == null || !AnimalGearHelper.IsAnimal(pawn) || pawn.apparel == null || pawn.apparel.WornApparelCount <= 0)
				{
					return;
				}
				string pawnKindDefName = pawn.kindDef.defName.ToString();
				List<KeyValuePair<Apparel, ThingDef>> list = new List<KeyValuePair<Apparel, ThingDef>>();
				for (int i = 0; i < pawn.apparel.WornApparel.Count; i++)
				{
					try
					{
						Apparel apparel = pawn.apparel.WornApparel[i];
						if (string.IsNullOrWhiteSpace(apparel.def.devNote) || !apparel.def.devNote.StartsWith("legacy"))
						{
							continue;
						}
						int num = apparel.def.devNote.IndexOf("[");
						int num2 = apparel.def.devNote.LastIndexOf("]");
						if (num <= 0 || num2 <= num)
						{
							continue;
						}
						List<string> list2 = new List<string>();
						list2 = apparel.def.devNote.Substring(num + 1, num2 - num - 1).Split(',').ToList();
						if (list2 == null || list2.Count <= 0)
						{
							continue;
						}
						foreach (string item in list2)
						{
							string tmpTrimmedReplacementDef = item.TrimStart().TrimEnd();
							ThingDef thingDef = allAnimalApparel.Where((ThingDef def) => def.defName == tmpTrimmedReplacementDef && def.apparel.tags != null && def.apparel.tags.Count > 0 && def.apparel.tags.Contains(pawnKindDefName)).FirstOrDefault();
							if (thingDef == null)
							{
								continue;
							}
							if (Prefs.DevMode)
							{
								Log.Message("AnimalGear: CheckAnimalAndReplaceLegacyGear replacement Found! " + thingDef.defName + " for Gear:" + apparel.def.defName + " devNote:" + (string.IsNullOrWhiteSpace(apparel.def.devNote) ? "null" : apparel.def.devNote));
							}
							bool flag = true;
							try
							{
								pawn.apparel.WornApparel[i].def = thingDef;
								flag = false;
								if (Prefs.DevMode)
								{
									Log.Message("AnimalGear: CheckAnimalAndReplaceLegacyGear switched out def! " + thingDef.defName);
								}
							}
							catch
							{
							}
							if (flag)
							{
								list.Add(new KeyValuePair<Apparel, ThingDef>(apparel, thingDef));
							}
						}
					}
					catch
					{
					}
				}
				if (list == null || list.Count <= 0)
				{
					return;
				}
				foreach (KeyValuePair<Apparel, ThingDef> item2 in list)
				{
					try
					{
						bool locked = pawn.apparel.IsLocked(item2.Key);
						ThingDef stuff = item2.Key.Stuff;
						Apparel newApparel = (Apparel)ThingMaker.MakeThing(item2.Value, stuff);
						pawn.apparel.Wear(newApparel, dropReplacedApparel: false, locked);
						if (Prefs.DevMode)
						{
							Log.Message("AnimalGear: CheckAnimalAndReplaceLegacyGear replaced! (special mod infos, like infused etc. will not be transfered..) " + item2.Value.defName);
						}
					}
					catch
					{
					}
				}
			}
			catch (Exception ex)
			{
				if (Prefs.DevMode)
				{
					Log.Error("AnimalGear: CheckAnimalAndReplaceLegacyGear: error: " + ex.Message);
				}
			}
		}
	
		public static void TryMarkMapPawnFrameSetDirty()
		{
			Map currentMap = Find.CurrentMap;
			if (currentMap == null || currentMap.mapPawns == null) return;
			
			foreach (Pawn item in currentMap.mapPawns.AllPawnsSpawned)
			{
				if (AnimalGearHelper.IsAnimal(item))
				{
					GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(item);
				}
			}
		}
	}
}
