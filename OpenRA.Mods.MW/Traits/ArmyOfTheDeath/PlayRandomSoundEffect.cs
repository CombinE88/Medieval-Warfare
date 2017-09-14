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
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
	public class PlayRandomSoundEffectInfo : ITraitInfo
	{
		public readonly int Delay = 1500;
		
		public readonly int RandomExtraDelay = 1500;
		
		public readonly HashSet<string> Audio = new HashSet<string>();
		
		
		public object Create(ActorInitializer init) { return new PlayRandomSoundEffect(this); }
	}

	public class PlayRandomSoundEffect : ITick
	{
		readonly PlayRandomSoundEffectInfo info;

		private int Ticker;
		private bool firsttick = true;

		public PlayRandomSoundEffect(PlayRandomSoundEffectInfo info)
		{
			this.info = info;
				
		}

		void ITick.Tick(Actor self)
		{
			if (firsttick)
			{
				firsttick = false;
				Ticker = info.Delay + self.World.SharedRandom.Next(info.RandomExtraDelay);
			}
			
			Ticker--;
			if (Ticker <= 0)
			{
				if (info.Audio.Any())
					Game.Sound.PlayToPlayer(SoundType.UI, self.Owner,
						info.Audio.ElementAt(self.World.SharedRandom.Next(info.Audio.Count)));
				Ticker = info.Delay + self.World.SharedRandom.Next(info.RandomExtraDelay);
			}
		}
		

	}
}
