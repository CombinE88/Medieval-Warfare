﻿#region Copyright & License Information
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
        private int ticker;
        private Actor respawnActor = null;
        private readonly WithFreeSpawnableActorInfo info;
        private int idlecount;



        public WithFreeSpawnableActor(ActorInitializer init, WithFreeSpawnableActorInfo info) : base(info)
        {
            this.info = info;
            if (!info.SpawnStart)
                ticker = info.RespawnTime;
            idlecount = 25;
        }

        public List<Actor> FindPeasants(Actor self)
        {
            var validList = self.World.ActorsHavingTrait<IPositionable>()
                .Where(a => a.Owner == self.Owner && a.Info.HasTraitInfo<IsPeasantInfo>() && info.TrainingActors.Contains(a.Info.Name) && a.Trait<IsPeasant>().isWorker == false).ToList();
            return validList;
        }


        bool DistanceGreaterAs(Actor a, Actor b, WDist dist)
        {
            return (a.CenterPosition - b.CenterPosition).LengthSquared >= dist.LengthSquared;
        }

        void ForceBouncingActor(Actor self) // set the actor back to its origin when going to far
        {
            respawnActor.CancelActivity();
            respawnActor.QueueActivity(respawnActor.TraitOrDefault<IMove>().MoveTo(self.Location + info.MoveOffset, 2));
        }

        void BounceLassoo(Actor self)
        {
            respawnActor.QueueActivity(respawnActor.TraitOrDefault<IMove>().MoveTo(self.Location + info.MoveOffset, 2));
        }

        void ITick.Tick(Actor self)
        {
            if (IsTraitDisabled)
                return;


            if (info.Sticky && idlecount-- <= 0)
            {
                if (respawnActor != null && !respawnActor.IsDead && respawnActor.IsInWorld)
                {
                    Sticky(self);

                }
                idlecount = 25;
            }
            else if ((respawnActor == null || respawnActor.IsDead || !respawnActor.IsInWorld) && ticker-- <= 0)
            {
                SpawnNewActor(self);
            }
        }

        void Sticky(Actor self)
        {
            if (DistanceGreaterAs(respawnActor, self, Info.ForceLasso))
            {
                ForceBouncingActor(self);
            }
            else if (respawnActor.IsIdle)
            {
                if (DistanceGreaterAs(respawnActor, self, info.Lasso))
                    BounceLassoo(respawnActor);
            }

        }

        void INotifyActorDisposing.Disposing(Actor self)
        {
            if (info.ReturnOnDeath && respawnActor != null && !respawnActor.IsDead && respawnActor.IsInWorld &&
                respawnActor.Info.Name == info.SpawnActor)
            {

                var td = new TypeDictionary
                {
                    new LocationInit(respawnActor.Location),
                    new CenterPositionInit(respawnActor.CenterPosition),
                    new OwnerInit(respawnActor.Owner),
                    new FactionInit(respawnActor.Owner.Faction.InternalName),
                    new FacingInit(0)
                };
                respawnActor.CancelActivity();
                respawnActor.QueueActivity(new RemoveSelf());
                if (info.UseConverting)
                    self.World.CreateActor(info.TrainingActors.ElementAt(self.World.SharedRandom.Next(info.TrainingActors.Count)), td);
            }

        }
        public void CreateActorSpawn(Actor self)
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
                    respawnActor = w.CreateActor(info.SpawnActor, td);
                    var moveto = respawnActor.TraitOrDefault<IMove>();
                    respawnActor.CancelActivity();
                    respawnActor.QueueActivity(moveto.VisualMove(respawnActor, respawnActor.CenterPosition,
                        self.World.Map.CenterOfCell(self.Location + info.MoveOffset)));

                    if (respawnActor.Info.HasTraitInfo<HarvesterInfo>())
                    {
                        respawnActor.QueueActivity(new FindResources(respawnActor));
                    }
                });
            }
        }


        public void SpawnNewActor(Actor self)
        {
            if (!info.UseConverting)
            {
                CreateActorSpawn(self);
                ticker = info.RespawnTime;
                return;
            }

            var owner = self.Owner;

            if (!owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                throw new System.Exception("PlayerCivilization Trait not found! Player must have PlayerCivilization trait!");
            }

            if (owner.PlayerActor.Trait<PlayerCivilization>().Peasantpopulationvar < 1)
            {
                return;
            }

            //basic setup of values
            var exit = self.Location + info.MoveOffset;
            //find produced unit cost values
            var ValidActors = FindPeasants(self);

            if (!ValidActors.Any())
            {
                ticker = 50;
                return;
            }
            //find enough actors
            var actor = ValidActors.ClosestTo(self);

            var infiltrate = self.CenterPosition + info.Offset;
            //Actor is possible to move?
            respawnActor = actor;

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
                    peas.SetWroking();
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

                        CreateActorSpawn(self);
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
                return false;
            }
            return true;
        }

        void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                if (respawnActor != null && !respawnActor.IsDead && respawnActor.IsInWorld && respawnActor.Info.HasTraitInfo<IsPeasantInfo>())
                {
                    respawnActor.Trait<IsPeasant>().SetPeasant();
                }
            }
        }
    }
}

