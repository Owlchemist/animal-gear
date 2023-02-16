using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AnimalGear
{
	static class AnimalGearHarmony
	{
		public const string ModVersion = "v1.4.0.0",
			ModNameGiddyUp = "giddy-up 2",
			ModNameAlphaAnimals = "Alpha Animals",
			ModNameRPGStyleInventory = "RPG Style Inventory",
			ModNameGiddyUpDLL = "GiddyUpCore",
			ModNameRPGStyleInventoryDLL = "Sandy_Detailed_RPG_Inventory",
			suffixToCheck = "_east";
		public static bool ModGiddyUp_ON,
			ModAlphaAnimals_ON,
			ModRPGStyleInventory_ON,
			ModGiddyUp_GiddyUp_CompOverlayFound,
			ModRPGStyleInventory_RPG_GearTabFound,
			ModRPGStyleInventory_HumanlikePatched,
			ModRPGStyleInventory_FillTabPatched;

		public static Type ModGiddyUp_GiddyUp_CompOverlay, ModRPGStyleInventory_RPG_GearTab;
		public static int ModGiddyUp_Patched, ModRPGStyleInventory_Patched;

		[HarmonyPatch(typeof(PawnComponentsUtility), nameof(PawnComponentsUtility.CreateInitialComponents))]
		public static class PawnComponentsUtility_CreateInitialComponents_Patch
		{
			public static void Postfix(Pawn pawn)
			{
				try
				{
					if (AnimalGearHelper.IsAnimalOfAFaction(pawn))
					{
						AnimalGearHelper.InitAllAnimalTracker(pawn);
					}
				}
				catch (Exception ex)
				{
					if (Prefs.DevMode)
					{
						Log.Error("PawnComponentsUtility_CreateInitialComponents_Patch: error: " + ex.Message);
					}
				}
			}
		}

		[HarmonyPatch(typeof(PawnComponentsUtility), nameof(PawnComponentsUtility.AddAndRemoveDynamicComponents))]
		public static class PawnComponentsUtility_AddAndRemoveDynamicComponents_Patch
		{
			public static void Postfix(Pawn pawn, bool actAsIfSpawned)
			{
				try
				{
					if (AnimalGearHelper.IsAnimalOfAFaction(pawn))
					{
						AnimalGearHelper.InitAllAnimalTracker(pawn);
					}
				}
				catch (Exception ex)
				{
					if (Prefs.DevMode)
					{
						Log.Error("PawnComponentsUtility_AddAndRemoveDynamicComponents_Patch: error: " + ex.Message);
					}
				}
			}
		}

		[HarmonyPatch(typeof(ITab_Pawn_Gear))]
		[HarmonyPatch("IsVisible", MethodType.Getter)]
		public static class ITab_Pawn_Gear_IsVisible_Patch
		{
			public static void Postfix(ITab_Pawn_Gear __instance, ref bool __result)
			{
				try
				{
					if (!__result)
					{
						Pawn pawn = SelPawnForGearPatch(__instance);
						if (pawn != null && pawn != null && AnimalGearHelper.IsAnimalOfAFaction(pawn))
						{
							__result = true;
						}
					}
				}
				catch (Exception ex)
				{
					if (Prefs.DevMode)
					{
						Log.Error("ITab_Pawn_Gear_IsVisible_Patch: error: " + ex.Message);
					}
				}
			}

			public static Pawn SelPawnForGearPatch(ITab_Pawn_Gear __instance)
			{
				Thing singleSelectedThing = Find.Selector.SingleSelectedThing;
				if (singleSelectedThing != null)
				{
					Pawn pawn = null;
					try
					{
						pawn = (Pawn)singleSelectedThing;
					}
					catch
					{
					}
					if (pawn != null)
					{
						return pawn;
					}
					Corpse corpse = null;
					try
					{
						corpse = singleSelectedThing as Corpse;
					}
					catch
					{
					}
					if (corpse != null)
					{
						return corpse.InnerPawn;
					}
					throw new InvalidOperationException("Gear tab on non-pawn non-corpse " + singleSelectedThing);
				}
				return null;
			}
		}

		[HarmonyPatch(typeof(RaceProperties), nameof(RaceProperties.Humanlike), MethodType.Getter)]
		static class RPGInventory_RaceProperties_Humanlike_Patch
		{
			static bool Prepare()
			{
				return AnimalGearHarmony.ModRPGStyleInventory_ON && AnimalGearHarmony.ModRPGStyleInventory_RPG_GearTabFound;
			}
			static void Postfix(ref bool __result)
			{
				if (ModRPGStyleInventory_ON && RPGInventory_FillTab_Patch.cachedAnimalGearRPGInventoryAnimalCompatibilityEnabled && RPGInventory_FillTab_Patch.currentIsAnimalOfAFactionActive)
				{
					__result = true;
				}
			}
			static void Cleanup()
			{
				AnimalGearHarmony.ModRPGStyleInventory_HumanlikePatched = true;
				AnimalGearHarmony.ModRPGStyleInventory_Patched++;
			}
		}

		[HarmonyPatch]
		public static class RPGInventory_FillTab_Patch
		{
			public static bool currentIsFillingTabRightNow,
				currentIsAnimalActive,
				currentIsAnimalOfColonyActive,
				currentIsAnimalOfAFactionActive,
				cachedAnimalGearRPGInventoryAnimalCompatibilityEnabled;

			public static Pawn currentPawn;
			static MethodBase target;

			static bool Prepare()
			{
				target = AccessTools.Method(AnimalGearHarmony.ModRPGStyleInventory_RPG_GearTab, "FillTab");
				if (target == null) return false;
				return AnimalGearHarmony.ModRPGStyleInventory_ON && AnimalGearHarmony.ModRPGStyleInventory_RPG_GearTabFound;
			}

			static MethodBase TargetMethod()
			{
				return target;
			}
			
			static void Prefix(ITab_Pawn_Gear __instance)
			{
				currentPawn = null;
				currentIsFillingTabRightNow = true;
				currentIsAnimalActive = false;
				currentIsAnimalOfColonyActive = false;
				currentIsAnimalOfAFactionActive = false;
				
				
				cachedAnimalGearRPGInventoryAnimalCompatibilityEnabled = AnimalGearSettings.AnimalGearRPGInventoryAnimalCompatibilityEnabled;
				currentPawn = ITab_Pawn_Gear_IsVisible_Patch.SelPawnForGearPatch(__instance);
				currentIsAnimalActive = AnimalGearHelper.IsAnimal(currentPawn);
				if (currentIsAnimalActive)
				{
					currentIsAnimalOfColonyActive = AnimalGearHelper.IsAnimalOfColony(currentPawn);
					currentIsAnimalOfAFactionActive = AnimalGearHelper.IsAnimalOfAFaction(currentPawn);
				}
				
			}

			static void Postfix()
			{
				currentIsFillingTabRightNow = false;
				currentIsAnimalActive = false;
				currentIsAnimalOfColonyActive = false;
				currentIsAnimalOfAFactionActive = false;
				currentPawn = null;
			}

			static void Cleanup()
			{
				AnimalGearHarmony.ModRPGStyleInventory_Patched++;
				AnimalGearHarmony.ModRPGStyleInventory_FillTabPatched = true;
				target = null;
			}
		}

		[HarmonyPatch(typeof(FloatMenuOption), "", MethodType.Constructor)]
		[HarmonyPatch(new Type[]
		{
			typeof(string),
			typeof(Action),
			typeof(MenuOptionPriority),
			typeof(Action<Rect>),
			typeof(Thing),
			typeof(float),
			typeof(Func<Rect, bool>),
			typeof(WorldObject),
			typeof(bool),
			typeof(int)
		})]
		[HarmonyPriority(Priority.VeryLow)]
		public static class FloatMenuOption_FloatMenuOption_Patch
		{
			public static void Postfix(FloatMenuOption __instance, Action action)
			{
				if (action == null || action.Target == null)
				{
					return;
				}
				var traverse = Traverse.Create(action.Target);
				if (!traverse.Fields().Contains("recipe")) return;
				
				RecipeDef recipeDef = (RecipeDef)traverse.Field("recipe").GetValue();
				if (recipeDef != null && recipeDef.products != null && 
				recipeDef.products.Where((ThingDefCountClass t) => t != null && t.thingDef != null && t.thingDef.apparel != null && t.thingDef.apparel.tags != null && t.thingDef.apparel.tags.Contains("Animal")).FirstOrDefault() != null)
				{
					if (!__instance.Label.EndsWith("Animal"))
					{
						__instance.Label += " Animal";
					}
					__instance.Priority = MenuOptionPriority.Low;
				}
			}
		}

		[HarmonyPatch(typeof(PawnGraphicSet), nameof(PawnGraphicSet.ResolveApparelGraphics))]
		[HarmonyBefore(new string[] { "com.tammybee.apparentclothes", "apparentclothes" })]
		[HarmonyPriority(600)]
		public static class PawnGraphicSet_ResolveApparelGraphics_Patch
		{
			public static bool Prefix(PawnGraphicSet __instance)
			{
				try
				{
					if (AnimalGearHelper.IsAnimalOfAFaction(__instance.pawn))
					{
						try
						{
							AnimalGearHelper.InitAllAnimalTracker(__instance.pawn);
							__instance.ClearCache();
							__instance.apparelGraphics.Clear();
							foreach (Apparel item in __instance.pawn.apparel.WornApparel)
							{
								if (TryGetGraphicApparelAnimal(item, __instance.pawn, __instance.pawn.kindDef, out var rec))
								{
									__instance.apparelGraphics.Add(rec);
								}
							}
						}
						catch (Exception ex)
						{
							if (Prefs.DevMode)
							{
								Log.Error("PawnGraphicSet_ResolveApparelGraphics_Patch: error-inner: " + ex.Message);
							}
						}
						return false;
					}
				}
				catch (Exception ex2)
				{
					if (Prefs.DevMode)
					{
						Log.Error("PawnGraphicSet_ResolveApparelGraphics_Patch: error: " + ex2.Message);
					}
				}
				return true;
			}
			
			public static bool TryGetGraphicApparelAnimal(Apparel apparel, Pawn pawn, PawnKindDef kindDef, out ApparelGraphicRecord rec)
				{
					try
					{
						bool flag = false, flag2 = false;
						string text = "", text2 = "", text3 = "";

						if (apparel.WornGraphicPath.NullOrEmpty())
						{
							rec = new ApparelGraphicRecord(null, null);
							return false;
						}
						flag = (((apparel.def.apparel.tags != null && apparel.def.apparel.tags.Contains("AnimalCUTOUTCOMPLEX")) || apparel.def.apparel.useWornGraphicMask) ? true : false);
						
						if (apparel.def.apparel.tags != null && apparel.def.apparel.tags.Contains("AnimalALLInvisible") && ContentFinder<Texture2D>.Get("Things/Pawn/Animal/Apparel/emptyGear_east", reportFailure: false) != null)
						{
							Graphic graphic = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Animal/Apparel/emptyGear", flag2 ? ShaderDatabase.CutoutComplex : ShaderDatabase.Cutout, apparel.def.graphicData.drawSize, apparel.DrawColor);
							rec = new ApparelGraphicRecord(graphic, apparel);
							return true;
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
									if (ContentFinder<Texture2D>.Get(apparel.WornGraphicPath + text + text2 + suffixToCheck, reportFailure: false) != null)
									{
										if (flag && ContentFinder<Texture2D>.Get(apparel.WornGraphicPath + text + text2 + suffixToCheck+"m", reportFailure: false) != null)
										{
											flag2 = true;
										}
										
										Graphic graphic = GraphicDatabase.Get<Graphic_Multi>(apparel.WornGraphicPath + text + text2, flag2 ? ShaderDatabase.CutoutComplex : ShaderDatabase.Cutout, apparel.def.graphicData.drawSize, apparel.DrawColor);
										rec = new ApparelGraphicRecord(graphic, apparel);
										if (Prefs.DevMode)
										{
											Log.Message(string.Concat("AnimalGear: Special Graphics loaded for Pawn: ", pawn.Name, " - looking for: '", apparel.WornGraphicPath, text, text2, "' Shader:", (apparel.def.graphicData.shaderType != null) ? apparel.def.graphicData.shaderType.ToString() : "null"));
										}
										return true;
									}
									if (!string.IsNullOrEmpty(text3) && ContentFinder<Texture2D>.Get(apparel.WornGraphicPath + text + text3 + suffixToCheck, reportFailure: false) != null)
									{
										if (flag && ContentFinder<Texture2D>.Get(apparel.WornGraphicPath + text + text3 + suffixToCheck+"m", reportFailure: false) != null)
										{
											flag2 = true;
										}
										Graphic graphic = GraphicDatabase.Get<Graphic_Multi>(apparel.WornGraphicPath + text + text3, flag2 ? ShaderDatabase.CutoutComplex : ShaderDatabase.Cutout, apparel.def.graphicData.drawSize, apparel.DrawColor);
										rec = new ApparelGraphicRecord(graphic, apparel);
										if (Prefs.DevMode)
										{
											Log.Message(string.Concat("AnimalGear: Special Graphics loaded for Pawn: ", pawn.Name, " - looking for: '", apparel.WornGraphicPath, text, text3, "' Shader:", (apparel.def.graphicData.shaderType != null) ? apparel.def.graphicData.shaderType.ToString() : "null"));
										}
										return true;
									}
								}
							}
						}
						if (!AnimalGearSettings.AnimalGearFallbackToEmptyTextureEnabled)
						{
							if (flag && ContentFinder<Texture2D>.Get(apparel.WornGraphicPath + text + suffixToCheck+ "m", reportFailure: false) != null)
							{
								flag2 = true;
							}
							Graphic graphic = GraphicDatabase.Get<Graphic_Multi>(apparel.WornGraphicPath + text, flag2 ? ShaderDatabase.CutoutComplex : ShaderDatabase.Cutout, apparel.def.graphicData.drawSize, apparel.DrawColor);
							rec = new ApparelGraphicRecord(graphic, apparel);
							return true;
						}
						if (ContentFinder<Texture2D>.Get(apparel.WornGraphicPath + text + suffixToCheck, reportFailure: false) != null)
						{
							if (flag && ContentFinder<Texture2D>.Get(apparel.WornGraphicPath + text + suffixToCheck+"m", reportFailure: false) != null)
							{
								flag2 = true;
							}
							
							Graphic graphic = GraphicDatabase.Get<Graphic_Multi>(apparel.WornGraphicPath + text, flag2 ? ShaderDatabase.CutoutComplex : ShaderDatabase.Cutout, apparel.def.graphicData.drawSize, apparel.DrawColor);
							rec = new ApparelGraphicRecord(graphic, apparel);
							return true;
						}
						if (ContentFinder<Texture2D>.Get("Things/Pawn/Animal/Apparel/emptyGear"+suffixToCheck, reportFailure: false) != null)
						{
							Graphic graphic = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Animal/Apparel/emptyGear", flag2 ? ShaderDatabase.CutoutComplex : ShaderDatabase.Cutout, apparel.def.graphicData.drawSize, apparel.DrawColor);
							rec = new ApparelGraphicRecord(graphic, apparel);
							if (Prefs.DevMode)
							{
								Log.Message(string.Concat("AnimalGear: !Empty! Graphics loaded for Pawn: ", pawn.Name, " - looking for: '", apparel.WornGraphicPath, text, "(", text2, " / ", text3, ")' Shader:", (apparel.def.graphicData.shaderType != null) ? apparel.def.graphicData.shaderType.ToString() : "null"));
							}
							return true;
						}
					}
					catch (Exception ex)
					{
						if (Prefs.DevMode)
						{
							Log.Error("ApparelGraphicRecordGetterAnimal: TryGetGraphicApparelAnimal: error: " + ex.Message);
						}
					}
					rec = new ApparelGraphicRecord(null, null);
					return false;
				}
		}

		[HarmonyPatch(typeof(PawnGraphicSet), nameof(PawnGraphicSet.ResolveAllGraphics))]
		static class PawnGraphicSet_ResolveAllGraphics_Patch
		{
			static void Postfix(PawnGraphicSet __instance)
			{
				if (AnimalGearHelper.IsAnimalOfAFaction(__instance.pawn))
				{
					__instance.ResolveApparelGraphics();
				}
			}
		}

		[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.CanWearWithoutDroppingAnything))]
		static class Pawn_ApparelTracker_CanWearWithoutDroppingAnything_Patch
		{
			[HarmonyPostfix]
			static bool Postfix(bool __result, Pawn_ApparelTracker __instance, ThingDef apDef)
			{
				if (!__result || !AnimalGearHelper.IsAnimalOfAFaction(__instance.pawn)) return __result;

				ThingOwner<Apparel> thingOwner = Traverse.Create(__instance).Field("wornApparel").GetValue<ThingOwner<Apparel>>();
				
				if (thingOwner == null) return __result;

				for (int i = 0; i < thingOwner.Count; i++)
				{
					if (!CanWearTogetherAnimal(apDef, thingOwner[i].def, __instance.pawn.RaceProps.body))
					{
						return false;
					}
				}
				return __result;
			}

			public static bool CanWearTogetherAnimal(ThingDef A, ThingDef B, BodyDef body)
			{
				if (A.defName == B.defName)
				{
					return false;
				}
				bool flag = false;
				for (int i = 0; i < A.apparel.layers.Count; i++)
				{
					for (int j = 0; j < B.apparel.layers.Count; j++)
					{
						if (A.apparel.layers[i] == B.apparel.layers[j])
						{
							flag = true;
						}
						if (flag)
						{
							break;
						}
					}
					if (flag)
					{
						break;
					}
				}
				if (!flag)
				{
					return true;
				}
				BodyPartGroupDef[] interferingBodyPartGroups = A.apparel.GetInterferingBodyPartGroups(body);
				BodyPartGroupDef[] interferingBodyPartGroups2 = B.apparel.GetInterferingBodyPartGroups(body);
				for (int k = 0; k < interferingBodyPartGroups.Length; k++)
				{
					if (interferingBodyPartGroups2.Contains(interferingBodyPartGroups[k]))
					{
						return false;
					}
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))]
		public static class Pawn_ApparelTracker_Wear_Patch
		{
			public static bool Prefix(Pawn_ApparelTracker __instance, Apparel newApparel, bool dropReplacedApparel = true, bool locked = false)
			{
				if (AnimalGearHelper.IsAnimalOfAFaction(__instance.pawn))
				{
					ThingOwner<Apparel> thingOwner = Traverse.Create(__instance).Field("wornApparel").GetValue<ThingOwner<Apparel>>();
					
					if (thingOwner != null)
					{
						newApparel.DeSpawnOrDeselect();
						if (!ApparelUtility.HasPartsToWear(__instance.pawn, newApparel.def))
						{
							Log.Warning(string.Concat(__instance.pawn.ToString(), " tried to wear ", newApparel, " but he has no body parts required to wear it."));
						}
						else if (CompBiocodable.IsBiocoded(newApparel) && !CompBiocodable.IsBiocodedFor(newApparel, __instance.pawn))
						{
							CompBiocodable compBiocodable = newApparel.TryGetComp<CompBiocodable>();
							Log.Warning(string.Concat(__instance.pawn.ToString(), " tried to wear ", newApparel, " but it is biocoded for ", compBiocodable.CodedPawnLabel.ToString(), " ."));
						}
						else if (!newApparel.PawnCanWear(__instance.pawn, ignoreGender: true))
						{
							Log.Warning(string.Concat(__instance.pawn.ToString(), " tried to wear ", newApparel, " but is not allowed to."));
						}
						else
						{
							for (int num = thingOwner.Count - 1; num >= 0; num--)
							{
								Apparel apparel = thingOwner[num];
								if (!Pawn_ApparelTracker_CanWearWithoutDroppingAnything_Patch.CanWearTogetherAnimal(newApparel.def, apparel.def, __instance.pawn.RaceProps.body))
								{
									if (dropReplacedApparel)
									{
										bool forbid = __instance.pawn.Faction.HostileTo(Faction.OfPlayer);
										if (!__instance.TryDrop(apparel, out var _, __instance.pawn.Position, forbid))
										{
											Log.Error(__instance.pawn.ToString() + " could not drop " + apparel);
											return false;
										}
									}
									else __instance.Remove(apparel);
								}
							}
							if (newApparel.Wearer != null)
							{
								Log.Warning(string.Concat(__instance.pawn.ToString(), " is trying to wear ", newApparel, " but this apparel already has a wearer (", newApparel.Wearer, "). This may or may not cause bugs."));
							}
							thingOwner.TryAdd(newApparel, canMergeWithExistingStacks: false);
							if (!locked)
							{
								return false;
							}
							__instance.Lock(newApparel);
						}
					}
					return false;
				}
				
				return true;
			}
		}

		[HarmonyPatch(typeof(JobGiver_OptimizeApparel), nameof(JobGiver_OptimizeApparel.ApparelScoreRaw))]
		[HarmonyPriority(800)]
		static class JobGiver_OptimizeApparel_ApparelScoreRaw_INIT_Patch
		{
			static void Prefix(Pawn pawn)
			{
				JobGiver_OptimizeApparel_TryGiveJob_Patch.isApparelScoreRawActive = true;
				JobGiver_OptimizeApparel_TryGiveJob_Patch.IsAnimalCacheRefresh(pawn, skipWarmth: true);	
			}

			public static void Postfix()
			{
				JobGiver_OptimizeApparel_TryGiveJob_Patch.isApparelScoreRawActive = false;
			}
		}

		[HarmonyPatch(typeof(JobGiver_OptimizeApparel), nameof(JobGiver_OptimizeApparel.ApparelScoreRaw))]
		[HarmonyAfter(new string[] { "rimworld.outfitted" })]
		static class JobGiver_OptimizeApparel_ApparelScoreRaw_Patch
		{
			static bool Prefix(Pawn pawn, Apparel ap, ref float __result)
			{
				JobGiver_OptimizeApparel_TryGiveJob_Patch.IsAnimalCacheRefresh(pawn);
				if (JobGiver_OptimizeApparel_TryGiveJob_Patch.currentIsAnimalOfAFaction)
				{
					__result = ApparelScoreRawAnimal(pawn, ap, JobGiver_OptimizeApparel_TryGiveJob_Patch.currentNeededWarmth);
					return false;
				}
				
				return true;
			}

			static float ApparelScoreRawAnimal(Pawn pawn, Apparel ap, NeededWarmth neededWarmth)
			{
				SimpleCurve value = Traverse.Create<JobGiver_OptimizeApparel>().Field("InsulationColdScoreFactorCurve_NeedWarm").GetValue<SimpleCurve>();
				SimpleCurve value2 = Traverse.Create<JobGiver_OptimizeApparel>().Field("HitPointsPercentScoreFactorCurve").GetValue<SimpleCurve>();
				if (!ap.PawnCanWear(pawn, ignoreGender: true))
				{
					return -10f;
				}
				if (ap.def.apparel.blocksVision)
				{
					return -10f;
				}
				if (ap.def.apparel.mechanitorApparel && pawn.mechanitor == null)
				{
					return -10f;
				}
				float num = 0.1f + ap.def.apparel.scoreOffset + (ap.GetStatValue(StatDefOf.ArmorRating_Sharp) + ap.GetStatValue(StatDefOf.ArmorRating_Blunt));
				if (ap.def.useHitPoints)
				{
					float x = (float)ap.HitPoints / (float)ap.MaxHitPoints;
					num *= value2.Evaluate(x);
				}
				float num2 = num + ap.GetSpecialApparelScoreOffset();
				float num3 = 1f;
				if (neededWarmth == NeededWarmth.Warm)
				{
					float statValue = ap.GetStatValue(StatDefOf.Insulation_Cold);
					num3 *= value.Evaluate(statValue);
				}
				float num4 = num2 * num3;
				if (pawn != null && !ap.def.apparel.CorrectGenderForWearing(pawn.gender))
				{
					num4 *= 0.01f;
				}
				return num4;
			}
		}

		[HarmonyPatch(typeof(JobGiver_OptimizeApparel), nameof(JobGiver_OptimizeApparel.TryGiveJob))]
		[HarmonyPriority(800)]
		public static class JobGiver_OptimizeApparel_TryGiveJob_Patch
		{
			public static bool isOptimizeApparelJobActive,
				isApparelScoreRawActive,
				checkIfItIsAnimal,
				currentIsAnimal,
				currentIsAnimalOfColony,
				currentIsAnimalOfAFaction;

			public static NeededWarmth currentNeededWarmth;

			static void Prefix()
			{
				isOptimizeApparelJobActive = true;
				checkIfItIsAnimal = true;
				currentIsAnimal = false;
				currentIsAnimalOfColony = false;
				currentIsAnimalOfAFaction = false;	
			}

			[HarmonyPriority(200)]
			public static void Postfix()
			{
				isOptimizeApparelJobActive = false;
				checkIfItIsAnimal = true;
				currentIsAnimal = false;
				currentIsAnimalOfColony = false;
				currentIsAnimalOfAFaction = false;
			}

			public static void IsAnimalCacheRefresh(Pawn pawn)
			{
				IsAnimalCacheRefresh(pawn, skipWarmth: false);
			}

			public static void IsAnimalCacheRefresh(Pawn pawn, bool skipWarmth)
			{
				if (checkIfItIsAnimal)
				{
					currentIsAnimal = AnimalGearHelper.IsAnimal(pawn);
					if (currentIsAnimal)
					{
						currentIsAnimalOfColony = AnimalGearHelper.IsAnimalOfColony(pawn);
						currentIsAnimalOfAFaction = AnimalGearHelper.IsAnimalOfAFaction(pawn);
					}
					else
					{
						currentIsAnimalOfColony = false;
						currentIsAnimalOfAFaction = false;
					}
					if (!skipWarmth || isOptimizeApparelJobActive)
					{
						currentNeededWarmth = Traverse.Create<JobGiver_OptimizeApparel>().Field("neededWarmth").GetValue<NeededWarmth>();
					}
					if (isOptimizeApparelJobActive)
					{
						checkIfItIsAnimal = false;
					}
				}
			}

			public static void LogStackFunctions()
			{
				string text = "", text2 = "";
				
				MethodBase method = new StackFrame(1).GetMethod();
				text = method.Name;
				text2 = method.Module.Assembly.FullName;
				if (Prefs.DevMode)
				{
					Log.Message("S1: " + text2 + " - " + text);
				}
				
				MethodBase method2 = new StackFrame(2).GetMethod();
				text = method2.Name;
				text2 = method2.Module.Assembly.FullName;
				if (Prefs.DevMode)
				{
					Log.Message("S2: " + text2 + " - " + text);
				}
				
				MethodBase method3 = new StackFrame(3).GetMethod();
				text = method3.Name;
				text2 = method3.Module.Assembly.FullName;
				if (Prefs.DevMode)
				{
					Log.Message("S3: " + text2 + " - " + text);
				}
				
				MethodBase method4 = new StackFrame(4).GetMethod();
				text = method4.Name;
				text2 = method4.Module.Assembly.FullName;
				if (Prefs.DevMode)
				{
					Log.Message("S4: " + text2 + " - " + text);
				}
				
				MethodBase method5 = new StackFrame(5).GetMethod();
				text = method5.Name;
				text2 = method5.Module.Assembly.FullName;
				if (Prefs.DevMode)
				{
					Log.Message("S5: " + text2 + " - " + text);
				}
				
				MethodBase method6 = new StackFrame(6).GetMethod();
				text = method6.Name;
				text2 = method6.Module.Assembly.FullName;
				if (Prefs.DevMode)
				{
					Log.Message("S6: " + text2 + " - " + text);
				}
				
				MethodBase method7 = new StackFrame(7).GetMethod();
				text = method7.Name;
				text2 = method7.Module.Assembly.FullName;
				if (Prefs.DevMode)
				{
					Log.Message("S7: " + text2 + " - " + text);
				}
				
			}
		}

		[HarmonyPatch(typeof(ApparelUtility), nameof(ApparelUtility.HasPartsToWear))]
		public static class ApparelUtility_HasPartsToWear_Patch
		{
			public static bool Postfix(bool __result, Pawn p, ThingDef apparel)
			{
				if (!__result) return __result;

				if (apparel != null && apparel.IsApparel && apparel.apparel.tags != null && apparel.apparel.tags.Count > 0 && apparel.apparel.tags.Contains("Animal"))
				{
					if (AnimalGearHelper.IsAnimal(p))
					{
						if (p.kindDef != null && apparel.apparel.tags.Contains(p.kindDef.defName.ToString()))
						{
							return true;
						}
						else if (apparel.apparel.tags.Contains("AnimalALL"))
						{
							return true;
						}
						return false;
					}
					return false;
				}
				else if (apparel != null && apparel.IsApparel && apparel.apparel.tags != null && apparel.apparel.tags.Count > 0 && apparel.apparel.tags.Contains("AnimalCompatible"))
				{
					if (AnimalGearHelper.IsAnimal(p))
					{
						if (p.kindDef != null && apparel.apparel.tags.Contains(p.kindDef.defName.ToString()))
						{
							return true;
						}
						else if (apparel.apparel.tags.Contains("AnimalALL"))
						{
							return true;
						}
						return false;
					}
				}
				else if (AnimalGearHelper.IsAnimal(p))
				{
					return false;
				}
				return __result;
			}
		}

		[HarmonyPatch(typeof(ApparelUtility), nameof(ApparelUtility.CanWearTogether))]
		public static class ApparelUtility_CanWearTogether_Patch
		{
			public static bool Prefix(ThingDef A, ThingDef B, BodyDef body, ref bool __result)
			{
				if ((JobGiver_OptimizeApparel_TryGiveJob_Patch.isOptimizeApparelJobActive || 
					JobGiver_OptimizeApparel_TryGiveJob_Patch.isApparelScoreRawActive) &&
					JobGiver_OptimizeApparel_TryGiveJob_Patch.currentIsAnimal)
				{
					__result = Pawn_ApparelTracker_CanWearWithoutDroppingAnything_Patch.CanWearTogetherAnimal(A, B, body);
					return false;
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderAsPack))]
		static class PawnRenderer_RenderAsPack_Patch
		{
			static bool Prepare()
			{
				return false; //Temporarily disable patch
			}
			static bool Postfix(bool __result, Apparel apparel)
			{
				bool flag = false;
				Pawn pawn = null;
				if (__result)
				{
					if (PawnRenderer_RenderPawnAt_Transpiler_Patch.renderActiveAnimalGear)
					{
						flag = true;
						pawn = PawnRenderer_RenderPawnAt_Transpiler_Patch.currentPawn;
					}
					else if (ModRPGStyleInventory_ON && RPGInventory_FillTab_Patch.cachedAnimalGearRPGInventoryAnimalCompatibilityEnabled && RPGInventory_FillTab_Patch.currentIsAnimalOfAFactionActive)
					{
						flag = true;
						pawn = RPGInventory_FillTab_Patch.currentPawn;
					}
					if (flag && apparel.def.apparel.wornGraphicData != null && (pawn == null || pawn.story == null))
					{
						return false;
					}
				}
				return __result;
			}
		}

		[HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
		public static class LoadTraindableDef
		{
			static void Postfix(Map __instance)
			{
				if (Prefs.DevMode)
				{
					Log.Message("AnimalGear("+ModVersion+"): Active Mods: "+ModNameGiddyUp+": " + ModGiddyUp_ON.ToString() + 
						"(P:" + ModGiddyUp_Patched + ") "+ModNameRPGStyleInventory+": " + ModRPGStyleInventory_ON.ToString() + 
						"(P:" + ModRPGStyleInventory_Patched + " Option:" + ((AnimalGearSettings.AnimalGearRPGInventoryAnimalCompatibilityEnabled) ? 
						
						(AnimalGearSettings.AnimalGearRPGInventoryAnimalCompatibilityEnabled).ToString() : "null") + ") "+ModNameAlphaAnimals+": " + ModAlphaAnimals_ON.ToString());
				}
				
				FloatMenuMakerMap_AddDraftedOrders_Patch.cachedTrainableDefHaul = DefDatabase<TrainableDef>.GetNamed("Haul", errorOnFail: false);
				
				
				if (__instance == null || __instance.mapPawns == null)
				{
					return;
				}
				List<ThingDef> list = null;
				list = DefDatabase<ThingDef>.AllDefsListForReading.Where((ThingDef def) => def.IsApparel && def.apparel.tags != null && def.apparel.tags.Count > 0 && (def.apparel.tags.Contains("Animal") || def.apparel.tags.Contains("AnimalCompatible"))).ToList();
				foreach (Pawn item in __instance.mapPawns.PawnsInFaction(Faction.OfPlayer))
				{
					if (item != null)
					{
						AnimalGearHelper.CheckAnimalAndReplaceLegacyGear(item, list, loading: true);
					}
				}
			}
		}

		[HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.AddDraftedOrders))]
		public static class FloatMenuMakerMap_AddDraftedOrders_Patch
		{
			public static TrainableDef cachedTrainableDefHaul;

			static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts, bool suppressAutoTakeableGoto)
			{
				if (pawn == null || !AnimalGearHelper.IsAnimalOfColony(pawn) || pawn.apparel == null)
				{
					return;
				}
				IntVec3 c = IntVec3.FromVector3(clickPos);
				c.GetThingList(pawn.Map);
				Apparel apparel = pawn.Map.thingGrid.ThingAt<Apparel>(c);
				if (apparel != null)
				{
					string key = "CannotWear";
					string key2 = "ForceWear";
					if (apparel.def.apparel.LastLayer.IsUtilityLayer)
					{
						key = "CannotEquipApparel";
						key2 = "ForceEquipApparel";
					}
					if (AnimalGearSettings.AnimalGearDraftedForceWearEnabled)
					{
						string cantReason;
						FloatMenuOption item = ((!IsAnimalTrainedToHaul(pawn) && AnimalGearSettings.AnimalGearDraftedForceWearOnlyCarryTrainedEnabled) ? new FloatMenuOption(key.Translate(apparel.Label, apparel) + ": " + "AG_NotTrained".Translate().CapitalizeFirst(), null) : ((!pawn.CanReach(apparel, PathEndMode.ClosestTouch, Danger.Deadly)) ? new FloatMenuOption(key.Translate(apparel.Label, apparel) + ": " + "NoPath".Translate().CapitalizeFirst(), null) : (apparel.IsBurning() ? new FloatMenuOption(key.Translate(apparel.Label, apparel) + ": " + "Burning".Translate(), null) : (pawn.apparel.WouldReplaceLockedApparel(apparel) ? new FloatMenuOption(key.Translate(apparel.Label, apparel) + ": " + "WouldReplaceLockedApparel".Translate().CapitalizeFirst(), null) : ((!ApparelUtility.HasPartsToWear(pawn, apparel.def)) ? new FloatMenuOption(key.Translate(apparel.Label, apparel) + ": " + "CannotWearBecauseOfMissingBodyParts".Translate().CapitalizeFirst(), null) : (EquipmentUtility.CanEquip(apparel, pawn, out cantReason) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(key2.Translate(apparel.LabelShort, apparel), delegate
						{
							apparel.SetForbidden(value: false);
                            Job job3 = JobMaker.MakeJob(RimWorld.JobDefOf.Wear, apparel);
							pawn.jobs.TryTakeOrderedJob(job3, JobTag.Misc);
						}, MenuOptionPriority.High), pawn, apparel) : new FloatMenuOption(key.Translate(apparel.Label, apparel) + ": " + cantReason, null)))))));
						opts.Add(item);
					}
				}
				Building_StylingStation station = pawn.Map.thingGrid.ThingAt<Building_StylingStation>(c);
				if (station == null || !ModLister.IdeologyInstalled || !AnimalGearSettings.AnimalGearDraftedForceWearEnabled)
				{
					return;
				}
				if (IsAnimalTrainedToHaul(pawn) || !AnimalGearSettings.AnimalGearDraftedForceWearOnlyCarryTrainedEnabled)
				{
					FloatMenuOption item2 = (pawn.CanReach(station, PathEndMode.OnCell, Danger.Deadly) ? new FloatMenuOption("ChangeStyle".Translate().CapitalizeFirst(), delegate
					{
						pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.AnimalGearOpenStylingStationDialog, station), JobTag.Misc);
					}) : new FloatMenuOption("CannotUseReason".Translate("NoPath".Translate().CapitalizeFirst()), null));
					opts.Add(item2);
					if (JobGiver_OptimizeApparel.TryCreateRecolorJob(pawn, out var _, dryRun: true))
					{
						item2 = new FloatMenuOption("RecolorApparel".Translate().CapitalizeFirst(), delegate
						{
							JobGiver_OptimizeApparel.TryCreateRecolorJob(pawn, out var job2);
							pawn.jobs.TryTakeOrderedJob(job2, JobTag.Misc);
						});
						opts.Add(item2);
					}
				}
				else
				{
					FloatMenuOption item2 = new FloatMenuOption("CannotUseReason".Translate("AG_NotTrained".Translate().CapitalizeFirst()), null);
					opts.Add(item2);
				}
			}

			static bool IsAnimalTrainedToHaul(Pawn pawn)
			{
				return pawn.RaceProps.Animal && cachedTrainableDefHaul != null && (pawn.training?.HasLearned(cachedTrainableDefHaul) ?? false);
			}
		}

		[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnInternal))]
		[HarmonyPatch(new Type[]
		{
			typeof(Vector3),
			typeof(float),
			typeof(bool),
			typeof(Rot4),
			typeof(RotDrawMode),
			typeof(PawnRenderFlags)
		})]
		public static class PawnRenderer_RenderPawnInternal_Patch
		{
			public static void Prefix(ref PawnRenderFlags flags)
			{
				if (PawnRenderer_RenderPawnAt_Transpiler_Patch.renderActiveAnimalGear && 
					(PawnRenderer_RenderPawnAt_Transpiler_Patch.renderMode == AnimalGearSettings.AnimalGearRenderModeHandleEnum._FallbackRenderPawnInternal || 
					(PawnRenderer_RenderPawnAt_Transpiler_Patch.renderMode == AnimalGearSettings.AnimalGearRenderModeHandleEnum._AutoMixed && 
					PawnRenderer_RenderPawnAt_Transpiler_Patch.renderFarAway)) && 
					PawnRenderer_RenderPawnAt_Transpiler_Patch.currentPawn != null)
				{
					flags |= PawnRenderFlags.Clothes;
				}
			}
		}

		[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
		[HarmonyPatch(new Type[]
		{
			typeof(Vector3),
			typeof(Rot4?),
			typeof(bool)
		})]
		public static class PawnRenderer_RenderPawnAt_Transpiler_Patch
		{
			public static bool renderActiveAnimalGear,
				resetStoryTrackerAnimalGear,
				renderFarAway;
			public static Pawn currentPawn;
			public static float currentBaseBodySize = 1f,
				currentZoomRequested = 1f;
			public static AnimalGearSettings.AnimalGearRenderModeHandleEnum renderMode;


			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				bool flag = false;
				List<CodeInstruction> list = new List<CodeInstruction>(instructions);
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].opcode == OpCodes.Callvirt && list[i].operand.ToString().EndsWith("get_RaceProps()") && i + 1 < list.Count && list[i + 1].opcode == OpCodes.Callvirt && list[i + 1].operand.ToString().EndsWith("get_Humanlike()"))
					{
						list[i].opcode = OpCodes.Nop;
						list[i + 1].opcode = OpCodes.Call;
						list[i + 1].operand = AccessTools.Method(typeof(PawnRenderer_RenderPawnAt_Transpiler_Patch), nameof(PawnRenderer_RenderPawnAt_Transpiler_Patch.GetHumanlikeModdedAnimal));
						flag = true;
						break;
					}
				}
				if (flag)
				{
					if (Prefs.DevMode)
					{
						Log.Message("AnimalGear: Transpiler(RenderPawnAt): Patched!");
					}
				}
				else if (Prefs.DevMode)
				{
					Log.Error("AnimalGear: Transpiler(RenderPawnAt): Not patched! (maybe another mod messed things up?)");
				}
				return list.AsEnumerable();
			}

			static bool GetHumanlikeModdedAnimal(Pawn pawn)
			{
				if (renderActiveAnimalGear && 
					(renderMode == AnimalGearSettings.AnimalGearRenderModeHandleEnum._NewMeshMode || 
					(renderMode == AnimalGearSettings.AnimalGearRenderModeHandleEnum._AutoMixed && !renderFarAway)))
				{
					return true;
				}
				return pawn.def.race?.intelligence == Intelligence.Humanlike;
				
			}

			static void Prefix(PawnRenderer __instance)
			{
				Pawn pawn = __instance.pawn;

				renderActiveAnimalGear = false;
				resetStoryTrackerAnimalGear = false;
				currentPawn = null;
				currentBaseBodySize = 1f;
				currentZoomRequested = -1f;
				if (pawn == null || !AnimalGearHelper.IsAnimalOfAFaction(pawn))
				{
					return;
				}

				renderActiveAnimalGear = (pawn.apparel != null && pawn.apparel.wornApparel.Count > 0);
				if (renderActiveAnimalGear)
				{
					currentPawn = pawn;
					currentBaseBodySize = pawn.RaceProps.baseBodySize;
					overrideCurrentBaseBodySize(pawn);
					
					renderFarAway = (currentBaseBodySize <= 1f && Current.cameraDriverInt.CurrentZoom < CameraZoomRange.Far);
				}
			}

			static void Postfix(PawnRenderer __instance)
			{
				Pawn pawn = __instance.pawn;

				if (renderActiveAnimalGear && resetStoryTrackerAnimalGear && currentPawn != null && currentPawn.story != null && AnimalGearHelper.IsAnimalOfAFaction(currentPawn))
				{
					currentPawn.story = null;
				}
				renderActiveAnimalGear = false;
				resetStoryTrackerAnimalGear = false;
				currentPawn = null;
				currentBaseBodySize = 1f;
				currentZoomRequested = -1f;
			}

			public static void overrideCurrentBaseBodySize(Pawn pawn)
			{				
				if (!ModAlphaAnimals_ON) return;

				string text = "";
				text = pawn.def.defName;
				if (currentBaseBodySize < 2f && text.StartsWith("AA_"))
				{
					//If/else faster than switch for only a few string checks
					if (text == "AA_RoyalAve") currentBaseBodySize = 2.4f;
					else if (text == "AA_FrostAve") currentBaseBodySize = 2.3f;
					else if (text == "AA_MeadowAve") currentBaseBodySize = 2.2f;
					else if (text == "AA_NightAve") currentBaseBodySize = 2.1f;
					else if (text == "AA_DesertAve") currentBaseBodySize = 2f;
				}
			}
		}

		[HarmonyPatch]
		static class GiddyUp_CompOverlay_PostDraw_Transpiler_Patch
		{
			static MethodBase target;
			static bool Prepare()
			{
				var type = AccessTools.TypeByName("GiddyUp.CompOverlay");
				if (type == null) return false;
				target = AccessTools.Method(type, "PostDraw");
				if (target == null) return false;
				return true;
			}
			static MethodBase TargetMethod()
			{
				return target;
			}
			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				AnimalGearHarmony.ModGiddyUp_Patched++;
				return instructions.MethodReplacer(AccessTools.Method(typeof(Graphic), nameof(Graphic.Draw)),
				AccessTools.Method(typeof(AnimalGearHarmony), nameof(DrawCompOverlay)));
			}
		}

		public static void DrawCompOverlay(this Graphic graphic, Vector3 loc, Rot4 rot, Thing thing, float extraRotation)
		{
			bool flag = false;
			Pawn pawn;
			
			pawn = (Pawn)thing;
			if (pawn.apparel != null && pawn.apparel.WornApparelCount > 0)
			{
				for (int i = 0; i < pawn.apparel.WornApparel.Count; i++)
				{
					
					Apparel apparel = pawn.apparel.WornApparel[i];
					if (apparel.def.apparel.tags != null && apparel.def.apparel.tags.Contains("AnimalHideGiddyUpOverlay"))
					{
						flag = true;
					}
				}
			}
			
			if (!flag) graphic.Draw(loc, rot, thing, extraRotation);
			pawn = (Pawn)thing;
			if (pawn.apparel != null && pawn.apparel.WornApparelCount > 0 && pawn.Drawer != null && pawn.Drawer.renderer != null && pawn.Drawer.renderer.graphics != null)
			{
				List<ApparelGraphicRecord> apparelGraphics = pawn.Drawer.renderer.graphics.apparelGraphics;
				Vector3 loc2 = new Vector3(loc.x, loc.y + 0.046875f, loc.z);
				for (int j = 0; j < apparelGraphics.Count; j++)
				{
					Apparel sourceApparel = apparelGraphics[j].sourceApparel;
					if (sourceApparel.def.apparel.tags != null && sourceApparel.def.apparel.tags.Contains("AnimalDrawOverGiddyUpOverlay"))
					{
						Quaternion quat = Quaternion.AngleAxis(extraRotation, Vector3.up);
						ApparelGraphicRecord apparelGraphicRecord = apparelGraphics[j];
						bool flag2 = false;
						Mesh mesh = (!pawn.RaceProps.Humanlike) ? pawn.Drawer.renderer.graphics.nakedGraphic.MeshAt(rot) : MeshPool.humanlikeBodySet.MeshAt(rot);
						Material mat = OverrideMaterialIfNeededAnimal(pawn.Drawer.renderer, apparelGraphicRecord.graphic.MatAt(rot, null), pawn, flag2);
						GenDraw.DrawMeshNowOrLater(mesh, loc2, quat, mat, flag2);
						loc2.y += 0.0030612245f;
					}
				}
			}

			Material OverrideMaterialIfNeededAnimal(PawnRenderer renderer, Material original, Pawn pawn, bool portrait = false)
			{
				Material baseMat = (!portrait && pawn.IsInvisible()) ? InvisibilityMatPool.GetInvisibleMat(original) : original;
				return renderer.graphics.flasher.GetDamagedMat(baseMat);
			}
		}

		[HarmonyPatch(typeof(GlobalTextureAtlasManager), nameof(GlobalTextureAtlasManager.TryGetPawnFrameSet))]
		[HarmonyPriority(700)]
		static class GlobalTextureAtlasManager_TryGetPawnFrameSet_Patch
		{
			static List<PawnTextureAtlas> animalTextureAtlases256 = new List<PawnTextureAtlas>(), animalTextureAtlases512 = new List<PawnTextureAtlas>();

			static bool Prefix(Pawn pawn, ref PawnTextureAtlasFrameSet frameSet, ref bool createdNew, bool allowCreatingNew, ref bool __result)
			{
				if (pawn != null && (PawnRenderer_RenderPawnAt_Transpiler_Patch.renderActiveAnimalGear || AnimalGearHelper.IsAnimalOfAFaction(pawn)))
				{
					float num = 1f;
					if (PawnRenderer_RenderPawnAt_Transpiler_Patch.renderActiveAnimalGear)
					{
						num = PawnRenderer_RenderPawnAt_Transpiler_Patch.currentBaseBodySize;
					}
					else
					{
						num = pawn.RaceProps.baseBodySize;
						PawnRenderer_RenderPawnAt_Transpiler_Patch.overrideCurrentBaseBodySize(pawn);
					}
					if (num <= 1f)
					{
						return true;
					}
					if (num < 2f)
					{
						foreach (PawnTextureAtlas item in animalTextureAtlases256)
						{
							if (item.TryGetFrameSet(pawn, out frameSet, out createdNew))
							{
								__result = true;
								return false;
							}
						}
						if (allowCreatingNew)
						{
							PawnTextureAtlas pawnTextureAtlas = new PawnTextureAtlas();
							reeinitPawnTextureAtlasLarger(ref pawnTextureAtlas, 256);
							animalTextureAtlases256.Add(pawnTextureAtlas);
							__result = pawnTextureAtlas.TryGetFrameSet(pawn, out frameSet, out createdNew);
							return false;
						}
					}
					else
					{
						foreach (PawnTextureAtlas item2 in animalTextureAtlases512)
						{
							if (item2.TryGetFrameSet(pawn, out frameSet, out createdNew))
							{
								__result = true;
								return false;
							}
						}
						if (allowCreatingNew)
						{
							PawnTextureAtlas pawnTextureAtlas2 = new PawnTextureAtlas();
							reeinitPawnTextureAtlasLarger(ref pawnTextureAtlas2, 512);
							animalTextureAtlases512.Add(pawnTextureAtlas2);
							__result = pawnTextureAtlas2.TryGetFrameSet(pawn, out frameSet, out createdNew);
							return false;
						}
					}
					createdNew = false;
					frameSet = null;
					__result = false;
					return false;
				}
				return true;
			}

			static void reeinitPawnTextureAtlasLarger(ref PawnTextureAtlas pawnTextureAtlas, int newSize)
			{
				RenderTexture renderTexture = null;
				List<PawnTextureAtlasFrameSet> list = null;
				
				renderTexture = Traverse.Create(pawnTextureAtlas).Field("texture").GetValue<RenderTexture>();
				list = Traverse.Create(pawnTextureAtlas).Field("freeFrameSets").GetValue<List<PawnTextureAtlasFrameSet>>();
				
				int num = 2048, num2 = 2048;
				float num3 = 0.0625f;
				switch (newSize)
				{
					case 256:
						num3 = 0.125f;
						break;
					case 512:
						num3 = 0.25f;
						break;
					default:
						num3 = 1f / (2048f / (float)newSize);
						break;
					case 128:
						break;
				}
				renderTexture = new RenderTexture(num, num2, 24, RenderTextureFormat.ARGB32, 0);
				list = new List<PawnTextureAtlasFrameSet>();
				List<Rect> list2 = new List<Rect>();
				for (int i = 0; i < num; i += newSize)
				{
					for (int j = 0; j < num2; j += newSize)
					{
						list2.Add(new Rect((float)i / (float)num, (float)j / (float)num2, num3, num3));
					}
				}
				while (list2.Count >= 8)
				{
					PawnTextureAtlasFrameSet pawnTextureAtlasFrameSet = new PawnTextureAtlasFrameSet();
					pawnTextureAtlasFrameSet.uvRects = new Rect[8]
					{
						list2.Pop(),
						list2.Pop(),
						list2.Pop(),
						list2.Pop(),
						list2.Pop(),
						list2.Pop(),
						list2.Pop(),
						list2.Pop()
					};
					pawnTextureAtlasFrameSet.meshes = pawnTextureAtlasFrameSet.uvRects.Select((Rect u) => TextureAtlasHelper.CreateMeshForUV(u)).ToArray();
					pawnTextureAtlasFrameSet.atlas = renderTexture;
					list.Add(pawnTextureAtlasFrameSet);
				}
				
				Traverse.Create(pawnTextureAtlas).Field("texture").SetValue(renderTexture);
				Traverse.Create(pawnTextureAtlas).Field("freeFrameSets").SetValue(list);
			}
		}

		[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.GetBlitMeshUpdatedFrame))]
		[HarmonyPatch(new Type[]
		{
			typeof(PawnTextureAtlasFrameSet),
			typeof(Rot4),
			typeof(PawnDrawMode)
		})]
		[HarmonyPriority(700)]
		public static class PawnRenderer_GetBlitMeshUpdatedFrame_Patch
		{
			static bool Prefix(PawnTextureAtlasFrameSet frameSet, Rot4 rotation, PawnDrawMode drawMode, ref Mesh __result)
			{	
				if (PawnRenderer_RenderPawnAt_Transpiler_Patch.renderActiveAnimalGear && 
				(PawnRenderer_RenderPawnAt_Transpiler_Patch.renderMode == AnimalGearSettings.AnimalGearRenderModeHandleEnum._NewMeshMode || 
				(PawnRenderer_RenderPawnAt_Transpiler_Patch.renderMode == AnimalGearSettings.AnimalGearRenderModeHandleEnum._AutoMixed && !PawnRenderer_RenderPawnAt_Transpiler_Patch.renderFarAway)) && 
				PawnRenderer_RenderPawnAt_Transpiler_Patch.currentPawn != null)
				{
					PawnRenderer_RenderPawnAt_Transpiler_Patch.resetStoryTrackerAnimalGear = false;
					if (PawnRenderer_RenderPawnAt_Transpiler_Patch.currentPawn.story == null)
					{
						PawnRenderer_RenderPawnAt_Transpiler_Patch.currentPawn.story = new Pawn_StoryTracker(PawnRenderer_RenderPawnAt_Transpiler_Patch.currentPawn);
						PawnRenderer_RenderPawnAt_Transpiler_Patch.resetStoryTrackerAnimalGear = true;
					}
					
					
					int index = frameSet.GetIndex(rotation, drawMode);
					if (frameSet.isDirty[index])
					{
						Find.PawnCacheCamera.rect = frameSet.uvRects[index];
						PawnRenderer_RenderPawnAt_Transpiler_Patch.currentZoomRequested = ((PawnRenderer_RenderPawnAt_Transpiler_Patch.currentBaseBodySize > 1f) ? (1f / PawnRenderer_RenderPawnAt_Transpiler_Patch.currentBaseBodySize) : 1f);
						Find.PawnCacheRenderer.RenderPawn(PawnRenderer_RenderPawnAt_Transpiler_Patch.currentPawn, frameSet.atlas, Vector3.zero, PawnRenderer_RenderPawnAt_Transpiler_Patch.currentZoomRequested, 0f, rotation, renderHead: true, drawMode == PawnDrawMode.BodyAndHead);
						Find.PawnCacheCamera.rect = new Rect(0f, 0f, 1f, 1f);
						if (PawnRenderer_RenderPawnAt_Transpiler_Patch.currentBaseBodySize > 1f)
						{
							frameSet.meshes[index] = TextureAtlasHelper.CreateMeshForUV(frameSet.uvRects[index], PawnRenderer_RenderPawnAt_Transpiler_Patch.currentBaseBodySize);
						}
						frameSet.isDirty[index] = false;
					}
					__result = frameSet.meshes[index];
					
					if (PawnRenderer_RenderPawnAt_Transpiler_Patch.resetStoryTrackerAnimalGear)
					{
						PawnRenderer_RenderPawnAt_Transpiler_Patch.currentPawn.story = null;
						PawnRenderer_RenderPawnAt_Transpiler_Patch.resetStoryTrackerAnimalGear = false;
					}
					return false;
				}
				
				return true;
			}
		}

		[HarmonyPatch(typeof(PawnCacheRenderer), nameof(PawnCacheRenderer.RenderPawn))]
		[HarmonyAfter(new string[] { "AlienRace", "FacialStuff" })]
		[HarmonyPriority(200)]
		static class PawnCacheRenderer_RenderPawn_Patch
		{
			static bool Prefix(ref float cameraZoom)
			{
				if (
					PawnRenderer_RenderPawnAt_Transpiler_Patch.renderActiveAnimalGear &&
					(PawnRenderer_RenderPawnAt_Transpiler_Patch.renderMode == AnimalGearSettings.AnimalGearRenderModeHandleEnum._NewMeshMode || 
					(PawnRenderer_RenderPawnAt_Transpiler_Patch.renderMode == AnimalGearSettings.AnimalGearRenderModeHandleEnum._AutoMixed && 
					!PawnRenderer_RenderPawnAt_Transpiler_Patch.renderFarAway)) && 
					PawnRenderer_RenderPawnAt_Transpiler_Patch.currentPawn != null && 
					PawnRenderer_RenderPawnAt_Transpiler_Patch.currentBaseBodySize > 1f && 
					PawnRenderer_RenderPawnAt_Transpiler_Patch.currentZoomRequested > 0f)
				{
					cameraZoom = PawnRenderer_RenderPawnAt_Transpiler_Patch.currentZoomRequested;
				}
				
				return true;
			}
		}

		[HarmonyPatch(typeof(ThoughtUtility), nameof(ThoughtUtility.CanGetThought))]
		static class ThoughtUtility_CanGetThought_PatchOutf
		{
			static readonly int humanLeatherApparelSad = ThoughtDefOf.HumanLeatherApparelSad.shortHash,
				humanLeatherApparelHappy = ThoughtDefOf.HumanLeatherApparelHappy.shortHash,
				deadMansApparel = ThoughtDefOf.DeadMansApparel.shortHash;
			static bool Prefix(Pawn pawn, ThoughtDef def, ref bool __result)
			{
				var hash = def.shortHash;
				if (hash == humanLeatherApparelSad || hash == humanLeatherApparelHappy || hash == deadMansApparel)
				{
					if (AnimalGearHelper.IsAnimalOfAFaction(pawn))
					{
						__result = false;
						return false;
					}
				}
				
				return true;
			}
		}

		public static void InitModOn()
		{
			try
			{
				var list = LoadedModManager.RunningModsListForReading;
				int length = list.Count;
				for (int i = 0; i < length; i++)
				{
                	string name = list[i].Name?.ToLower() ?? "NULL";
                	if (name.StartsWith(ModNameGiddyUp))
                	{
						ModGiddyUp_ON = true;
						ModGiddyUp_GiddyUp_CompOverlay = AccessTools.TypeByName("GiddyUp.CompOverlay");
						ModGiddyUp_GiddyUp_CompOverlayFound = ModGiddyUp_GiddyUp_CompOverlay == null ? false : true;
					}
					else if (name.StartsWith(ModNameAlphaAnimals))
                	{
						ModAlphaAnimals_ON = true;
					}
					else if (name.StartsWith(ModNameRPGStyleInventory))
                	{
						ModRPGStyleInventory_ON = true;
						ModRPGStyleInventory_RPG_GearTab = AccessTools.TypeByName(ModNameRPGStyleInventoryDLL + ".Sandy_Detailed_RPG_GearTab");
						ModRPGStyleInventory_RPG_GearTabFound = ModRPGStyleInventory_RPG_GearTab == null ? false : true;
					}
				}
			}
			catch (Exception ex)
			{
				if (Prefs.DevMode)
				{
					Log.Error("AnimalGear: InitModOn: error: " + ex.Message);
				}
			}
		}
	}
}
