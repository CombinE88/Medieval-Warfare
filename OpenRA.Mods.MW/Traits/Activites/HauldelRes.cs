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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.MW.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Activities
{
    public class HaulDelRes : Activity
    {
        readonly HaulerInfo info;

        private int maxmaterial;
        private int currentmaterial;

        private int maxtick;

        public HaulDelRes(Actor self)
        {
            info = self.Info.TraitInfo<HaulerInfo>();
            maxmaterial = info.Storageamount;
            currentmaterial = self.Trait<Hauler>().material;
            maxtick = info.Delivertime;
        }

        public override Activity Tick(Actor self)
        {
            if (IsCanceled)
                return NextActivity;

            if (NextInQueue != null)
                return NextInQueue;

            if (currentmaterial <= 0)
            {
                return NextActivity;
            }
            
            var target = self.Trait<Hauler>().DeliverActor;
            
            if (target != null && currentmaterial>0 && target.Info.HasTraitInfo<ConstructionProgressInfo>() && target.Trait<ConstructionProgress>().Resources < target.Info.TraitInfo<ConstructionProgressInfo>().Resourceprocess && currentmaterial<maxmaterial)
            {
                currentmaterial--;
                self.Trait<Hauler>().material = currentmaterial;
                target.Trait<ConstructionProgress>().Resources++;
                return ActivityUtils.SequenceActivities(new Wait(maxtick), this);
            }
            
            return this;
        }
    }
}