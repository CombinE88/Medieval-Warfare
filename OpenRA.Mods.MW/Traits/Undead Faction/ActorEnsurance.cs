#region Copyright & License Information
/*
 * Copyright 2017-2017 The MW Developers)
 * This file is part of Medieval Warfare, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion


using System.Linq;
using System.Net.Security;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;


namespace OpenRA.Mods.Mw.Traits
{
	[Desc("A actor has to enter the building before the unit spawns.")]
	public class ActorEnsuranceInfo : ITraitInfo
	{

		[Desc("Actortype to be spawned.")]
		public readonly string SpawnActor;

		[Desc("Time when no actor is given and another should respawn.")]
		public readonly int RespawnTime = 125;

		public readonly int Maxalive = 1;
		
		[Desc("Spawnoffset at the actors Position.")]
		public readonly WVec Offset = WVec.Zero;
		
		[Desc("Target Pos where newly spawned actor move.")]
		public readonly CVec MoveOffset = CVec.Zero;

		public readonly int StartDelay = 125;

		public object Create(ActorInitializer init) { return new ActorEnsurance(init, this); }
	}

	class ActorEnsurance : ITick
	{
		private int Ticker;
		private readonly ActorEnsuranceInfo info;



		public ActorEnsurance(ActorInitializer init, ActorEnsuranceInfo info)
		{
			this.info = info;
			
			Ticker = info.StartDelay;
		}


		void ITick.Tick(Actor self)
		{
				Ticker--;

			if (Ticker < 1)
			{
				var actors = self.World.ActorMap.ActorsInBox(self.World.Map.ProjectedTopLeft, self.World.Map.ProjectedBottomRight)
					.Where(a => a.Info.Name == info.SpawnActor && a.Owner == self.Owner && !a.IsDead && a.IsInWorld);

				if (actors.Count() < info.Maxalive)
				{
					SpawnNewActor(self);
				}
				Ticker = info.RespawnTime;
			}
		}
		
		public void SpawnNewActor(Actor self)
		{
			if (!self.IsDead || self.IsInWorld)
			{
				self.World.AddFrameEndTask(w =>
				{
					var td = new TypeDictionary
					{
						new ParentActorInit(self),
						new LocationInit(self.Location + info.MoveOffset),
						new CenterPositionInit(self.CenterPosition + info.Offset),
						new OwnerInit(self.Owner),
						new FactionInit(self.Owner.Faction.InternalName),
						new FacingInit(0)
					};
					var RespawnActor = w.CreateActor(info.SpawnActor, td);
					var moveto = RespawnActor.TraitOrDefault<IMove>();
					RespawnActor.CancelActivity();
					RespawnActor.QueueActivity(moveto.VisualMove(RespawnActor, RespawnActor.CenterPosition,
						self.World.Map.CenterOfCell(self.Location + info.MoveOffset)));
				});
			}
		}
	}
}

