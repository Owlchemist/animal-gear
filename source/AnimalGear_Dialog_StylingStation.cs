using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace AnimalGear
{
	public class AnimalGear_Dialog_StylingStation : Window
	{
		public enum StylingTab
		{
			ApparelColor
		}

		private Pawn pawn;
		Thing stylingStation;
		StylingTab curTab;
		Vector2 apparelColorScrollPosition;
		List<TabRecord> tabs = new List<TabRecord>();
		Dictionary<Apparel, Color> apparelColors = new Dictionary<Apparel, Color>();
		float viewRectHeight;
		bool showHeadgear, showClothes, devEditMode;
		List<Color> allColors;
		static readonly Vector2 ButSize = new Vector2(SetColorButtonWidth, 40f);
		static readonly Vector3 PortraitOffset = new Vector3(0f, 0f, 0.15f);
		const float PortraitZoom = 1.1f, 
			TabMargin = 18f, 
			IconSize = 60f,
			LeftRectPercent = 0.3f,
			ApparelRowButtonsHeight = 24f,
			SetColorButtonWidth = 200f;
		static List<StyleItemDef> tmpStyleItems = new List<StyleItemDef>();

		private List<Color> AllColors
		{
			get
			{
				if (allColors == null)
				{
					allColors = new List<Color>();
					if (pawn.Ideo != null && !Find.IdeoManager.classicMode)
					{
						allColors.Add(pawn.Ideo.ApparelColor);
					}
					if (pawn.story != null && !pawn.DevelopmentalStage.Baby() && pawn.story.favoriteColor.HasValue && !allColors.Any((Color c) => pawn.story.favoriteColor.Value.IndistinguishableFrom(c)))
					{
						allColors.Add(pawn.story.favoriteColor.Value);
					}
					foreach (ColorDef colDef in DefDatabase<ColorDef>.AllDefs.Where((ColorDef x) => x.colorType == ColorType.Ideo || x.colorType == ColorType.Misc))
					{
						if (!allColors.Any((Color x) => x.IndistinguishableFrom(colDef.color)))
						{
							allColors.Add(colDef.color);
						}
					}
					allColors.SortByColor((Color x) => x);
				}
				return allColors;
			}
		}

		public AnimalGear_Dialog_StylingStation(Pawn pawn, Thing stylingStation)
		{
			this.pawn = pawn;
			this.stylingStation = stylingStation;
			forcePause = true;
			showClothes = true;
			foreach (Apparel item in pawn.apparel.WornApparel)
			{
				if (item.TryGetComp<CompColorable>() != null)
				{
					apparelColors.Add(item, item.DesiredColor ?? item.DrawColor);
				}
			}
		}

		public override void PostOpen()
		{
			if (!ModLister.CheckIdeology("Styling station"))
			{
				Close();
			}
			else
			{
				base.PostOpen();
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			Text.Font = GameFont.Medium;
			Rect rect = new Rect(inRect);
			rect.height = Text.LineHeight * 2f;
			Rect rect2 = rect;
			Widgets.Label(rect2, "StylePawn".Translate().CapitalizeFirst() + ": " + Find.ActiveLanguageWorker.WithDefiniteArticle(pawn.Name.ToStringShort, pawn.gender, plural: false, name: true).ApplyTag(TagType.Name));
			Text.Font = GameFont.Small;
			inRect.yMin = rect2.yMax + 4f;
			Rect rect3 = inRect;
			rect3.width *= LeftRectPercent;
			rect3.yMax -= ButSize.y + 4f;
			DrawPawn(rect3);
			Rect rect4 = inRect;
			rect4.xMin = rect3.xMax + 10f;
			rect4.yMax -= ButSize.y + 4f;
			DrawTabs(rect4);
			DrawBottomButtons(inRect);
			if (Prefs.DevMode)
			{
				Widgets.CheckboxLabeled(new Rect(inRect.xMax - 120f, 0f, 120f, 30f), "DEV: Show all", ref devEditMode);
			}
		}

		void DrawPawn(Rect rect)
		{
			Rect rect2 = rect;
			rect2.yMin = rect.yMax - Text.LineHeight * 2f;
			Widgets.CheckboxLabeled(new Rect(rect2.x, rect2.y, rect2.width, rect2.height / 2f), "ShowHeadgear".Translate(), ref showHeadgear);
			Widgets.CheckboxLabeled(new Rect(rect2.x, rect2.y + rect2.height / 2f, rect2.width, rect2.height / 2f), "ShowApparel".Translate(), ref showClothes);
			rect.yMax = rect2.yMin - 4f;
			Widgets.BeginGroup(rect);
			for (int i = 0; i < 3; i++)
			{
				Rect position = new Rect(0f, rect.height / 3f * (float)i, rect.width, rect.height / 3f).ContractedBy(4f);
				RenderTexture image = PortraitsCache.Get(pawn, new Vector2(position.width, position.height), new Rot4(2 - i), PortraitOffset, PortraitZoom, supersample: true, compensateForUIScale: true, showHeadgear, showClothes, apparelColors, null, stylingStation: true);
				GUI.DrawTexture(position, image);
			}
			Widgets.EndGroup();
		}

		void DrawTabs(Rect rect)
		{
			tabs.Clear();
			tabs.Add(new TabRecord("ApparelColor".Translate().CapitalizeFirst(), delegate
			{
				curTab = StylingTab.ApparelColor;
			}, curTab == StylingTab.ApparelColor));
			Widgets.DrawMenuSection(rect);
			TabDrawer.DrawTabs(rect, tabs);
			rect = rect.ContractedBy(TabMargin);
			if (curTab == StylingTab.ApparelColor)
			{
				DrawApparelColor(rect);
			}
		}

		void DrawDyeRequirement(Rect rect, ref float curY, int requiredDye)
		{
			Widgets.ThingIcon(new Rect(rect.x, curY, Text.LineHeight, Text.LineHeight), ThingDefOf.Dye, null, null, PortraitZoom);
			string text = string.Concat("Required".Translate() + ": ", requiredDye, " ", ThingDefOf.Dye.label);
			float x = Text.CalcSize(text).x;
			Widgets.Label(new Rect(rect.x + Text.LineHeight + 4f, curY, x, Text.LineHeight), text);
			Rect rect2 = new Rect(rect.x, curY, x + Text.LineHeight + 8f, Text.LineHeight);
			if (Mouse.IsOver(rect2))
			{
				Widgets.DrawHighlight(rect2);
				TooltipHandler.TipRegionByKey(rect2, "TooltipDyeExplanation");
			}
			curY += Text.LineHeight;
		}

		void DrawApparelColor(Rect rect)
		{
			bool flag = false;
			Rect viewRect = new Rect(rect.x, rect.y, rect.width - 16f, viewRectHeight);
			Widgets.BeginScrollView(rect, ref apparelColorScrollPosition, viewRect);
			int num = 0;
			float curY = rect.y;
			foreach (Apparel item in pawn.apparel.WornApparel)
			{
				Rect rect2 = new Rect(rect.x, curY, viewRect.width, 92f);
				Color color = apparelColors[item];
				curY += rect2.height + 10f;
				if (!pawn.apparel.IsLocked(item))
				{
					flag |= Widgets.ColorSelector(rect2, ref color, AllColors, out var _, item.def.uiIcon);
					float num2 = rect2.x;
					if (pawn.Ideo != null && !Find.IdeoManager.classicMode)
					{
						rect2 = new Rect(num2, curY, SetColorButtonWidth, ApparelRowButtonsHeight);
						if (Widgets.ButtonText(rect2, "SetIdeoColor".Translate()))
						{
							flag = true;
							color = pawn.Ideo.ApparelColor;
							SoundDefOf.Tick_Low.PlayOneShotOnCamera();
						}
						num2 += 210f;
					}
					Pawn_StoryTracker story = pawn.story;
					if (story != null && story.favoriteColor.HasValue)
					{
						rect2 = new Rect(num2, curY, SetColorButtonWidth, ApparelRowButtonsHeight);
						if (Widgets.ButtonText(rect2, "SetFavoriteColor".Translate()))
						{
							flag = true;
							color = pawn.story.favoriteColor.Value;
							SoundDefOf.Tick_Low.PlayOneShotOnCamera();
						}
					}
					if (!color.IndistinguishableFrom(item.DrawColor))
					{
						num++;
					}
					apparelColors[item] = color;
				}
				else
				{
					Widgets.ColorSelectorIcon(new Rect(rect2.x, rect2.y, 88f, 88f), item.def.uiIcon, color);
					Text.Anchor = TextAnchor.MiddleLeft;
					Rect rect3 = rect2;
					rect3.x += 100f;
					Widgets.Label(rect3, "ApparelLockedCannotRecolor".Translate(pawn.Named("PAWN"), item.Named("APPAREL")).Colorize(ColorLibrary.RedReadable));
					Text.Anchor = TextAnchor.UpperLeft;
				}
				curY += 34f;
			}
			if (num > 0)
			{
				DrawDyeRequirement(rect, ref curY, num);
			}
			if (pawn.Map.resourceCounter.GetCount(ThingDefOf.Dye) < num)
			{
				Rect rect4 = new Rect(rect.x, curY, rect.width - 16f - 10f, IconSize);
				Color color2 = GUI.color;
				GUI.color = ColorLibrary.RedReadable;
				Widgets.Label(rect4, "NotEnoughDye".Translate() + " " + "NotEnoughDyeWillRecolorApparel".Translate());
				GUI.color = color2;
				curY += rect4.height;
			}
			if (Event.current.type == EventType.Layout)
			{
				viewRectHeight = curY - rect.y;
			}
			Widgets.EndScrollView();
		}

		void DrawStylingItemType<T>(Rect rect, ref Vector2 scrollPosition, Action<Rect, T> drawAction, Action<T> selectAction, Func<StyleItemDef, bool> hasStyleItem, Func<StyleItemDef, bool> hadStyleItem, Func<StyleItemDef, bool> extraValidator = null, bool doColors = false) where T : StyleItemDef
		{
			Rect viewRect = new Rect(rect.x, rect.y, rect.width - 16f, viewRectHeight);
			int num = Mathf.FloorToInt(viewRect.width / IconSize) - 1;
			float num2 = (viewRect.width - (float)num * IconSize - (float)(num - 1) * 10f) / 2f;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			tmpStyleItems.Clear();
			tmpStyleItems.AddRange(DefDatabase<T>.AllDefs.Where((T x) => (devEditMode || PawnStyleItemChooser.WantsToUseStyle(pawn, x) || hadStyleItem(x)) && (extraValidator == null || extraValidator(x))));
			tmpStyleItems.SortBy((StyleItemDef x) => 0f - PawnStyleItemChooser.StyleItemChoiceLikelihoodFor(x, pawn));
			if (tmpStyleItems.NullOrEmpty())
			{
				Widgets.NoneLabelCenteredVertically(rect, "(" + "NoneUsableForPawn".Translate(pawn.Named("PAWN")) + ")");
				return;
			}
			Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
			foreach (StyleItemDef tmpStyleItem in tmpStyleItems)
			{
				if (num5 >= num - 1)
				{
					num5 = 0;
					num4++;
				}
				else if (num3 > 0)
				{
					num5++;
				}
				Rect rect2 = new Rect(rect.x + num2 + (float)num5 * IconSize + (float)num5 * 10f, rect.y + (float)num4 * IconSize + (float)num4 * 10f, IconSize, IconSize);
				Widgets.DrawHighlight(rect2);
				if (Mouse.IsOver(rect2))
				{
					Widgets.DrawHighlight(rect2);
					TooltipHandler.TipRegion(rect2, tmpStyleItem.LabelCap);
				}
				drawAction?.Invoke(rect2, tmpStyleItem as T);
				if (hasStyleItem(tmpStyleItem))
				{
					Widgets.DrawBox(rect2, 2);
				}
				if (Widgets.ButtonInvisible(rect2))
				{
					selectAction?.Invoke(tmpStyleItem as T);
					SoundDefOf.Tick_High.PlayOneShotOnCamera();
					pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
					PortraitsCache.SetDirty(pawn);
				}
				num3++;
			}
			if (Event.current.type == EventType.Layout)
			{
				viewRectHeight = (float)(num4 + 1) * IconSize + (float)num4 * 10f + 10f;
			}
			Widgets.EndScrollView();
		}

		void DrawBottomButtons(Rect inRect)
		{
			if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "Cancel".Translate()))
			{
				Reset();
				Close();
			}
			if (Widgets.ButtonText(new Rect(inRect.xMin + inRect.width / 2f - ButSize.x / 2f, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "Reset".Translate()))
			{
				Reset();
				SoundDefOf.Tick_Low.PlayOneShotOnCamera();
			}
			if (Widgets.ButtonText(new Rect(inRect.xMax - ButSize.x, inRect.yMax - ButSize.y, ButSize.x, ButSize.y), "Accept".Translate()))
			{
				ApplyApparelColors();
				Close();
			}
		}

		void ApplyApparelColors()
		{
			bool flag = false;
			foreach (KeyValuePair<Apparel, Color> apparelColor in apparelColors)
			{
				if (apparelColor.Key.DrawColor != apparelColor.Value)
				{
					apparelColor.Key.DesiredColor = apparelColor.Value;
					flag = true;
				}
			}
			try
			{
				if (pawn.Drafted && flag)
				{
					pawn.drafter.Drafted = false;
				}
			}
			catch
			{
			}
			pawn.mindState.Notify_OutfitChanged();
		}

		void Reset(bool resetColors = true)
		{
			if (resetColors)
			{
				apparelColors.Clear();
				foreach (Apparel item in pawn.apparel.WornApparel)
				{
					if (item.TryGetComp<CompColorable>() != null)
					{
						apparelColors.Add(item, item.DesiredColor ?? item.DrawColor);
					}
				}
			}
			pawn.Drawer.renderer.graphics.SetAllGraphicsDirty();
			PortraitsCache.SetDirty(pawn);
		}
	}
}
