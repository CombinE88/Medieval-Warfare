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

namespace OpenRA.Mods.Common.Traits
{
	public class ShakeOnCreateInfo : ITraitInfo
	{
		public readonly int Intensity = 10;
		public object Create(ActorInitializer init) { return new ShakeOnCreate(this); }
	}

	public class ShakeOnCreate : INotifyCreated
	{
		readonly ShakeOnCreateInfo info;

		public ShakeOnCreate(ShakeOnCreateInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			self.World.WorldActor.Trait<ScreenShaker>().AddEffect(info.Intensity, self.CenterPosition, 1);
		}
	}
}
