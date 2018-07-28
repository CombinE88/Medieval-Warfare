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

    class WithActorProduction : Production
    {
        readonly WithActorProductionInfo info;
        private HashSet<Actor> inuse = new HashSet<Actor>();

        public WithActorProduction(ActorInitializer init, WithActorProductionInfo info)
            : base(init, info)
        {
            this.info = info;
        }

        public List<Actor> FindPeasants(Actor self, ActorInfo producee)
        {
            if (producee.HasTraitInfo<PersonValuedInfo>() && producee.TraitInfo<PersonValuedInfo>().ActorCount > 0 && producee.TraitInfo<PersonValuedInfo>().ConvertingActors.Any())
            {
                var validList = self.World.ActorsHavingTrait<IPositionable>().Where(a =>
                {
                    return a.Owner == self.Owner && a.Info.HasTraitInfo<IsPeasantInfo>() &&
                               producee.TraitInfo<PersonValuedInfo>().ConvertingActors.Contains(a.Info.Name) &&
                               a.Trait<IsPeasant>().IsWorker == false;
                }).ToList();
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

            var newexit = self.Info.TraitInfos<ExitInfo>().FirstOrDefault();

            owner.World.AddFrameEndTask(w =>
            {
            self.World.AddFrameEndTask(ww => DoProduction(self, producee, newexit, productionType, inits));
            Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio,
                   self.Owner.Faction.InternalName);
            });

            return true;
        }
    }
}