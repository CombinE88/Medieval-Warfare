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
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.MW.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Activities
{
    public class FindPrey : Activity, IDockActivity
    {
        readonly AcolytePreyInfo info;

        readonly HashSet<string> preyBuildings;

        private Actor target;

        public FindPrey(Actor self)
        {
            info = self.Info.TraitInfo<AcolytePreyInfo>();
            preyBuildings = info.TargetActors;
        }

        public IEnumerable<Actor> GetPentas(Actor self)
        {
            return self.World.ActorsHavingTrait<DockManager>()
                .Where(a =>
                preyBuildings.Contains(a.Info.Name)
                && (!a.Info.HasTraitInfo<ValidPreyTargetInfo>() ||
                (a.Info.HasTraitInfo<ValidPreyTargetInfo>() &&
                a.TraitOrDefault<ValidPreyTarget>().Actors.Any())));
        }

        Actor FindDock(Actor self)
        {
            var pentas = GetPentas(self);
            var dockablePentas = pentas
                .Where(p => p.Trait<DockManager>().HasFreeServiceDock(self));
            if (dockablePentas.Any())
                return dockablePentas.ClosestTo(self);
            else if (pentas.Any())
                return pentas.ClosestTo(self);
            else
                return null;
        }

        public override Activity Tick(Actor self)
        {
            if (IsCanceled)
                return NextActivity;

            target = FindDock(self);

            if (target == null)
            {
                Cancel(self);
                return NextActivity;
            }

            if (!target.Trait<DockManager>().HasFreeServiceDock(self))
            {
                return new Wait(20);
            }

            self.QueueActivity(new Prey(self, target));

            return NextActivity;
        }

        Activity IDockActivity.ApproachDockActivities(Actor host, Actor client, Dock dock)
        {
            return DockUtils.GenericApproachDockActivities(host, client, dock, this, true);
        }

        Activity IDockActivity.DockActivities(Actor host, Actor client, Dock dock)
        {
            return ActivityUtils.SequenceActivities(new PreyActivity(client, host, true, dock));
        }

        Activity IDockActivity.ActivitiesAfterDockDone(Actor host, Actor client, Dock dock)
        {
            return null;
        }

        Activity IDockActivity.ActivitiesOnDockFail(Actor client)
        {
            return null;
        }
    }
}
