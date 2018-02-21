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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;


namespace OpenRA.Mods.MW.Traits
{
    [Desc("A actor has to enter the building before the unit spawns.")]
    public class WithActorProductionInfo : ProductionInfo, Requires<ExitInfo>
    {
        public readonly string ReadyAudio = "UnitReady";

        [Desc("Valid actortypes wich can pe used to produce/ convert.")]
        public readonly HashSet<string> TrainingActors = new HashSet<string>();

        [Desc("The range in cells where should be looked for.")]
        public readonly int FindRadius = 0;

        [Desc("Go direct into the entry not checking anything else.")]
        public readonly bool GoDirect = false;

        public override object Create(ActorInitializer init) { return new WithActorProduction(init, this); }
    }

    class WithActorProduction : Production, INotifyRemovedFromWorld
    {
        readonly WithActorProductionInfo info;
        public HashSet<Actor> inuse = new HashSet<Actor>();

        public WithActorProduction(ActorInitializer init, WithActorProductionInfo info)
            : base(init, info)
        {
            this.info = info;
        }

        public bool FactoriesGet(Actor self, Actor actor)
        {


            foreach (var n in self.World.ActorsHavingTrait<WithActorProduction>()
                .Where(a =>
                {
                    if (a.Owner != self.Owner)
                        return false;
                    return true;
                }).ToHashSet())
            {
                var FindTraitWithActorProduction = n.TraitsImplementing<WithActorProduction>().FirstOrDefault();
                var hashcode = FindTraitWithActorProduction.inuse;

                if (hashcode.Contains(actor))
                {
                    return true;
                }
            }

            return false;
        }

