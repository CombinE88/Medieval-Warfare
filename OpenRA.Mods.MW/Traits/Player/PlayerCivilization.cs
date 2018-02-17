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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.MW.Traits;
using OpenRA.Primitives;

namespace OpenRA.Traits
{
    public class PlayerCivilizationInfo : ITraitInfo
    {
        [Desc("Basetime in seconds, before the game checks if a new Peasant can spawn.")]
        public readonly int Delay = 10;
        [Desc("Each Ammount in ProvidesLivingspaces reduces this time by this modifier.")]
        public readonly int SpawnModifier = 1;
        [Desc("Maximal ammount of Peasants that leave the houses at a same time (new will instantly spawn when gets lower of that point).")]
        public readonly int AlivePeasants = 15;
        [Desc("Each Ammount in ProvidesLivingspaces reduces this time by this modifier for this faction.")]
        public readonly Dictionary<string, int> SpecialModifier = new Dictionary<string, int>();

        public readonly HashSet<string> Peasants = new HashSet<string>();

        public readonly bool AllowModifiers = true;

        public object Create(ActorInitializer init) { return new PlayerCivilization(init.Self, this); }
    }


    public class PlayerCivilization : ITick, INotifyCreated
    {
        readonly PlayerCivilizationInfo info;
        public int nextchecktick;
        public int basecheck;
        private Player player;

        public int Peasantpopulationvar; // number of idle peasants
        public int MaxLivingspacevar; // Maximal Beds
        public int WorkerPopulationvar; // how many people use a bed (Peasants + Other)

        public int FreePopulation; // Free beds

        public int hiddenpeasants; // How many Peasants are not outside the building but still count

        public decimal PercentageModifier;
        public int DirectModifier;


        public HashSet<Actor> PeasantProvider = new HashSet<Actor>();

        void INotifyCreated.Created(Actor self)
        {
            nextchecktick += info.Delay * 25;
            basecheck += info.Delay * 25;
            player = self.Owner;
            PercentageModifier = 100;
            DirectModifier = 0;
        }

        public PlayerCivilization(Actor self, PlayerCivilizationInfo info)
        {
            this.info = info;
            nextchecktick += info.Delay * 25;
            basecheck += info.Delay * 25;
        }

        public Actor RandomBuildingWithLivingspace() // Use a random possible buildings that can hold peasants and selects one to spawn from. The list gets updated when specific actors die or spawn
        {
            if (PeasantProvider.Any())
            {
                return PeasantProvider.Random(player.World.SharedRandom);
            }
            return null;
        }

        private void AddHiddenPeasant() // add a new peasant but don't spawn it so the actor count is reduced.
        {
            hiddenpeasants += 1;
            Peasantpopulationvar += 1;
        }

        private void AddRegularPeasant(Actor SpawnPosition) // Spawn a peasant of a before selected building
        {
            player.World.AddFrameEndTask(w =>
            {
                if (SpawnPosition.Disposed || SpawnPosition.IsDead || !SpawnPosition.IsInWorld)
                    return;

                var randomspawnedpeas = info.Peasants.Random(player.World.SharedRandom); // select random peasant actor

                var exitinfo = SpawnPosition.Info.TraitInfo<ProvidesLivingspaceInfo>();
                var exit = SpawnPosition.Location + exitinfo.ExitCell;
                var spawn = SpawnPosition.CenterPosition + exitinfo.SpawnOffset;

                var td = new TypeDictionary
                        {
                            new CenterPositionInit(spawn),
                            new OwnerInit(player),
                            new ParentActorInit(SpawnPosition),
                            new FactionInit(player.Faction.InternalName)
                        };

                var peas = w.CreateActor(randomspawnedpeas, td);
                if (peas != null)
                    if (!peas.IsDead && peas.IsInWorld)
                    {
                        var move = peas.TraitOrDefault<IMove>();
                        peas.QueueActivity(move.MoveIntoWorld(peas, exit, SubCell.Any));
                    }
            });
        }

        public void Spawnapeasant() // Peasant spawn initiater and selection if peasant stays at home or not
        {
            var SpwanPosition = RandomBuildingWithLivingspace();
            if (SpwanPosition != null)
            {
                if (Peasantpopulationvar - hiddenpeasants >= info.AlivePeasants) // stay at home when maximum peasants are outside and generate a "hidden" one
                {
                    AddHiddenPeasant();
                }
                else
                {
                    AddRegularPeasant(SpwanPosition);
                }
            }
        }

        public void SpawnStoredPeasant() // checks if less than maximum peasants are outside and idle, if not spawn a peasant via this tick, shoots everytime a change in the population happens
        {
            if (hiddenpeasants > 0 && Peasantpopulationvar - hiddenpeasants < info.AlivePeasants)
            {
                var SpwanPosition = RandomBuildingWithLivingspace();
                if (SpwanPosition != null)
                {
                    hiddenpeasants -= 1;  // remove the hidden peasant from the counter
                    Peasantpopulationvar -= 1; // reduce the peasant counter, will be increased automaticly while spawning the actor

                    AddRegularPeasant(SpwanPosition);
                }

            }

        }

        /* The following block recalculates all village values due to spawn and death of units.
         * each time a unit with specific traits (IsPeasant and PersonValued) dies or spawns
         * all village specific values have to be recalculated. to reduce the lag during each tick i decided to calculate only when an actual event of such happens.         * 
         */
        public void Recalculate()
        {
            RecalculatePopulation();
            SpawnStoredPeasant();
        }

        private void RecalculatePopulation() // Recalculates the population values
        {
            if (player.Faction.InternalName != "ded")
            {
                FreePopulation = (MaxLivingspacevar) - (WorkerPopulationvar + Peasantpopulationvar);
            }
            else
            {
                FreePopulation = MaxLivingspacevar - WorkerPopulationvar;
            }
        }

        private void ResetSpawn() // Reset the peasant spawn bar and recalculate a new bar with new values and modifiers
        {
            nextchecktick = info.Delay * 25;
            var everyonereduction = 0;


            if (FreePopulation <= 100 && FreePopulation > 0)
            {
                everyonereduction = (int)(decimal)(-0.005 * ((FreePopulation - 100) * (FreePopulation - 100)) + 50);
                //Game.Debug("" + everyonereduction, everyonereduction);
                //Log.Write("debug", "" + everyonereduction);
            }
            else if (FreePopulation > 100)
            {
                everyonereduction = 50;
            }

            if (info.SpecialModifier.Any())
                foreach (var var in info.SpecialModifier)
                {
                    if (player.Faction.InternalName == var.Key)
                    {
                        everyonereduction += everyonereduction / 100 * var.Value;
                        //spawn2 = (FreePopulation * var.Value) / 2 > nextchecktick/3 ? nextchecktick/3 : (FreePopulation * var.Value) / 2;
                    }
                }

            nextchecktick -= everyonereduction;

            decimal devider = Decimal.Round(100 / PercentageModifier, 2);

            basecheck = (int)(decimal)(nextchecktick * devider);
            basecheck -= DirectModifier;

            if (basecheck < 25)
                basecheck = 25;
        }

        void ITick.Tick(Actor self)
        {
            if (player.Faction.InternalName != "ded")
            {
                if (FreePopulation > 0)
                    basecheck--;

                if (basecheck <= 0)
                {
                    Spawnapeasant();
                    ResetSpawn();
                }
            }
        }
    }
}
