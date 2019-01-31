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

using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.MW.Widgets.Logic
{
    public class IngameMaxPopulationCounterLogic : ChromeLogic
    {
        readonly Player player;
        readonly string cashLabel;

        int displayResources;
        string displayLabel;
        int displaymaxbeds = 0;

        [ObjectCreator.UseCtor]
        public IngameMaxPopulationCounterLogic(Widget widget, World world)
        {
            var pop = widget.Get<LabelWithTooltipWidget>("MAXPOP");
            player = world.LocalPlayer;

            cashLabel = pop.Text;
            displayLabel = cashLabel.F(displayResources);

            pop.GetText = () => displayLabel;

            displayResources = player.PlayerActor.Trait<PowerManager>().PowerProvided - player.PlayerActor.Trait<PowerManager>().PowerDrained;
            displaymaxbeds = player.PlayerActor.Trait<PowerManager>().PowerProvided;
        }

        public override void Tick()
        {
            displayResources = player.PlayerActor.Trait<PowerManager>().PowerProvided - player.PlayerActor.Trait<PowerManager>().PowerDrained;
            displaymaxbeds = player.PlayerActor.Trait<PowerManager>().PowerProvided;

            if (player.Faction.InternalName != "ded")
                displayLabel = "Free Beds: " + cashLabel.F((displayResources >= 0 ? displayResources : 0) + " ( " + displaymaxbeds + " )");
            else
                displayLabel = "Free Graves: " + cashLabel.F((displayResources >= 0 ? displayResources : 0) + " ( " + displaymaxbeds + " )");
        }
    }
}
