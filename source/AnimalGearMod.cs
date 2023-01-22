using Verse;
using HarmonyLib;
using System;
using UnityEngine;
using static AnimalGear.AnimalGearSettings;

namespace AnimalGear
{
	internal class AnimalGearMod : Mod
	{
		public AnimalGearMod(ModContentPack content) : base(content)
		{
			base.GetSettings<AnimalGearSettings>();
			AnimalGearHarmony.InitModOn();
			new Harmony("AnimalGear").PatchAll();
			AnimalGearHarmony.PawnRenderer_RenderPawnAt_Transpiler_Patch.renderMode = AnimalGearRenderMode;
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Standard options = new Listing_Standard();
			options.Begin(inRect);
			options.Label("AnimalGearRenderMode_Title".Translate(), -1f, null);
			if (options.RadioButton("AnimalGearRenderMode.AutoMixed".Translate(), AnimalGearRenderMode == AnimalGearRenderModeHandleEnum._AutoMixed, 0f, "AnimalGearRenderMode_Desc".Translate(), null))
			{
				AnimalGearRenderMode = AnimalGearRenderModeHandleEnum._AutoMixed;
			}
			else if (options.RadioButton("AnimalGearRenderMode.FallbackRenderPawnInternal".Translate(), AnimalGearRenderMode == AnimalGearRenderModeHandleEnum._FallbackRenderPawnInternal, 0f, "AnimalGearRenderMode_Desc".Translate(), null))
			{
				AnimalGearRenderMode = AnimalGearRenderModeHandleEnum._FallbackRenderPawnInternal;
			}
			else if (options.RadioButton("AnimalGearRenderMode.NewMeshMode".Translate(), AnimalGearRenderMode == AnimalGearRenderModeHandleEnum._NewMeshMode, 0f, "AnimalGearRenderMode_Desc".Translate(), null))
			{
				AnimalGearRenderMode = AnimalGearRenderModeHandleEnum._NewMeshMode;
			}
			else if (options.RadioButton("AnimalGearRenderMode.Off".Translate(), AnimalGearRenderMode == AnimalGearRenderModeHandleEnum._Off, 0f, "AnimalGearRenderMode_Desc".Translate(), null))
			{
				AnimalGearRenderMode = AnimalGearRenderModeHandleEnum._Off;
			}

			options.Gap();
			options.GapLine();
			options.Gap();

			options.CheckboxLabeled("AnimalGearFallbackToEmptyTextureEnabled_Title".Translate(), ref AnimalGearFallbackToEmptyTextureEnabled, "AnimalGearFallbackToEmptyTextureEnabled_Desc".Translate());
			options.CheckboxLabeled("AnimalGearReplaceLegacyGearWithNewVariantEnabled_Title".Translate(), ref AnimalGearReplaceLegacyGearWithNewVariantEnabled, "AnimalGearReplaceLegacyGearWithNewVariantEnabled_Desc".Translate());
			if (AnimalGearHarmony.ModRPGStyleInventory_ON) options.CheckboxLabeled("AnimalGearRPGInventoryAnimalCompatibilityEnabled_Title".Translate(), ref AnimalGearRPGInventoryAnimalCompatibilityEnabled, "AnimalGearRPGInventoryAnimalCompatibilityEnabled_Desc".Translate());
			options.CheckboxLabeled("AnimalGearDraftedForceWearEnabled_Title".Translate(), ref AnimalGearDraftedForceWearEnabled, "AnimalGearDraftedForceWearEnabled_Desc".Translate());
			if (AnimalGearDraftedForceWearEnabled) options.CheckboxLabeled("AnimalGearDraftedForceWearOnlyCarryTrainedEnabled_Title".Translate(), ref AnimalGearDraftedForceWearOnlyCarryTrainedEnabled, "AnimalGearDraftedForceWearOnlyCarryTrainedEnabled_Desc".Translate());
			options.End();
			base.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "Animal Gear";
		}
		
		public override void WriteSettings()
		{
			base.WriteSettings();

			AnimalGearHarmony.PawnRenderer_RenderPawnAt_Transpiler_Patch.renderMode = AnimalGearRenderMode;
			if (Current.ProgramState == ProgramState.Playing)
			{
				try
				{
					AnimalGearHelper.TryMarkMapPawnFrameSetDirty();
				}
				catch (Exception ex)
				{
					if (Prefs.DevMode)
					{
						Log.Error("AnimalGear: AnimalGearRenderMode.ValueChanged: error: " + ex.Message);
					}
				}
			}
		}
	}

	public class AnimalGearSettings : ModSettings
	{
		public override void ExposeData()
		{
			//Scribe_Values.Look<int>(ref bias, "bias", 5);
			Scribe_Values.Look<bool>(ref AnimalGearFallbackToEmptyTextureEnabled, "myAnimalGearFallbackToEmptyTextureEnabled", true);
			Scribe_Values.Look<bool>(ref AnimalGearReplaceLegacyGearWithNewVariantEnabled, "myAnimalGearReplaceLegacyGearWithNewVariantEnabled", true);
			Scribe_Values.Look<bool>(ref AnimalGearRPGInventoryAnimalCompatibilityEnabled, "myAnimalGearRPGInventoryAnimalCompatibilityEnabled", true);
			Scribe_Values.Look<bool>(ref AnimalGearDraftedForceWearEnabled, "myAnimalGearDraftedForceWearEnabled", true);
			Scribe_Values.Look<bool>(ref AnimalGearDraftedForceWearOnlyCarryTrainedEnabled, "myAnimalGearDraftedForceWearOnlyCarryTrainedEnabled", true);
			Scribe_Values.Look<AnimalGearRenderModeHandleEnum>(ref AnimalGearRenderMode, "enumAnimalGearRenderMode");

			base.ExposeData();
		}

		public enum AnimalGearRenderModeHandleEnum
		{
			_AutoMixed = 0,
			_FallbackRenderPawnInternal = 20,
			_NewMeshMode = 40,
			_Off = 100
		}

		public static AnimalGearRenderModeHandleEnum AnimalGearRenderMode;

		public static bool AnimalGearFallbackToEmptyTextureEnabled = true, 
			AnimalGearReplaceLegacyGearWithNewVariantEnabled = true, 
			AnimalGearRPGInventoryAnimalCompatibilityEnabled = true, 
			AnimalGearDraftedForceWearEnabled = true, 
			AnimalGearDraftedForceWearOnlyCarryTrainedEnabled = true;
	}
}
