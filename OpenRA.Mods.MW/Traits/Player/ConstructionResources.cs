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

using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
	public class ConstructionResourcesInfo : ITraitInfo
	{
		public readonly string Type = "";
		public object Create(ActorInitializer init) { return new ConstructionResources(init.Self, this); }
	}
	

	public class ConstructionResources : ITick, INotifyCreated
	{

		private int tickrate;
		private Actor TraitSelf;
		private ConstructionResourcesInfo info;
		public int Stored;

		void INotifyCreated.Created(Actor self)
		{
		}

		public ConstructionResources(Actor self, ConstructionResourcesInfo info)
		{
			this.info = info;
			TraitSelf = self;
		}

		

		public void Tick(Actor self)
		{
			tickrate--;

			if (tickrate <= 0)
			{
				tickrate = 10;
			}
		}
	}
}
