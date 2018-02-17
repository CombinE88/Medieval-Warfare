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

using System.Drawing;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.MW.Widgets.Logic
{
    public class IngamePeasantSpawnBarLogic : ChromeLogic
    {
        int xsize;

        [ObjectCreator.UseCtor]
        public IngamePeasantSpawnBarLogic(Widget widget, World world)
        {


            var populationManager = world.LocalPlayer.PlayerActor.Trait<PlayerCivilization>();

            var powerBar = widget.Get<ResourceBarWidget>("POWERBAR");
            powerBar.IndicatorImage = "indicator-peasant";

            xsize = widget.Bounds.X;

            powerBar.Orientation = ResourceBarOrientation.Horizontal;
            powerBar.GetProvided = () => xsize - (int)(double)(xsize * (populationManager.basecheck * 100 / populationManager.nextchecktick)) / 100;
            powerBar.GetUsed = () => widget.Bounds.X;
            powerBar.TooltipFormat = "Peasant Spawns in: {1}/{0}";
            powerBar.GetBarColor = () =>
            {
                if (populationManager.basecheck >= populationManager.nextchecktick / 2)
                    return Color.DarkGreen;
                if (populationManager.basecheck >= populationManager.nextchecktick / 3)
                    return Color.SeaGreen;
                if (populationManager.basecheck >= populationManager.nextchecktick / 5)
                    return Color.Green;
                return Color.LimeGreen;
            };

        }


    }

}
