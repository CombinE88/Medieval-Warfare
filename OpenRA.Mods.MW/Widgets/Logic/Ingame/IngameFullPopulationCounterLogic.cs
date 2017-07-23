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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Mw.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;


namespace OpenRA.Mods.MW.Widgets.Logic
{
	public class IngameFullPopulationCounterLogic : ChromeLogic
	{
		const float DisplayFracPerFrame = .07f;
		const int DisplayDeltaPerFrame = 37;

		readonly World world;
		readonly Player player;
		readonly int playerResources;
		readonly string cashLabel;
		

		int nextCashTickTime = 0;
		int displayResources;
		private int nextres;
		string displayLabel;

		[ObjectCreator.UseCtor]
		public IngameFullPopulationCounterLogic(Widget widget, World world)
		{
			var pop = widget.Get<LabelWithTooltipWidget>("FULLPOP");
			
			this.world = world;
			player = world.LocalPlayer;
			
			cashLabel = pop.Text;
			displayLabel = cashLabel.F(displayResources);

			pop.GetText = () => displayLabel;

			displayResources = nextres;
		}		

		public override void Tick()
		{
			
			
			if (nextCashTickTime > 0)
				nextCashTickTime--;

			var nextres = player.PlayerActor.Trait<PlayerCivilization>().WorkerPopulationvar+player.PlayerActor.Trait<PlayerCivilization>().Peasantpopulationvar;

			var diff = Math.Abs(nextres - displayResources);
			var move = Math.Min(Math.Max((int)(diff * DisplayFracPerFrame), DisplayDeltaPerFrame), diff);

			if (displayResources < nextres)
			{
				displayResources += move;
			}
			else if (displayResources > nextres)
			{
				displayResources -= move;

				if (Game.Settings.Sound.CashTicks && nextCashTickTime == 0)
				{
					nextCashTickTime = 2;
				}
			}

			displayLabel = cashLabel.F(displayResources);
		}
	}
}
