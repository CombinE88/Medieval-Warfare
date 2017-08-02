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
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.MW.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Activities
{
	public class PreyBuild : Activity, IDockActivity
	{
		readonly AcolytePreyBuildInfo info;
		
		readonly HashSet<string> preyBuildings;

		public PreyBuild(Actor self)
		{
			info = self.Info.TraitInfo<AcolytePreyBuildInfo>();
			preyBuildings = info.TargetActors;
			
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;
			
			
			var rearmTarget = self.World.Actors.Where(a => preyBuildings.Contains(a.Info.Name))
				.ClosestTo(self);
			
			if (rearmTarget == null)
				return new Wait(20);
			
			rearmTarget.Trait<DockManager>().ReserveDock(rearmTarget, self, this);
			return NextActivity;
		}

		Activity IDockActivity.ApproachDockActivities(Actor host, Actor client, Dock dock)
		{
			return DockUtils.GenericApproachDockActivities(host, client, dock, this, true);
		}

		Activity IDockActivity.DockActivities(Actor host, Actor client, Dock dock)
		{
			return ActivityUtils.SequenceActivities( new PreyBuildActivity(client,host,dock.Info.FaceTowardsCenter,dock) );
		}

		Activity IDockActivity.ActivitiesAfterDockDone(Actor host, Actor client, Dock dock)
		{
			return null;
		}

		Activity IDockActivity.ActivitiesOnDockFail(Actor client)
		{
			// Find another FIX or something.
			return null;
		}
	}
}