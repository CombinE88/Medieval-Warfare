#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.MW.Traits.BuildingTraits;
using OpenRA.Widgets;

namespace OpenRA.Mods.MW.Widgets.Logic.Ingame
{
	public class MWProductionTabsLogic : ChromeLogic
	{
		readonly MWProductionTabsWidget tabs;
		readonly World world;

		void SetupProductionGroupButton(MWProductionTypeButtonWidget button)
		{
			if (button == null)
				return;

			Action<bool> selectTab = reverse =>
			{
				if (tabs.QueueGroup == button.ProductionGroup)
					tabs.SelectNextTab(reverse);
				else
					tabs.QueueGroup = button.ProductionGroup;

				if (!button.InstantProductionType)
					tabs.PickUpCompletedBuilding();
			};

			button.IsDisabled = () => !tabs.Groups[button.ProductionGroup].Tabs.Any(t => t.Queue.BuildableItems().Any());
			button.OnMouseUp = mi => selectTab(mi.Modifiers.HasModifier(Modifiers.Shift));
			button.OnKeyPress = e => selectTab(e.Modifiers.HasModifier(Modifiers.Shift));
			button.IsHighlighted = () => tabs.QueueGroup == button.ProductionGroup && !button.InstantProductionType;

			var chromeName = button.ProductionGroup.ToLowerInvariant();
			var icon = button.Get<ImageWidget>("ICON");
			icon.GetImageName = () => button.IsDisabled() ? chromeName + "-disabled" :
				tabs.Groups[button.ProductionGroup].Alert && !button.InstantProductionType ? chromeName + "-alert" : chromeName;
		}

		[ObjectCreator.UseCtor]
		public MWProductionTabsLogic(Widget widget, World world)
		{
			this.world = world;
			tabs = widget.Get<MWProductionTabsWidget>("PRODUCTION_TABS");
			world.ActorAdded += tabs.ActorChanged;
			world.ActorRemoved += tabs.ActorChanged;
			Game.BeforeGameStart += UnregisterEvents;

			var typesContainer = Ui.Root.Get(tabs.TypesContainer);
			foreach (var i in typesContainer.Children)
				SetupProductionGroupButton(i as MWProductionTypeButtonWidget);

			var background = Ui.Root.GetOrNull(tabs.BackgroundContainer);
			if (background != null)
			{
				var palette = tabs.Parent.Get<MWProductionPaletteWidget>(tabs.PaletteWidget);
				var icontemplate = background.Get("ICON_TEMPLATE");

				Action<int, int> updateBackground = (oldCount, newCount) =>
				{
					background.RemoveChildren();

					for (var i = 0; i < newCount; i++)
					{
						var x = i % palette.Columns;
						var y = i / palette.Columns;

						var bg = icontemplate.Clone();
						bg.Bounds.X = palette.IconSize.X * x;
						bg.Bounds.Y = palette.IconSize.Y * y;
						background.AddChild(bg);
					}
				};

				palette.OnIconCountChanged += updateBackground;

				// Set the initial palette state
				updateBackground(0, 0);
			}
		}

		void UnregisterEvents()
		{
			Game.BeforeGameStart -= UnregisterEvents;
			world.ActorAdded -= tabs.ActorChanged;
			world.ActorRemoved -= tabs.ActorChanged;
		}
	}
}
