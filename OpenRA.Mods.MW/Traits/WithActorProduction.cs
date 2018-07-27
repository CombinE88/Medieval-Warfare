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

    class WithActorProduction : Production  //, INotifyRemovedFromWorld
    {
        readonly WithActorProductionInfo info;
        public HashSet<Actor> inuse = new HashSet<Actor>();

        public WithActorProduction(ActorInitializer init, WithActorProductionInfo info)
            : base(init, info)
        {
            this.info = info;
        }

        public List<Actor> FindPeasants(Actor self, ActorInfo producee)
        {
            if (producee.HasTraitInfo<PersonValuedInfo>() && producee.TraitInfo<PersonValuedInfo>().ActorCount > 0 && producee.TraitInfo<PersonValuedInfo>().ConvertingActors.Any())
            {
                var validList = self.World.ActorsHavingTrait<IPositionable>()
                    .Where(a => a.Owner == self.Owner && a.Info.HasTraitInfo<IsPeasantInfo>() && producee.TraitInfo<PersonValuedInfo>().ConvertingActors.Contains(a.Info.Name) && a.Trait<IsPeasant>().isWorker == false).ToList();
                return validList;
            }
            return new List<Actor>();
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

            var owner = self.Owner;

            if (!owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            {
                throw new System.Exception("PlayerCivilization Trait not found! Player must have PlayerCivilization trait!");
            }

            var neededPop = 1;


            if (producee.HasTraitInfo<PersonValuedInfo>())
            {
                neededPop = producee.TraitInfo<PersonValuedInfo>().ActorCount;
            }


            if (owner.PlayerActor.Trait<PlayerCivilization>().FreePopulation < neededPop)
            {
                return false;
            }

            //var validpeasants = FindPeasants(self, producee);

            //if (validpeasants.Count() < neededPop)
            //{
            //    return false;
            //}

            //var alreadyReached = 0;

            ////find exit cell and spawn locations
            var newexit = self.Info.TraitInfos<ExitInfo>().FirstOrDefault();

            owner.World.AddFrameEndTask(w =>
            {
            self.World.AddFrameEndTask(ww => DoProduction(self, producee, newexit, productionType, inits));
            Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio,
                   self.Owner.Faction.InternalName);
            });


            ////foreach (var actor in validpeasants)
            //for (var i = 0; i < neededPop; i++)
            //{
            //    var actor = validpeasants[i];

            //    alreadyReached++;
            //    if (!actor.IsDead && actor.IsInWorld && actor.Info.HasTraitInfo<IsPeasantInfo>())
            //    {
            //        var peas = actor.TraitsImplementing<IsPeasant>().FirstOrDefault();
            //        peas.SetWroking();
            //    }
            //    //}));
            //    //set reached units state +1 and units in movement state -1
            //    var destination = actor.Owner.PlayerActor.Trait<PlayerCivilization>().PeasantProvider.Any() ? actor.Owner.PlayerActor.Trait<PlayerCivilization>().PeasantProvider.ClosestTo(actor) : null;
            //    if (destination == null)
            //    {
            //        actor.CancelActivity();
            //        actor.QueueActivity(new RemoveSelf());
            //        if (inuse.Contains(actor))
            //        {
            //            inuse.Remove(actor);
            //        }
            //    }
            //    // actor.QueueActivity(new RemoveSelf()); //of he goes
            //    else
            //    {
            //        inuse.Add(actor);
            //        var move = actor.TraitOrDefault<IMove>();

            //        var exitinfo = destination.Info.TraitInfo<ProvidesLivingspaceInfo>();
            //        var exitcell = destination.Location + exitinfo.ExitCell;
            //        var spawn = destination.CenterPosition + exitinfo.SpawnOffset;

            //        self.Owner.PlayerActor.Trait<PlayerCivilization>().SpawnStoredPeasant();
            //        //beginn movement
            //        actor.CancelActivity();

            //        if (!info.GoDirect)
            //        {
            //            actor.QueueActivity(move.MoveTo(exitcell, 5));
            //        }
            //        //what happens when actor or barracks dies
            //        actor.QueueActivity(new CallFunc(() =>
            //        {

            //            if (StillValid(actor, self))
            //            {
            //                return;
            //            }
            //            //if not died continue and recalculate ow position

            //            //visually enter the building
            //            var selfposition = actor.CenterPosition;
            //            //actor.QueueActivity(move.MoveIntoWorld(actor,exit));
            //            if (!info.GoDirect)
            //            {
            //                actor.QueueActivity(move.VisualMove(actor, selfposition, spawn));
            //            }
            //            actor.QueueActivity(new CallFunc(() =>
            //            {
            //                if (StillValid(actor, self))
            //                    return;
            //                //if dead before finished
            //                actor.QueueActivity(new RemoveSelf());
            //                if (inuse.Contains(actor))
            //                {
            //                    inuse.Remove(actor);
            //                }
            //            }));
            //        }));
            //    }

            //    /*        void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
            //            {
            //                if (!self.Owner.NonCombatant && self.Owner.WinState != WinState.Lost && self.Owner.PlayerActor.Info.HasTraitInfo<PlayerCivilizationInfo>())
            //                {
            //                    if (inuse.Any())
            //                    {
            //                        foreach (var var in inuse)
            //                        {
            //                            if (!var.IsDead && var.IsInWorld && var.Info.HasTraitInfo<IsPeasantInfo>())
            //                            {
            //                                var.Trait<IsPeasant>().SetPeasant();
            //                            }
            //                        }
            //                    }
            //                }
            //            }*/

            //}
            return true;
        }
    }
}