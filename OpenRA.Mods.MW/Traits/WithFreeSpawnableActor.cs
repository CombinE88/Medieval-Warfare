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


using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;


namespace OpenRA.Mods.MW.Traits
{
	[Desc("A actor has to enter the building before the unit spawns.")]
	public class WithFreeSpawnableActorInfo : ConditionalTraitInfo
    {
		[Desc("Valid actortypes wich can pe used to produce/ convert.")]
		public readonly HashSet<string> TrainingActors = new HashSet<string>();
		
		[Desc("The range in cells where should be looked for.")]
		public readonly int FindRadius = 15;

		[Desc("Actortype to be spawned.")]
		public readonly string SpawnActor;
		
		[Desc("Spawn at the Start or wait for RespawnTime.")]
		public readonly bool SpawnStart = true;
		
		[Desc("Time when no actor is given and another should respawn.")]
		public readonly int RespawnTime = 500;
		
		[Desc("Spawnoffset at the actors Position.")]
		public readonly WVec Offset = WVec.Zero;
		
		[Desc("Target Pos where newly spawned actor move.")]
		public readonly CVec MoveOffset = CVec.Zero;

		[Desc("Should stay actors near the Spawnarea.")]
		public readonly bool Sticky = false;
		
		[Desc("How Far in Cells can the actor go until he gets a returm queued.")]
		public readonly WDist Lasso = WDist.FromCells(5);
		
		[Desc("How Far in Cells can the actor go until he is forced to instantly Return.")]
		public readonly WDist ForceLasso = WDist.FromCells(20);
		
		public readonly bool UseConverting = true;
		
		public readonly bool ReturnOnDeath = false;
		
		public override object Create(ActorInitializer init) { return new WithFreeSpawnableActor(init, this); }
	}

	class WithFreeSpawnableActor : ConditionalTrait<WithFreeSpawnableActorInfo>, ITick, INotifyActorDisposing, INotifyRemovedFromWorld
    {
		private int Ticker;
		private Actor RespawnActor = null;
		private readonly WithFreeSpawnableActorInfo info;
		public HashSet<Actor> InUse = new HashSet<Actor>();
		private int idlecount;



		public WithFreeSpawnableActor(ActorInitializer init, WithFreeSpawnableActorInfo info) : base(info)
        {
			this.info = info;
			if (!info.SpawnStart)
				Ticker = info.RespawnTime;
			idlecount = 25;
		}

		public bool FactoriesGet(Actor self, Actor actor)
		{
			var possibles = self.World.ActorsHavingTrait<WithActorProduction>()
				.Where(a =>
				{
					if (a.Owner != self.Owner)
						return false;
					return true;
				});

			var hashsets = possibles.ToHashSet();

			foreach (var n in hashsets)
			{
				var FindTraitWithActorProduction = n.TraitsImplementing<WithActorProduction>().FirstOrDefault();
				var hashcode = FindTraitWithActorProduction.InUse;

				if (hashcode.Contains(actor))
				{
					return true;
				}
			}

			return false;
		}

		public bool FreeSpawnable(Actor self, Actor actor)
		{
			var possibles = self.World.ActorsHavingTrait<WithFreeSpawnableActor>()
				.Where(a =>
				{
					if (a.Owner != self.Owner)
						return false;
					return true;
				});

			var hashsets = possibles.ToHashSet();

			foreach (var n in hashsets)
			{

				var howmany = n.TraitsImplementing<WithFreeSpawnableActor>();
				
				
				foreach (var b in howmany)
				{
					if (b.InUse.Contains(actor))
					{
						return true;
					}
				}
			}

			return false;
		}

