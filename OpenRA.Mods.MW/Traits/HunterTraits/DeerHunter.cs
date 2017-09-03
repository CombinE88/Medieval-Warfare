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
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenRA.Chat;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.MW.Traits;
using OpenRA.Traits;


namespace OpenRA.Mods.Mw.Traits
{
	[Desc("A actor has to enter the building before the unit spawns.")]
	public class DeerHunterInfo : ITraitInfo
	{

		[Desc("Huntable Type")]
		public readonly HashSet<string> HuntTypes = new HashSet<string>();	
		
		[Desc("Lootable Type")]
		public readonly HashSet<string> LootTypes = new HashSet<string>();	
		
		public readonly WDist SearchDistance = WDist.FromCells(20);
        		
		
		
		
		public object Create(ActorInitializer init) { return new DeerHunter(init, this); }
	}

	class DeerHunter : INotifyCreated, INotifyIdle
	{
		readonly IEnumerable<AttackBase> activeAttackBases;
		private readonly DeerHunterInfo info;
		private Actor self;

		public Actor DeerHunt;
		public Actor DeerLoot;
		public Actor Lodge;
		
		Actor NextLootTarget;
		Actor NextHunttarget;

		private int Tick;
		
		AmmoPool ammoPool;
		int maxAmmo;
		int whatAmmo;


		public DeerHunter(ActorInitializer init, DeerHunterInfo info)
		{
			this.info = info;
			self = init.Self;
			activeAttackBases = self.TraitsImplementing<AttackBase>().ToArray().Where(Exts.IsTraitEnabled);
			
		}

		
		//Set homeLodge when created
		void INotifyCreated.Created(Actor self)
		{
			ammoPool = self.TraitsImplementing<AmmoPool>().FirstOrDefault(la => la.Info.Name == "Food");
			maxAmmo = ammoPool.Info.Ammo;
			whatAmmo = ammoPool.CurrentAmmo;
		}


		public void FindALodge()
		{
			var possiblelodges = self.World.ActorsHavingTrait<LodgeActor>()
				.Where(a =>
				{
					if (a.Owner != self.Owner)
						return false;
					
					if (a.IsDead || !a.IsInWorld)
						return false;
					
					return true;
				});

			if (possiblelodges.Any())
			{
				Lodge = possiblelodges.ClosestTo(self);
				Tick = 10;
			}

		}

		public Actor FindLootableDeer()
		{
			var LootableDeer = Lodge.World.FindActorsInCircle(Lodge.CenterPosition, info.SearchDistance)
				.Where(a =>
				{
					
					if (a.IsDead || !a.IsInWorld)
						return false;

					if (!a.Info.HasTraitInfo<LootableBodyInfo>())
						return false;

					if (a.TraitsImplementing<LootableBody>().FirstOrDefault().Hunter != null)
						return false;

					var contains = false;

					foreach (var n in a.Info.TraitInfoOrDefault<LootableBodyInfo>().LootTypes)
					{
						if (info.LootTypes.Contains(n))
							contains = true;
					}

					return contains;
				});
			Actor closestLoot = null;
			
			if (LootableDeer.Any())
			{

				closestLoot = LootableDeer.ClosestTo(self);
				closestLoot.Trait<LootableBody>().Hunter = self;
				DeerLoot = closestLoot;
				return closestLoot;
			}
			return null;
		}
		
		public Actor FindhuntableDeer()
		{
			var HuntableDeer = Lodge.World.FindActorsInCircle(Lodge.CenterPosition, info.SearchDistance)
				.Where(a =>
				{

					if (a.IsDead || !a.IsInWorld)
						return false;

					if (!a.Info.HasTraitInfo<HuntableDeerInfo>())
						return false;

					if (a.TraitsImplementing<HuntableDeer>().FirstOrDefault().Hunter != null)
							return false;
					
					var contains = false;
					foreach (var n in a.Info.TraitInfoOrDefault<HuntableDeerInfo>().HuntTypes)
					{
						if (info.HuntTypes.Contains(n))
							contains = true;
					}
					return contains;
				});

			Actor closestHunt = null;

			if (HuntableDeer.Any())
			{
				closestHunt = HuntableDeer.ClosestTo(self);
				closestHunt.Trait<HuntableDeer>().Hunter = self;
				DeerHunt = closestHunt;
				return closestHunt;
				
			}
	
			return null;
		}
		
		void Attack(Actor self, Actor targetActor)
		{
			var target = Target.FromActor(targetActor);
			
			self.QueueActivity(new Attack(self,target,true,true));
		}

		void Move(Actor self, Actor targetActor)
		{
			if ((self.CenterPosition - targetActor.CenterPosition).LengthSquared > WDist.FromCells(4).LengthSquared)
			{

				var cells = self.World.Map.FindTilesInCircle(targetActor.Location, 4, false);
				var move = self.TraitOrDefault<IMove>();
				self.QueueActivity(new AttackMoveActivity(self, move.MoveTo(self.ClosestCell(cells), 5)));
			}
		}
		
		void MovePlusRandomVector(Actor self, Actor targetActor)
		{
			var move = self.TraitOrDefault<IMove>();
			self.QueueActivity(new AttackMoveActivity(self,
				move.MoveTo(
					targetActor.Location + new CVec(self.World.SharedRandom.Next(-2, 3), self.World.SharedRandom.Next(-2, 3)), 4)));
		}
		
		
		
		void INotifyIdle.TickIdle(Actor self)
		{

			if (self == null)
				return;
			if (!self.IsInWorld || self.IsDead)
				return;
			
			Tick--;
			if (self.IsInWorld && !self.IsDead && Tick < 1 && self.IsIdle)
			{
				whatAmmo = ammoPool.CurrentAmmo;
				
				if ((Lodge == null || Lodge.IsDead || !Lodge.IsInWorld) && self.IsInWorld && !self.IsDead)
				{
					FindALodge();
				}
				else if (whatAmmo > 0 && self.IsInWorld && !self.IsDead)
				{
					Move(self, Lodge);
					Attack(self, Lodge);
				}
				else if ((DeerLoot != null && !DeerLoot.IsDead && DeerLoot.IsInWorld) && self.IsInWorld && !self.IsDead)
				{
					Move(self, DeerLoot);
					Attack(self, DeerLoot);
				}
				else if ((NextLootTarget != null && !NextLootTarget.IsDead && NextLootTarget.IsInWorld) && self.IsInWorld && !self.IsDead)
				{
					Move(self, NextLootTarget);
					Attack(self, NextLootTarget);
				}
				else if ((DeerHunt != null && !DeerHunt.IsDead && DeerHunt.IsInWorld) && self.IsInWorld && !self.IsDead)
				{
					Move(self, DeerHunt);
					Attack(self, DeerHunt);
				}
				else if ((NextHunttarget != null && !NextHunttarget.IsDead && NextHunttarget.IsInWorld) && self.IsInWorld && !self.IsDead)
				{
					Move(self, NextHunttarget);
					Attack(self, NextHunttarget);
				}
				else if (self.IsInWorld && !self.IsDead)
				{
					MovePlusRandomVector(self, Lodge);
					if (DeerLoot == null || (DeerLoot != null && (DeerLoot.IsDead || !DeerLoot.IsInWorld)))
						NextLootTarget = FindLootableDeer();
					if (DeerHunt == null || (DeerHunt != null && (DeerHunt.IsDead || !DeerHunt.IsInWorld)))
						NextHunttarget = FindhuntableDeer();
				}
				Tick = 10;
			}
		}

	}

}