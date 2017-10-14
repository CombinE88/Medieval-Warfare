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

using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
	[Desc("Lets the actor spread resources around it in a circle.")]
	class DecaysResourceInfo : ConditionalTraitInfo
	{
		public readonly int Interval = 75;
		public readonly string ResourceType = "Ore";
		public readonly int MaxRange = 10;

		public override object Create(ActorInitializer init) { return new DecaysResource(init.Self, this); }
	}

	class DecaysResource : ConditionalTrait<DecaysResourceInfo>, ITick
	{
		readonly DecaysResourceInfo _info;
        
		readonly ResourceLayer _resLayer;

		public DecaysResource(Actor self, DecaysResourceInfo info)
			: base(info)
		{
			this._info = info;
			_resLayer = self.World.WorldActor.Trait<ResourceLayer>();
		}

		int _ticks;

		public void Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (--_ticks <= 0)
			{
				Seed(self);
				_ticks = _info.Interval;
			}
		}

		public void Seed(Actor self)
		{

			var cellone = Util.RandomWalk(self.Location, self.World.SharedRandom)
				.Take(_info.MaxRange)
				.SkipWhile(p => !self.World.Map.Contains(p) ||
				                (_resLayer.GetResourceDensity(p) > 0 && _resLayer.IsFull(p)))
				.Cast<CPos?>().FirstOrDefault();

			if (cellone != null)
			{
				var cell = Util.RandomWalk(cellone.Value, self.World.SharedRandom)
					.Take(_info.MaxRange)
					.SkipWhile(p => !self.World.Map.Contains(p) && _resLayer.GetResourceDensity(p) > 0)
					.Cast<CPos?>().FirstOrDefault();

				if (cell != null)
					_resLayer.Harvest(cell.Value);
			}
		}
		
	}
}