		public Actor PossibleActor(Actor self, HashSet<Actor> AlreadyUsed, HashSet<string> ValidList)
		{
			// Find all Actors within the range, and filter thier Type and if they r taken
			var possibles = self.World.FindActorsInCircle(self.CenterPosition, WDist.FromCells(info.FindRadius))
				.Where(a =>
				{
					if (a == self)
						return false;

					if (a.Owner != self.Owner)
						return false;

					if (FactoriesGet(self, a))
						return false;

					if (FreeSpawnable(self, a))
						return false;

					if (AlreadyUsed.Contains(a))
						return false;

					if (ValidList != null && ValidList.Count > 0)
					{
						if (ValidList.Contains(a.Info.Name))
							return true;
					}

					return false;
				});

			// TODO: change to smalles path
			var closest = possibles.ClosestTo(self);

			if (closest != null)
				return closest;

			return null;
		}

		public WPos Position(Actor actor)
		{
			return actor.CenterPosition;
		}

        void BouncingActor(Actor self) // set the actor back to its origin when going to far
        {
            if (RespawnActor != null && !RespawnActor.IsDead && RespawnActor.IsInWorld && info.Sticky &&
            RespawnActor.Info.Name == info.SpawnActor && !self.IsDead &&
            self.IsInWorld)
            {
                if ((RespawnActor.CenterPosition - self.CenterPosition).LengthSquared >= info.ForceLasso.LengthSquared) // force the movement
                {

                    RespawnActor.CancelActivity();
                    RespawnActor.QueueActivity(RespawnActor.TraitOrDefault<IMove>().MoveTo(self.Location + info.MoveOffset, 2));
                }
                else if (idlecount <= 0 && (RespawnActor.CenterPosition - self.CenterPosition).LengthSquared >= info.Lasso.LengthSquared) // queue the movement for later
                {
                    RespawnActor.QueueActivity(RespawnActor.TraitOrDefault<IMove>().MoveTo(self.Location + info.MoveOffset, 2));
                }
                
            }
        }

