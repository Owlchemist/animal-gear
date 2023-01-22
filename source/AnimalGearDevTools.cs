using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace AnimalGear
{
	internal static class AnimalGearDevTools
	{
		[DebugAction(null, null, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns, category = "AnimalGear")]
		private static void GearInfo(Pawn pawn)
		{
			try
			{
				if (pawn == null)
				{
					return;
				}
				string text = "";
				string text2 = "";
				string text3 = "";
				if (pawn.outfits != null)
				{
					if (text != "")
					{
						text = text + ", " + Environment.NewLine;
					}
					text += "has outfits tracker";
				}
				if (pawn.equipment != null)
				{
					if (text != "")
					{
						text = text + ", " + Environment.NewLine;
					}
					text += "has equipment tracker";
				}
				if (pawn.apparel != null)
				{
					if (text != "")
					{
						text = text + ", " + Environment.NewLine;
					}
					foreach (Apparel item in pawn.apparel.WornApparel)
					{
						text2 = ((!(text2 != "")) ? (text2 + item.LabelShortCap) : (text2 + ", " + Environment.NewLine + item.LabelShortCap));
					}
					text = ((!(text2 != "")) ? (text + "has apparel tracker") : (text + "has apparel tracker: [" + Environment.NewLine + text2 + Environment.NewLine + "]"));
				}
				if (text != "")
				{
					text = " (" + text + ")";
				}
				try
				{
					text3 = string.Concat("[baseBodySize: ", pawn.RaceProps.baseBodySize.ToString(), " drawSize:", pawn.Graphic.drawSize, "]");
				}
				catch
				{
				}
				if (AnimalGearHelper.IsAnimalOfAFaction(pawn))
				{
					Messages.Message(string.Format("{0} is an animal of a faction" + Environment.NewLine + text + Environment.NewLine + text3, pawn.LabelShort), (GlobalTargetInfo)pawn, MessageTypeDefOf.NeutralEvent, historical: false);
					if (Prefs.DevMode)
					{
						Log.Message(string.Format("{0} is an animal of a faction" + Environment.NewLine + text + Environment.NewLine + text3, pawn.LabelShort));
					}
				}
				else if (AnimalGearHelper.IsAnimal(pawn))
				{
					Messages.Message(string.Format("{0} is an animal" + Environment.NewLine + text + Environment.NewLine + text3, pawn.LabelShort), (GlobalTargetInfo)pawn, MessageTypeDefOf.NeutralEvent, historical: false);
					if (Prefs.DevMode)
					{
						Log.Message(string.Format("{0} is an animal" + Environment.NewLine + text + Environment.NewLine + text3, pawn.LabelShort));
					}
				}
				else
				{
					Messages.Message(string.Format("{0} is not an animal" + Environment.NewLine + text + Environment.NewLine + text3, pawn.LabelShort), (GlobalTargetInfo)pawn, MessageTypeDefOf.NeutralEvent, historical: false);
					if (Prefs.DevMode)
					{
						Log.Message(string.Format("{0} is not an animal" + Environment.NewLine + text + Environment.NewLine + text3, pawn.LabelShort));
					}
				}
			}
			catch (Exception ex)
			{
				if (Prefs.DevMode)
				{
					Log.Error("AG: GearInfo: error: " + ex.Message);
				}
			}
		}

		[DebugAction(null, null, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns, category = "AnimalGear")]
		private static void GearInfoWithTextures(Pawn pawn)
		{
			try
			{
				if (pawn == null)
				{
					return;
				}
				string text = "";
				string text2 = "";
				string text3 = "";
				if (pawn.outfits != null)
				{
					if (text != "")
					{
						text = text + ", " + Environment.NewLine;
					}
					text += "has outfits tracker";
				}
				if (pawn.equipment != null)
				{
					if (text != "")
					{
						text = text + ", " + Environment.NewLine;
					}
					text += "has equipment tracker";
				}
				if (pawn.apparel != null)
				{
					if (text != "")
					{
						text = text + ", " + Environment.NewLine;
					}
					foreach (Apparel item in pawn.apparel.WornApparel)
					{
						text2 = ((!(text2 != "")) ? (text2 + item.LabelShortCap + Environment.NewLine + GetApparelTexturePath(pawn, item)) : (text2 + ", " + Environment.NewLine + item.LabelShortCap + Environment.NewLine + GetApparelTexturePath(pawn, item)));
					}
					text = ((!(text2 != "")) ? (text + "has apparel tracker") : (text + "has apparel tracker: [" + Environment.NewLine + text2 + Environment.NewLine + "]"));
				}
				if (text != "")
				{
					text = " (" + text + ")";
				}
				try
				{
					text3 = string.Concat("[baseBodySize: ", pawn.RaceProps.baseBodySize.ToString(), " drawSize:", pawn.Graphic.drawSize, "]");
				}
				catch
				{
				}
				if (AnimalGearHelper.IsAnimalOfAFaction(pawn))
				{
					Messages.Message(string.Format("{0} is an animal of a faction" + Environment.NewLine + text + Environment.NewLine + text3, pawn.LabelShort), (GlobalTargetInfo)pawn, MessageTypeDefOf.NeutralEvent, historical: false);
					if (Prefs.DevMode)
					{
						Log.Message(string.Format("{0} is an animal of a faction" + Environment.NewLine + text + Environment.NewLine + text3, pawn.LabelShort));
					}
				}
				else if (AnimalGearHelper.IsAnimal(pawn))
				{
					Messages.Message(string.Format("{0} is an animal" + Environment.NewLine + text + Environment.NewLine + text3, pawn.LabelShort), (GlobalTargetInfo)pawn, MessageTypeDefOf.NeutralEvent, historical: false);
					if (Prefs.DevMode)
					{
						Log.Message(string.Format("{0} is an animal" + Environment.NewLine + text + Environment.NewLine + text3, pawn.LabelShort));
					}
				}
				else
				{
					Messages.Message(string.Format("{0} is not an animal" + Environment.NewLine + text + Environment.NewLine + text3, pawn.LabelShort), (GlobalTargetInfo)pawn, MessageTypeDefOf.NeutralEvent, historical: false);
					if (Prefs.DevMode)
					{
						Log.Message(string.Format("{0} is not an animal" + Environment.NewLine + text + Environment.NewLine + text3, pawn.LabelShort));
					}
				}
			}
			catch (Exception ex)
			{
				if (Prefs.DevMode)
				{
					Log.Error("AG: GearInfoWithTextures: error: " + ex.Message);
				}
			}
		}

		private static string GetApparelTexturePath(Pawn pawn, Apparel apparel)
		{
			try
			{
				string text = "";
				string text2 = "";
				string text3 = "";
				PawnKindDef kindDef = pawn.kindDef;
				if (apparel.WornGraphicPath.NullOrEmpty())
				{
					return "(No WornGraphicPath!)";
				}
				if (apparel.def.apparel.tags == null || !apparel.def.apparel.tags.Contains("AnimalALL"))
				{
					if (kindDef != null)
					{
						text = text + "_" + kindDef.defName.ToString();
					}
					if (pawn.ageTracker != null)
					{
						PawnKindLifeStage curKindLifeStage = pawn.ageTracker.CurKindLifeStage;
						if (curKindLifeStage != null && curKindLifeStage.bodyGraphicData != null && !string.IsNullOrEmpty(curKindLifeStage.bodyGraphicData.texPath))
						{
							text3 = "_" + pawn.ageTracker.CurLifeStageIndex;
							text2 += text3;
						}
						if (pawn.gender == Gender.Female && curKindLifeStage != null && curKindLifeStage.femaleGraphicData != null && !string.IsNullOrEmpty(curKindLifeStage.femaleGraphicData.texPath))
						{
							text2 += "_female";
						}
						if (!string.IsNullOrEmpty(text2))
						{
							if (string.IsNullOrEmpty(text3))
							{
								return "(looking for '" + apparel.WornGraphicPath + text + "_east' OR '" + apparel.WornGraphicPath + text + text2 + "_east')";
							}
							return "(looking for '" + apparel.WornGraphicPath + text + "_east' OR '" + apparel.WornGraphicPath + text + text2 + "_east' OR '" + apparel.WornGraphicPath + text + text3 + "_east')";
						}
					}
				}
				return "(looking for '" + apparel.WornGraphicPath + text + "_east')";
			}
			catch
			{
			}
			return "(error..)";
		}

		[DebugAction(null, null, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns, category = "AnimalGear")]
		private static void ArmorInfo(Pawn pawn)
		{
			try
			{
				if (pawn == null)
				{
					return;
				}
				string text = "";
				if (pawn.outfits != null && pawn.equipment != null && pawn.apparel != null)
				{
					foreach (Apparel item in pawn.apparel.WornApparel)
					{
						_ = item;
						float num = 0f;
						StatDef armorRating_Sharp = StatDefOf.ArmorRating_Sharp;
						float num2 = Mathf.Clamp01(pawn.GetStatValue(armorRating_Sharp) / 2f);
						List<BodyPartRecord> list = pawn.RaceProps.body.AllParts.Where((BodyPartRecord x) => x.groups != null && x.groups.Count > 0).ToList();
						List<Apparel> list2 = ((pawn.apparel != null) ? pawn.apparel.WornApparel : null);
						for (int i = 0; i < list.Count; i++)
						{
							float num3 = 1f - num2;
							if (list2 != null)
							{
								for (int j = 0; j < list2.Count; j++)
								{
									if (list2[j].def.apparel.CoversBodyPart(list[i]))
									{
										float num4 = Mathf.Clamp01(list2[j].GetStatValue(armorRating_Sharp) / 2f);
										num3 *= 1f - num4;
									}
								}
							}
							num += list[i].coverageAbs * (1f - num3);
							text = string.Concat(text, list[i].LabelShort + ": " + list[i].coverageAbs * (1f - num3), (i % 2 == 0) ? Environment.NewLine : ", ");
						}
						num = Mathf.Clamp(num * 2f, 0f, 2f);
					}
				}
				if (text != "")
				{
					Messages.Message(string.Format("{0} Sharp:" + Environment.NewLine + text, pawn.LabelShort), (GlobalTargetInfo)pawn, MessageTypeDefOf.NeutralEvent, historical: false);
				}
			}
			catch (Exception ex)
			{
				if (Prefs.DevMode)
				{
					Log.Error("AG: ArmorInfo: error: " + ex.Message);
				}
			}
		}

		[DebugAction(null, null, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns, category = "AnimalGear")]
		private static void ForceDropClothes(Pawn pawn)
		{
			try
			{
				if (pawn != null && pawn.apparel != null)
				{
					pawn.apparel.DropAll(pawn.Position);
				}
			}
			catch (Exception ex)
			{
				if (Prefs.DevMode)
				{
					Log.Error("AG: ForceDropClothes: error: " + ex.Message);
				}
			}
		}

		[DebugAction(null, null, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns, category = "AnimalGear")]
		private static void ForceWearItem(Pawn pawn)
		{
			try
			{
				if (pawn == null || pawn.apparel == null)
				{
					return;
				}
				if (!AnimalGearHelper.IsAnimalOfAFaction(pawn))
				{
					Messages.Message($"{pawn.LabelShort} is not an animal of a faction", (GlobalTargetInfo)pawn, MessageTypeDefOf.NeutralEvent, historical: false);
				}
				List<DebugMenuOption> list = new List<DebugMenuOption>();
				foreach (ThingDef def2 in from def in DefDatabase<ThingDef>.AllDefs
					where def.IsApparel
					select def into d
					orderby d.defName
					select d)
				{
					if (def2 == null || !def2.IsApparel || def2.apparel.tags == null || def2.apparel.tags.Count <= 0 || (!def2.apparel.tags.Contains("Animal") && !def2.apparel.tags.Contains("AnimalCompatible")))
					{
						continue;
					}
					if (ApparelUtility.HasPartsToWear(pawn, def2))
					{
						list.Add(new DebugMenuOption(def2.defName, DebugMenuOptionMode.Action, delegate
						{
							ThingDef stuff = GenStuff.RandomStuffFor(def2);
							Apparel newApparel = (Apparel)ThingMaker.MakeThing(def2, stuff);
							pawn.apparel.Wear(newApparel);
						}));
					}
					else if ((pawn.kindDef != null && def2.apparel.tags.Contains(pawn.kindDef.defName.ToString())) || def2.apparel.tags.Contains("AnimalALL"))
					{
						list.Add(new DebugMenuOption(def2.defName + "-noParts!", DebugMenuOptionMode.Action, delegate
						{
							Messages.Message("Incorrect body parts for apparel", MessageTypeDefOf.RejectInput, historical: false);
						}));
					}
					else
					{
						list.Add(new DebugMenuOption(def2.defName + "-noTag!", DebugMenuOptionMode.Action, delegate
						{
							Messages.Message("Incorrect tags for apparel", MessageTypeDefOf.RejectInput, historical: false);
						}));
					}
				}
				if (list == null || list.Count == 0)
				{
					Messages.Message("No apparel for pawn", MessageTypeDefOf.RejectInput, historical: false);
				}
				else
				{
					Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
				}
			}
			catch (Exception ex)
			{
				if (Prefs.DevMode)
				{
					Log.Error("AG: ForceWearItem: error: " + ex.Message);
				}
			}
		}
	}
}
