#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.MW.Traits.Render;
using OpenRA.Widgets;

namespace OpenRA.Mods.MW.Widgets
{
	/// <summary> Contains all functions that are unit-specific. </summary>
	public class ConstructionPriorityLogic : ChromeLogic
	{
		readonly World world;

		int selectionHash;
		Actor[] selectedActors = { };
		bool attackMoveDisabled = true;

		int deployHighlighted;
		int scatterHighlighted;
		int stopHighlighted;

		private bool priorityisdisabled;

		TraitPair<IIssueDeployOrder>[] selectedDeploys = { };

		[ObjectCreator.UseCtor]
		public ConstructionPriorityLogic(Widget widget, World world)
		{
			this.world = world;

			var addprioritybutton = widget.GetOrNull<ButtonWidget>("ADD_PRIO");
			if (addprioritybutton != null)
			{
				BindButtonIcon(addprioritybutton);
				addprioritybutton.IsDisabled = () =>
				{
					UpdateStateIfNecessary();
					return priorityisdisabled;
				};
				addprioritybutton.GetKey = _ => new Hotkey(Keycode.KP_PLUS, Modifiers.None);
				addprioritybutton.IsHighlighted = () => stopHighlighted > 0;
				addprioritybutton.OnClick = () => PerformOrderOnSelection(a => new Order("AddPrio", a, false));
				addprioritybutton.OnKeyPress = ki => { stopHighlighted = 2; addprioritybutton.OnClick(); };
			}
			var removeprioritybutton = widget.GetOrNull<ButtonWidget>("REM_PRIO");
			if (removeprioritybutton != null)
			{
				removeprioritybutton.IsDisabled = () =>
				{
					UpdateStateIfNecessary();
					return priorityisdisabled;
				};
				addprioritybutton.GetKey = _ => new Hotkey(Keycode.KP_MINUS, Modifiers.None);
				removeprioritybutton.IsHighlighted = () => stopHighlighted > 0;
				removeprioritybutton.OnClick = () => PerformOrderOnSelection(a => new Order("RemPrio", a, false));
				removeprioritybutton.OnKeyPress = ki => { stopHighlighted = 2; addprioritybutton.OnClick(); };

			}
		}

		void UpdateStateIfNecessary()
		{
			if (selectionHash == world.Selection.Hash)
				return;

			selectedActors = world.Selection.Actors
				.Where(a => a.Owner == world.LocalPlayer && a.IsInWorld && a.Info.HasTraitInfo<ConstructionPriorityInfo>())
				.ToArray();

			priorityisdisabled = !selectedActors.Any(a => a.Info.HasTraitInfo<ConstructionPriorityInfo>());
			
		}

		void BindButtonIcon(ButtonWidget button)
		{
			var icon = button.Get<ImageWidget>("ICON");
			icon.GetImageName = () => button.IsDisabled() ? icon.ImageName + "-disabled" : icon.ImageName;
		}

		void PerformOrderOnSelection(Func<Actor, Order> f)
		{
			UpdateStateIfNecessary();

			var orders = selectedActors
				.Select(f)
				.ToArray();

			foreach (var o in orders)
				world.IssueOrder(o);
		}
	}
}
