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

using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;


namespace OpenRA.Mods.MW.Widgets.Logic
	{
	public class IngameMaxPopulationCounterLogic : ChromeLogic
	{
		readonly World world;
		readonly Player player;
		readonly string cashLabel;
        
		public int displayResources;
		string displayLabel;
		int displaymaxbeds = 0;

		[ObjectCreator.UseCtor]
		public IngameMaxPopulationCounterLogic(Widget widget, World world)
		{
			var pop = widget.Get<LabelWithTooltipWidget>("MAXPOP");
		
			this.world = world;
			player = world.LocalPlayer;
		
			cashLabel = pop.Text;
			displayLabel = cashLabel.F(displayResources);

			pop.GetText = () => displayLabel;

			displayResources = player.PlayerActor.Trait<PlayerCivilization>().FreePopulation>0 ? player.PlayerActor.Trait<PlayerCivilization>().FreePopulation : 0;
			displaymaxbeds = player.PlayerActor.Trait<PlayerCivilization>().MaxLivingspacevar;

		}

		public override void Tick()
		{
		
			displayResources = player.PlayerActor.Trait<PlayerCivilization>().FreePopulation>0 ? player.PlayerActor.Trait<PlayerCivilization>().FreePopulation : 0;
			displaymaxbeds = player.PlayerActor.Trait<PlayerCivilization>().MaxLivingspacevar;
			
			if (player.Faction.InternalName != "ded")
				displayLabel = "Free Beds: " + cashLabel.F(displayResources + " ( " + displaymaxbeds + " )");
			else
				displayLabel = "Free Graves: " + cashLabel.F(displayResources + " ( " + displaymaxbeds + " )");
		}
	}
}
