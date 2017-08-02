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
using System.Diagnostics;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Mw.Traits;
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
		
		public readonly HashSet<string> TownHalls = new HashSet<string>();
		public readonly HashSet<string> Peasants = new HashSet<string>();
		
		public readonly int SmallHallBonus = 30;
		public readonly int BigHallBonus = 70;
		
		public object Create(ActorInitializer init) { return new PlayerCivilization(init.Self, this); }
	}
	

	public class PlayerCivilization : ITick, INotifyCreated
	{
		readonly PlayerCivilizationInfo info;
		readonly Player owner;
		readonly World world;
		public int nextchecktick;
		public int basecheck;
		private Player player;
		
		public int Peasantpopulationvar;
		public int MaxLivingspacevar;
		public int WorkerPopulationvar;
		
		public int FreePopulation;

		public int hiddenpeasants;

		public int HasTownHalls;
		public int HasAttrackter;
		

		public HashSet<Actor> PeasantPorivder = new HashSet<Actor>();
		
		void INotifyCreated.Created(Actor self)
		{
			nextchecktick += info.Delay * 25;
			basecheck += info.Delay * 25;
			player = self.Owner;
		}

		public PlayerCivilization(Actor self, PlayerCivilizationInfo info)
		{
			this.info = info;
			nextchecktick += info.Delay * 25;
			basecheck += info.Delay * 25;
		}
		
		public Actor RandomBuildingWithLivingspace(World world)
		{
			if (PeasantPorivder.Any())
			{
				return PeasantPorivder.Random(player.World.SharedRandom);
			}
			return null;
		}
		
		public void spawnapeasant(World world)
		{
			
			var SpwanPosition = RandomBuildingWithLivingspace(world);
			if (SpwanPosition != null)
			{
				if (Peasantpopulationvar-hiddenpeasants >= info.AlivePeasants)
				{
					hiddenpeasants += 1;
					Peasantpopulationvar += 1;
				}
				else
				{
					player.World.AddFrameEndTask(w =>
					{
						if (SpwanPosition.Disposed || SpwanPosition.IsDead || !SpwanPosition.IsInWorld)
							return;

						var randomspawnedpeas = info.Peasants.Random(player.World.SharedRandom);

						var exitinfo = SpwanPosition.Info.TraitInfo<ProvidesLivingspaceInfo>();
						var exit = SpwanPosition.Location + exitinfo.ExitCell;
						var spawn = SpwanPosition.CenterPosition + exitinfo.SpawnOffset;

						var td = new TypeDictionary
						{
							new CenterPositionInit(spawn),
							new OwnerInit(player),
							new ParentActorInit(SpwanPosition),
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
			}
		
		}
		
		public void SpawnStoredPeasant(World world)
		{
			if (hiddenpeasants > 0 && FreePopulation-hiddenpeasants < info.AlivePeasants)
			{
				var SpwanPosition = RandomBuildingWithLivingspace(world);
				if (SpwanPosition != null)
				{
					hiddenpeasants -= 1;
					Peasantpopulationvar -= 1;

					player.World.AddFrameEndTask(w =>
					{
						if (SpwanPosition.Disposed || SpwanPosition.IsDead || !SpwanPosition.IsInWorld)
							return;

						var randomspawnedpeas = info.Peasants.Random(player.World.SharedRandom);

						var exitinfo = SpwanPosition.Info.TraitInfo<ProvidesLivingspaceInfo>();
						var exit = SpwanPosition.Location + exitinfo.ExitCell;
						var spawn = SpwanPosition.CenterPosition + exitinfo.SpawnOffset;

						var td = new TypeDictionary
						{
							new CenterPositionInit(spawn),
							new OwnerInit(player),
							new ParentActorInit(SpwanPosition),
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

			}

		}
		

		public void Tick(Actor self)
		{
			if (self.Owner.Faction.InternalName != "ded")
			{
				FreePopulation = (MaxLivingspacevar) - (WorkerPopulationvar + Peasantpopulationvar);
			}
			{
				FreePopulation = MaxLivingspacevar - WorkerPopulationvar;
			}

			if (self.Owner.Faction.InternalName != "ded")
			{
				SpawnStoredPeasant(world);

			if (FreePopulation>0)
				basecheck--;

				if (basecheck <= 0)
				{
					spawnapeasant(world);

					nextchecktick = info.Delay * 25;
					var spawn2 = 0;
					if (info.SpecialModifier.Any())
						foreach (var var in info.SpecialModifier)
						{
							if (player.Faction.InternalName == var.Key)
							{
								spawn2 = FreePopulation * var.Value;
							}
						}

					nextchecktick -= FreePopulation * info.SpawnModifier;
					nextchecktick -= spawn2;

					var devider = 100;

					devider += HasAttrackter > 0 ? info.SmallHallBonus : 0;
					devider += HasTownHalls > 0 ? info.BigHallBonus : 0;

					basecheck = nextchecktick * 100 / devider;
					if (basecheck < 25)
						basecheck = 25;
				}
			}
		}
	}
}