        void ITick.Tick(Actor self)
		{
            if (IsTraitDisabled)
                return;

            if (RespawnActor != null && !RespawnActor.IsDead && RespawnActor.IsInWorld && RespawnActor.IsIdle && idlecount > -10)
			{
				idlecount--;
			}
			else if (RespawnActor != null && !RespawnActor.IsDead && RespawnActor.IsInWorld && !RespawnActor.IsIdle)
			{
				idlecount = 75;
			}

            BouncingActor(self);

            if (RespawnActor == null || !RespawnActor.IsInWorld || RespawnActor.IsDead)
				Ticker--;

			if (Ticker < 1)
			{
				SpawnNewActor(self);
			}
		}


		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (info.ReturnOnDeath && RespawnActor != null && !RespawnActor.IsDead && RespawnActor.IsInWorld &&
			    RespawnActor.Info.Name == info.SpawnActor)
			{

				var td = new TypeDictionary
				{
					new LocationInit(RespawnActor.Location),
					new CenterPositionInit(RespawnActor.CenterPosition),
					new OwnerInit(RespawnActor.Owner),
					new FactionInit(RespawnActor.Owner.Faction.InternalName),
					new FacingInit(0)
				};
				RespawnActor.CancelActivity();
				RespawnActor.QueueActivity(new RemoveSelf());
				if(info.UseConverting)
					self.World.CreateActor(info.TrainingActors.ElementAt(self.World.SharedRandom.Next(info.TrainingActors.Count)), td);
			}

		}
		
		
		public void SpawnNewActor(Actor self)
		{
			//basic setup of values
			var owner = self.Owner;
			var exit = self.Location + info.MoveOffset;


			//find produced unit cost values
			var ValidActors = new HashSet<string>();
			;

			var guysFound = new HashSet<Actor>();

			//find enough actors

			Actor actor = null;
			PersonValuedInfo BuilderInfo = null;
			if (self.World.Map.Rules.Actors[info.SpawnActor].HasTraitInfo<PersonValuedInfo>())
				BuilderInfo = self.World.Map.Rules.Actors[info.SpawnActor].TraitInfo<PersonValuedInfo>();

			if (BuilderInfo != null && info.UseConverting)
			{
				ValidActors = BuilderInfo.ConvertingActors;
				actor = PossibleActor(self, guysFound, ValidActors);
			}
			else if (info.TrainingActors.Any() && info.UseConverting)
			{
				ValidActors = info.TrainingActors;
				actor = PossibleActor(self, guysFound, ValidActors);
			}
			else
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
						RespawnActor = w.CreateActor(info.SpawnActor, td);
						var moveto = RespawnActor.TraitOrDefault<IMove>();
						RespawnActor.CancelActivity();
						RespawnActor.QueueActivity(moveto.VisualMove(RespawnActor, RespawnActor.CenterPosition,
							self.World.Map.CenterOfCell(self.Location + info.MoveOffset)));

						if (RespawnActor.Info.HasTraitInfo<HarvesterInfo>())
						{
							RespawnActor.QueueActivity(new FindResources(RespawnActor));
						}

						if (InUse.Contains(RespawnActor))
							InUse.Remove(RespawnActor);
					});
					Ticker = info.RespawnTime;
					return;
				}
			}
			Ticker = info.RespawnTime;
			if (actor == null)
			{
				Ticker = 50;
				return;
			}

			var infiltrate = self.CenterPosition + info.Offset;
			//Actor is possible to move?
			RespawnActor = actor;
			InUse.Add(actor);
			var move = actor.TraitOrDefault<IMove>();

			if ((!actor.IsInWorld || !actor.IsDead))
			{
				self.Owner.PlayerActor.Trait<PlayerCivilization>().SpawnStoredPeasant();
				//beginn movement
				actor.CancelActivity();
				actor.QueueActivity(move.MoveTo(exit, 5));

                if (!actor.IsDead && actor.IsInWorld && actor.Info.HasTraitInfo<IsPeasantInfo>())
                {
                    var peas = actor.TraitsImplementing<IsPeasant>().FirstOrDefault();
                    peas.setWroking();
                }

                //what happens when actor or barracks dies
                actor.QueueActivity(new CallFunc(() =>
				{

					if (StillValid(actor, self))
						return;

					//if not died continue and recalculate ow position
					//visually enter the building
					var selfposition = actor.CenterPosition;
					actor.QueueActivity(move.VisualMove(actor, selfposition, infiltrate));
					//if dead before finished
					actor.QueueActivity(new CallFunc(() =>
					{
						if (StillValid(actor, self))
							return;

						var td = new TypeDictionary
						{
							new ParentActorInit(self),
							new LocationInit(self.Location + info.MoveOffset),
							new CenterPositionInit(self.CenterPosition + info.Offset),
							new OwnerInit(self.Owner),
							new FactionInit(self.Owner.Faction.InternalName),
							new FacingInit(0)
						};


						self.World.AddFrameEndTask(w =>
						{
							RespawnActor = w.CreateActor(info.SpawnActor, td);
							var moveto = RespawnActor.TraitOrDefault<IMove>();
							RespawnActor.QueueActivity(move.MoveIntoWorld(RespawnActor, exit));
							RespawnActor.QueueActivity(move.MoveTo(exit, 2));

							if (RespawnActor.Info.HasTraitInfo<HarvesterInfo>())
							{
								RespawnActor.QueueActivity(new FindResources(RespawnActor));
							}

							if (InUse.Contains(RespawnActor))
								InUse.Remove(RespawnActor);
						});
					}));
					//set reached units state +1 and units in movement state -1
					actor.QueueActivity(new RemoveSelf()); //of he goes
				}));
			}
		}


		public bool StillValid(Actor actor, Actor self)
		{

			if (actor.IsInWorld && !actor.IsDead && self.IsInWorld && !self.IsDead)
			{
				if (InUse.Contains(actor))
					InUse.Remove(actor);
				return false;
			}
			return true;
		}

        public void RemovedFromWorld(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                if (InUse.Any())
                {
                    foreach (var var in InUse)
                    {
                        if (!var.IsDead && var.IsInWorld && var.Info.HasTraitInfo<IsPeasantInfo>())
                        {
                            var.Trait<IsPeasant>().setPeasant();
                        }
                    }
                }
            }
        }
    }
	
	

}

