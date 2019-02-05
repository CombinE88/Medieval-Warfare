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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.MW.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.MW.Traits.WorldTraits
{
	class MwProductionQueueFromSelectionInfo : ITraitInfo
	{
		public string ProductionTabsWidget = null;
		public string ProductionPaletteWidget = null;

		public object Create(ActorInitializer init) { return new MwProductionQueueFromSelection(init.World, this); }
	}

	class MwProductionQueueFromSelection : INotifySelection
	{
		readonly World world;
		readonly Lazy<MWProductionTabsWidget> tabsWidget;
		readonly Lazy<MWProductionPaletteWidget> paletteWidget;

		public MwProductionQueueFromSelection(World world, MwProductionQueueFromSelectionInfo info)
		{
			this.world = world;

			tabsWidget = Exts.Lazy(() => Ui.Root.GetOrNull(info.ProductionTabsWidget) as MWProductionTabsWidget);
			paletteWidget = Exts.Lazy(() => Ui.Root.GetOrNull(info.ProductionPaletteWidget) as MWProductionPaletteWidget);
		}

		void INotifySelection.SelectionChanged()
		{
			// Disable for spectators
			if (world.LocalPlayer == null)
				return;

			// Queue-per-actor
			var queue = world.Selection.Actors
				.Where(a => a.IsInWorld && a.World.LocalPlayer == a.Owner)
				.SelectMany(a => a.TraitsImplementing<ProductionQueue>())
				.FirstOrDefault(q => q.Enabled);

			// Queue-per-player
			if (queue == null)
			{
				var types = world.Selection.Actors.Where(a => a.IsInWorld && a.World.LocalPlayer == a.Owner)
					.SelectMany(a => a.TraitsImplementing<Production>().Where(p => !p.IsTraitDisabled))
					.SelectMany(t => t.Info.Produces);

				queue = world.LocalPlayer.PlayerActor.TraitsImplementing<ProductionQueue>()
					.FirstOrDefault(q => q.Enabled && types.Contains(q.Info.Type));
			}

			if (queue == null || !queue.BuildableItems().Any())
				return;

			if (tabsWidget.Value != null)
				tabsWidget.Value.CurrentQueue = queue;
			else if (paletteWidget.Value != null)
				paletteWidget.Value.CurrentQueue = queue;
		}
	}
}
