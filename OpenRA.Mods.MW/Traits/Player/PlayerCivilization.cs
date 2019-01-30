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

        [Desc("Delay (in ticks) between giving out orders to units.")]
        public readonly int AssignRolesInterval = 20;

        public readonly HashSet<string> Peasants = new HashSet<string>();

        public readonly bool AllowModifiers = true;

        public object Create(ActorInitializer init) { return new PlayerCivilization(init.Self, this); }
    }

    public class PlayerCivilization : INotifyCreated
    {
        readonly PlayerCivilizationInfo info;
        public int Nextchecktick;
        public int Basecheck;
        private Player player;

        public int Peasantpopulationvar; // number of idle peasants
        public int MaxLivingspacevar; // Maximal Beds
        public int WorkerPopulationvar; // how many people use a bed (Peasants + Other)

        public int FreePopulation; // Free beds

        public int Hiddenpeasants; // How many Peasants are not outside the building but still count

        public decimal PercentageModifier;
        public int DirectModifier;

        int assignRolesTicks;

        public HashSet<Actor> PeasantProvider = new HashSet<Actor>();
        private bool first;

        void INotifyCreated.Created(Actor self)
        {
            Nextchecktick += info.Delay * 25;
            Basecheck += info.Delay * 25;
            player = self.Owner;
            PercentageModifier = 100;
            DirectModifier = 0;
        }

        public PlayerCivilization(Actor self, PlayerCivilizationInfo info)
        {
            this.info = info;
            Nextchecktick += info.Delay * 25;
            Basecheck += info.Delay * 25;

            if (self.Owner.IsBot)
            {
                assignRolesTicks = 25;
            }
        }

        public void Recalculate()
        {
            RecalculatePopulation();
        }

        private void RecalculatePopulation() // Recalculates the population values
        {
            if (player.Faction.InternalName != "ded")
                FreePopulation = MaxLivingspacevar - (WorkerPopulationvar + Peasantpopulationvar);
            else
                FreePopulation = MaxLivingspacevar - WorkerPopulationvar;
        }
    }
}