        public bool FreeSpawnable(Actor self, Actor actor)
        {


            foreach (var n in self.World.ActorsHavingTrait<WithFreeSpawnableActor>()
                .Where(a =>
                {
                    if (a.Owner != self.Owner)
                        return false;
                    return true;
                }).ToHashSet())
            {
                var howmany = n.TraitsImplementing<WithFreeSpawnableActor>();

                foreach (var b in howmany)
                {
                    if (b.inuse.Contains(actor))
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

                        if (ValidList.Count > 0)
                        {
                            if (ValidList.Contains(a.Info.Name))
                                return true;
                        }
                        else
                        {
                            if (info.TrainingActors.Contains(a.Info.Name))
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

        public bool StillValid(Actor actor, Actor self)
        {

            if (!actor.IsInWorld || actor.IsDead)
            {
                if (inuse.Contains(actor))
                {
                    inuse.Remove(actor);
                    return true;
                }
            }
            return false;
        }

        public override bool Produce(Actor self, ActorInfo producee, string productionType, TypeDictionary inits)
        {

            if (self.Owner.PlayerActor.TraitOrDefault<DeveloperMode>() != null && self.Owner.PlayerActor.TraitOrDefault<DeveloperMode>().FastBuild)
            {

                var newexit2 = self.Info.TraitInfos<ExitInfo>().FirstOrDefault();

                self.World.AddFrameEndTask(ww => DoProduction(self, producee, newexit2, productionType, inits));
                Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio,
                    self.Owner.Faction.InternalName);
                return true;
            }

            //basic setup of values
            var owner = self.Owner;

            //find produced unit cost values
            var ValidActors = new HashSet<string>(); ;
            var Actorcount = 1;

            var guysFound = new HashSet<Actor>();
            var alreadyReached = 0;

            //find enough actors
            if (producee.HasTraitInfo<PersonValuedInfo>())
            {

                var BuilderInfo = producee.TraitInfo<PersonValuedInfo>();
                ValidActors = BuilderInfo.ConvertingActors;
                Actorcount = BuilderInfo.ActorCount;

                for (int i = 0; i < Actorcount; i++)
                {
                    var poss = PossibleActor(self, guysFound, ValidActors);
                    if (poss != null)
                        guysFound.Add(poss);
                }
            }
            else
            {
                var poss = PossibleActor(self, new HashSet<Actor>(), new HashSet<string>());
                if (poss != null)
                    guysFound.Add(poss);
            }



            //find exit cell and spawn locations
            var newexit = self.Info.TraitInfos<ExitInfo>().FirstOrDefault();

            if (guysFound.Count < Actorcount)
                return false;

            if (Actorcount > 0)
                foreach (var actor in guysFound)
                {
                    owner.World.AddFrameEndTask(w =>
                    {
                        /*
						//Actor is possible to move?
						var move = actor.TraitOrDefault<IMove>();

						if ((!actor.IsInWorld || !actor.IsDead))
						{
							//safe in array and set underway statement +1
							InUse.Add(actor);

							self.Owner.PlayerActor.Trait<PlayerCivilization>().SpawnStoredPeasant();
							//beginn movement
							actor.CancelActivity();

							if (!info.GoDirect)
							{
								actor.QueueActivity(move.MoveTo(exit, 5));
							}

                            if (!actor.IsDead && actor.IsInWorld && actor.Info.HasTraitInfo<IsPeasantInfo>())
                            {
                                var peas = actor.TraitsImplementing<IsPeasant>().FirstOrDefault();
                                peas.setWroking();
                            }

                            //what happens when actor or barracks dies
                            actor.QueueActivity(new CallFunc(() =>
							{
	
								if (StillValid(actor, self))
								{
									return;
								}
								//if not died continue and recalculate ow position
	
								//visually enter the building
								var selfposition = actor.CenterPosition;
								//actor.QueueActivity(move.MoveIntoWorld(actor,exit));
								if (!info.GoDirect)
								{
									actor.QueueActivity(move.VisualMove(actor, selfposition, infiltrate));
								}
								//if dead before finished
								actor.QueueActivity(new CallFunc(() =>
                                {
                                    if (StillValid(actor, self))
                                        return;

                                    if (!self.IsInWorld || self.IsDead)
                                    {
                                        return;
                                    }
                                    //if not continue

                                    */

                        alreadyReached++;
                        if (alreadyReached >= Actorcount)
                        {
                            self.World.AddFrameEndTask(ww => DoProduction(self, producee, newexit, productionType, inits));
                            Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio,
                                self.Owner.Faction.InternalName);
                        }

                        //}));
                        //set reached units state +1 and units in movement state -1
                        var destination = actor.Owner.PlayerActor.Trait<PlayerCivilization>().PeasantProvider.Any() ? actor.Owner.PlayerActor.Trait<PlayerCivilization>().PeasantProvider.ClosestTo(actor) : null;
                        if (destination == null)
                        {
                            actor.CancelActivity();
                            actor.QueueActivity(new RemoveSelf());
                            if (inuse.Contains(actor))
                            {
                                inuse.Remove(actor);
                            }
                        }
                        // actor.QueueActivity(new RemoveSelf()); //of he goes
                        else
                        {
                            inuse.Add(actor);
                            var move = actor.TraitOrDefault<IMove>();

                            var exitinfo = destination.Info.TraitInfo<ProvidesLivingspaceInfo>();
                            var exitcell = destination.Location + exitinfo.ExitCell;
                            var spawn = destination.CenterPosition + exitinfo.SpawnOffset;

                            self.Owner.PlayerActor.Trait<PlayerCivilization>().SpawnStoredPeasant();
                            //beginn movement
                            actor.CancelActivity();

                            if (!info.GoDirect)
                            {
                                actor.QueueActivity(move.MoveTo(exitcell, 5));
                            }

                            if (!actor.IsDead && actor.IsInWorld && actor.Info.HasTraitInfo<IsPeasantInfo>())
                            {
                                var peas = actor.TraitsImplementing<IsPeasant>().FirstOrDefault();
                                peas.setWroking();
                            }

                            //what happens when actor or barracks dies
                            actor.QueueActivity(new CallFunc(() =>
                            {

                                if (StillValid(actor, self))
                                {
                                    return;
                                }
                                //if not died continue and recalculate ow position

                                //visually enter the building
                                var selfposition = actor.CenterPosition;
                                //actor.QueueActivity(move.MoveIntoWorld(actor,exit));
                                if (!info.GoDirect)
                                {
                                    actor.QueueActivity(move.VisualMove(actor, selfposition, spawn));
                                }
                                actor.QueueActivity(new CallFunc(() =>
                                {
                                    if (StillValid(actor, self))
                                        return;
                                    //if dead before finished
                                    actor.QueueActivity(new RemoveSelf());
                                    if (inuse.Contains(actor))
                                    {
                                        inuse.Remove(actor);
                                    }
                                }));
                            }));
                        }
                    });
                }
            else
            {
                self.World.AddFrameEndTask(ww => DoProduction(self, producee, newexit, productionType, inits));
                Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio,
                    self.Owner.Faction.InternalName);
            }
            return true;
        }

        void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
        {
            if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                if (inuse.Any())
                {
                    foreach (var var in inuse)
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