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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
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

    class DeerHunter : INotifyCreated, INotifyIdle, INotifyKilled
    {
        private readonly DeerHunterInfo info;
        private Actor self;

        private Actor deerHunt;
        public Actor DeerLoot;
        private Actor lodge;

        Actor nextLootTarget;
        Actor nextHunttarget;

        private int tick;

        AmmoPool ammoPool;
        int whatAmmo;

        public DeerHunter(ActorInitializer init, DeerHunterInfo info)
        {
            this.info = info;
            self = init.Self;
        }

        // Set homeLodge when created
        void INotifyCreated.Created(Actor self)
        {
            ammoPool = self.TraitsImplementing<AmmoPool>().FirstOrDefault(la => la.Info.Name == "Food");
            whatAmmo = ammoPool.GetAmmoCount();
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
                lodge = possiblelodges.ClosestTo(self);
                tick = 10;
            }
        }

        public Actor FindLootableDeer()
        {
            var lootableDeer = lodge.World.FindActorsInCircle(lodge.CenterPosition, info.SearchDistance).Where(a =>
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

            if (lootableDeer.Any())
            {
                closestLoot = lootableDeer.ClosestTo(self);
                closestLoot.Trait<LootableBody>().Hunter = self;
                DeerLoot = closestLoot;
                return closestLoot;
            }

            return null;
        }

        public Actor FindhuntableDeer()
        {
            var huntableDeer = lodge.World.FindActorsInCircle(lodge.CenterPosition, info.SearchDistance).Where(a =>
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

            if (huntableDeer.Any())
            {
                closestHunt = huntableDeer.ClosestTo(self);
                closestHunt.Trait<HuntableDeer>().Hunter = self;
                deerHunt = closestHunt;
                return closestHunt;
            }

            return null;
        }

        void Attack(Actor self, Actor targetActor)
        {
            self.QueueActivity(new Attack(self, Target.FromActor(targetActor), true, true, 100));
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
            var imove = self.TraitOrDefault<IMove>();
            var target = targetActor.Location + new CVec(self.World.SharedRandom.Next(-2, 3), self.World.SharedRandom.Next(-2, 3));
            self.QueueActivity(new AttackMoveActivity(self, imove.MoveTo(target, 4)));
        }

        void HunterDecisions(Actor self)
        {
            whatAmmo = ammoPool.GetAmmoCount(); // how many loot do we have stored

            if ((lodge == null || lodge.IsDead || !lodge.IsInWorld) && self.IsInWorld && !self.IsDead)
            {
                // first, find a new home when our old got destroyed
                FindALodge();
            }
            else if (whatAmmo > 0 && self.IsInWorld && !self.IsDead)
            {
                // if we got loot stored let us deliver first
                Move(self, lodge);
                Attack(self, lodge);
            }
            else if (DeerLoot != null && !DeerLoot.IsDead && DeerLoot.IsInWorld && self.IsInWorld && !self.IsDead)
            {
                // do we have a shot deer somewhere and need to gather from it?
                Move(self, DeerLoot);
                Attack(self, DeerLoot);
            }
            else if (nextLootTarget != null && !nextLootTarget.IsDead && nextLootTarget.IsInWorld && self.IsInWorld && !self.IsDead)
            {
                // is there a dead dee somehwere that belongs to noone we can loot?
                Move(self, nextLootTarget);
                Attack(self, nextLootTarget);
            }
            else if (deerHunt != null && !deerHunt.IsDead && deerHunt.IsInWorld && self.IsInWorld && !self.IsDead)
            {
                // lets find the deer we already have reserved
                Move(self, deerHunt);
                Attack(self, deerHunt);
            }
            else if (nextHunttarget != null && !nextHunttarget.IsDead && nextHunttarget.IsInWorld && self.IsInWorld && !self.IsDead)
            {
                // lets find a fresh deer noone has reserved yet
                Move(self, nextHunttarget);
                Attack(self, nextHunttarget);
            }
            else if (self.IsInWorld && !self.IsDead)
            {
                MovePlusRandomVector(self, lodge);
                if (DeerLoot == null || (DeerLoot != null && (DeerLoot.IsDead || !DeerLoot.IsInWorld)))
                    nextLootTarget = FindLootableDeer();
                if (deerHunt == null || (deerHunt != null && (deerHunt.IsDead || !deerHunt.IsInWorld)))
                    nextHunttarget = FindhuntableDeer();
            }
        }

        void INotifyIdle.TickIdle(Actor self)
        {
            if (--tick > 0)
                return;

            if (self == null)
                return;
            if (!self.IsInWorld || self.IsDead)
                return;
            if (self.IsInWorld && !self.IsDead && tick <= 0 && self.IsIdle)
            {
                HunterDecisions(self);
                tick = 25; // wait a second
            }
        }

        void INotifyKilled.Killed(Actor self, AttackInfo e)
        {
            if (DeerLoot != null && !DeerLoot.IsDead && DeerLoot.IsInWorld)
                DeerLoot.Trait<LootableBody>().Hunter = null;
        }
    }
}