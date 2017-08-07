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
	public class Prey : Activity, IDockActivity
	{
		readonly AcolytePreyInfo info;
		
		readonly HashSet<string> preyBuildings;
		private Actor targetActor;
		Actor target;

		public Prey(Actor self, Actor who = null)
		{
			info = self.Info.TraitInfo<AcolytePreyInfo>();
			preyBuildings = info.TargetActors;
			targetActor =  self.Trait<AcolytePrey>().forceactor;
			
			target = who;

		}
		
		public IEnumerable<Actor> getPentas(Actor self)
		{
			return self.World.ActorsHavingTrait<DockManager>()
				.Where(a =>preyBuildings.Contains(a.Info.Name));
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;
			
			if (target == null || target.IsDead || target.Disposed || !target.Trait<DockManager>().HasFreeServiceDock(self))
			{
				var pentas = getPentas(self);
				var dockablePentas = pentas.Where(p => p.Trait<DockManager>().HasFreeServiceDock(self));
				if (dockablePentas.Any())
					target = dockablePentas.ClosestTo(self);
				else if (pentas.Any())
					target = pentas.ClosestTo(self);
				else
					target = null;
			}

			if (target == null)
			{
				Cancel(self);
				return NextActivity;
			}
			
			if (!target.Trait<DockManager>().HasFreeServiceDock(self))
			{
				return new Wait(20);
			}
			
			target.Trait<DockManager>().ReserveDock(target, self, this);
			
			return NextActivity;
		}

		Activity IDockActivity.ApproachDockActivities(Actor host, Actor client, Dock dock)
		{
			return DockUtils.GenericApproachDockActivities(host, client, dock, this, true);
		}

		Activity IDockActivity.DockActivities(Actor host, Actor client, Dock dock)
		{
			return ActivityUtils.SequenceActivities( new PreyActivity(client,host,dock.Info.FaceTowardsCenter,dock) );
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