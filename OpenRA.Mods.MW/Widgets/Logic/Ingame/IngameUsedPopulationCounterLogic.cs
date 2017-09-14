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
using System.Diagnostics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;


namespace OpenRA.Mods.MW.Widgets.Logic
{
	public class IngameUsedPopulationCounterLogic : ChromeLogic
	{
		const float DisplayFracPerFrame = .07f;
		const int DisplayDeltaPerFrame = 37;

		readonly World world;
		readonly Player player;
		readonly int playerResources;
		readonly string cashLabel;
		int nextCashTickTime = 0;
		int displayResources;
		string displayLabel;
		
		[ObjectCreator.UseCtor]
		public IngameUsedPopulationCounterLogic(Widget widget, World world)
		{
			var pop = widget.Get<LabelWithTooltipWidget>("USEDPOP");
			
			this.world = world;
			player = world.LocalPlayer;
			
			cashLabel = pop.Text;
			displayLabel = cashLabel.F(displayResources);

			pop.GetText = () => displayLabel;

			displayResources = player.PlayerActor.Trait<PlayerCivilization>().WorkerPopulationvar;
		}

		public override void Tick()
		{
			
			
			if (nextCashTickTime > 0)
				nextCashTickTime--;

			var actual = player.PlayerActor.Trait<PlayerCivilization>().WorkerPopulationvar;
			Debug.Write(actual.ToString());

			var diff = Math.Abs(actual - displayResources);
			var move = Math.Min(Math.Max((int)(diff * DisplayFracPerFrame), DisplayDeltaPerFrame), diff);

			if (displayResources < actual)
			{
				displayResources += move;
			}
			else if (displayResources > actual)
			{
				displayResources -= move;

				if (Game.Settings.Sound.CashTicks && nextCashTickTime == 0)
				{
					nextCashTickTime = 2;
				}
			}

			if (player.Faction.InternalName != "ded")
				displayLabel = "Working: " + cashLabel.F(displayResources);
			else
				displayLabel = "Ressurected: " + cashLabel.F(displayResources);
		}
	}
}
