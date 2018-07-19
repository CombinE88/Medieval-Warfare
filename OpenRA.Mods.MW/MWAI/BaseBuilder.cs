#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.MW.MWAI
{
	class BaseBuilder
	{
		readonly string category;

		readonly HackyAI ai;
		readonly World world;
		readonly Player player;
		readonly PlayerResources playerResources;

		int waitTicks;
		Actor[] playerBuildings;
		int failCount;
		int failRetryTicks;
		int checkForBasesTicks;
		int cachedBases;
		int cachedBuildings;

		enum Water
		{
			NotChecked,
			EnoughWater,
			NotEnoughWater
		}

		Water waterState = Water.NotChecked;

		public BaseBuilder(HackyAI ai, string category, Player p, PlayerResources pr)
		{
			this.ai = ai;
			world = p.World;
			player = p;
			playerResources = pr;
			this.category = category;
			failRetryTicks = ai.Info.StructureProductionResumeDelay;
		}

		public void Tick()
		{
			// If failed to place something N consecutive times, wait M ticks until resuming building production
			if (failCount >= ai.Info.MaximumFailedPlacementAttempts && --failRetryTicks <= 0)
			{
				var currentBuildings = world.ActorsHavingTrait<Building>().Count(a => a.Owner == player);
				var baseProviders = world.ActorsHavingTrait<BaseProvider>().Count(a => a.Owner == player);

				// Only bother resetting failCount if either a) the number of buildings has decreased since last failure M ticks ago,
				// or b) number of BaseProviders (construction yard or similar) has increased since then.
				// Otherwise reset failRetryTicks instead to wait again.
				if (currentBuildings < cachedBuildings || baseProviders > cachedBases)
					failCount = 0;
				else
					failRetryTicks = ai.Info.StructureProductionResumeDelay;
			}

			if (waterState == Water.NotChecked)
			{
				if (ai.IsAreaAvailable<BaseProvider>(ai.Info.MaxBaseRadius, ai.Info.WaterTerrainTypes))
					waterState = Water.EnoughWater;
				else
				{
					waterState = Water.NotEnoughWater;
					checkForBasesTicks = ai.Info.CheckForNewBasesDelay;
				}
			}

			if (waterState == Water.NotEnoughWater && --checkForBasesTicks <= 0)
			{
				var currentBases = world.ActorsHavingTrait<BaseProvider>().Count(a => a.Owner == player);

				if (currentBases > cachedBases)
				{
					cachedBases = currentBases;
					waterState = Water.NotChecked;
				}
			}

			// Only update once per second or so
			if (--waitTicks > 0)
				return;

			playerBuildings = world.ActorsHavingTrait<Building>().Where(a => a.Owner == player && !ai.Info.UndeadCommonNames.Gravestones.Contains(a.Info.Name)).ToArray();

			var active = false;
			foreach (var queue in ai.FindQueues(category))
				if (TickQueue(queue))
					active = true;

			// Add a random factor so not every AI produces at the same tick early in the game.
			// Minimum should not be negative as delays in HackyAI could be zero.
			var randomFactor = ai.Random.Next(0, ai.Info.StructureProductionRandomBonusDelay);

			// Needs to be at least 4 * OrderLatency because otherwise the AI frequently duplicates build orders (i.e. makes the same build decision twice)
			waitTicks = active ? 4 * world.LobbyInfo.GlobalSettings.OrderLatency + ai.Info.StructureProductionActiveDelay + randomFactor
				: ai.Info.StructureProductionInactiveDelay + randomFactor;
		}

		bool TickQueue(ProductionQueue queue)
		{
			var currentBuilding = queue.CurrentItem();

			// Waiting to build something
			if (currentBuilding == null && failCount < ai.Info.MaximumFailedPlacementAttempts)
			{
				var item = ChooseBuildingToBuild(queue);
				if (item == null)
					return false;

				ai.QueueOrder(Order.StartProduction(queue.Actor, item.Name, 1));
			}
			else if (currentBuilding != null && currentBuilding.Done)
			{
				// Production is complete
				// Choose the placement logic
				// HACK: HACK HACK HACK
				// TODO: Derive this from BuildingCommonNames instead
				var type = BuildingType.Building;
				if (world.Map.Rules.Actors[currentBuilding.Item].HasTraitInfo<AttackBaseInfo>())
					type = BuildingType.Defense;
				else if (ai.Info.BuildingCommonNames.Refineries.Contains(world.Map.Rules.Actors[currentBuilding.Item].Name.ToLower()))
					type = BuildingType.Refinery;
				else if (ai.Info.BuildingCommonNames.Farms.Contains(world.Map.Rules.Actors[currentBuilding.Item].Name.ToLower()))
					type = BuildingType.Farm;
				else if (ai.Info.UndeadCommonNames.Sppool.Contains(world.Map.Rules.Actors[currentBuilding.Item].Name.ToLower()))
					type = BuildingType.Sppool;
				else if (ai.Info.BuildingCommonNames.ImportantBuildings.Contains(world.Map.Rules.Actors[currentBuilding.Item].Name.ToLower()))
					type = BuildingType.Important;
				else if (ai.Info.BuildingCommonNames.Houses.Contains(world.Map.Rules.Actors[currentBuilding.Item].Name.ToLower()))
					type = BuildingType.Regular;
				else if (ai.Info.BuildingCommonNames.Hunter.Contains(world.Map.Rules.Actors[currentBuilding.Item].Name.ToLower()))
					type = BuildingType.Hunter;
				else if (ai.Player.Faction.InternalName == "ded")
					type = BuildingType.Undead;

				var location = ai.ChooseBuildLocation(currentBuilding.Item, true, type);
				if (location == null)
				{
					HackyAI.BotDebug("AI: {0} has nowhere to place {1}".F(player, currentBuilding.Item));
					ai.QueueOrder(Order.CancelProduction(queue.Actor, currentBuilding.Item, 1));
					failCount += failCount;

					// If we just reached the maximum fail count, cache the number of current structures
					if (failCount == ai.Info.MaximumFailedPlacementAttempts)
					{
						cachedBuildings = world.ActorsHavingTrait<Building>().Count(a => a.Owner == player);
						cachedBases = world.ActorsHavingTrait<BaseProvider>().Count(a => a.Owner == player);
					}
				}
				else
				{
					failCount = 0;
					ai.QueueOrder(new Order("PlaceBuilding", player.PlayerActor, Target.FromCell(world, location.Value), false)
					{
						// Building to place
						TargetString = currentBuilding.Item,

						// Actor ID to associate the placement with
						ExtraData = queue.Actor.ActorID,
						SuppressVisualFeedback = true
					});

					return true;
				}
			}

			return true;
		}

		ActorInfo GetProducibleBuilding(HashSet<string> actors, IEnumerable<ActorInfo> buildables, Func<ActorInfo, int> orderBy = null)
		{
			var available = buildables.Where(actor =>
			{
				// Are we able to build this?
				if (!actors.Contains(actor.Name))
					return false;

				if (!ai.Info.BuildingLimits.ContainsKey(actor.Name))
					return true;

				return playerBuildings.Count(a => a.Info.Name == actor.Name) < ai.Info.BuildingLimits[actor.Name];
			});

			if (orderBy != null)
				return available.MaxByOrDefault(orderBy);

			return available.RandomOrDefault(ai.Random);
		}

		ActorInfo ChooseBuildingToBuild(ProductionQueue queue)
		{
			if (ai.Player.Faction.InternalName != "ded")
			{
				var buildableThings = queue.BuildableItems();

				var houses = GetProducibleBuilding(ai.Info.BuildingCommonNames.Houses, buildableThings,
					a => a.TraitInfos<ValuedInfo>().Sum(p => p.Cost));

				// This gets used quite a bit, so let's cache it here
				var population = ai.CountPossiblePopulation() + ai.CountPotentialFreeBeds();

				// First priority is to get out of a low power situation
				if (population < ai.Info.Population)
				{
					if (houses != null)
					{
						HackyAI.BotDebug("AI: {0} decided to build {1}: Priority override (low population) Population: " + population, queue.Actor.Owner, houses.Name);
						return houses;
					}
					if (houses == null && population < ai.Info.MinimumPeasants)
						return null;
				}

				// Next is to build up a strong economy
				if (!ai.HasAdequateFarm() || !ai.HasMinimumFarm())
				{
					var farm = GetProducibleBuilding(ai.Info.BuildingCommonNames.Farms, buildableThings);
					if (farm != null && population > ai.Info.MinimumPeasants)
					{
						HackyAI.BotDebug("AI: {0} decided to build {1}: Priority override (refinery)", queue.Actor.Owner, farm.Name);
						return farm;
					}

					if ((houses != null && farm != null) && population < ai.Info.Population)
					{
						HackyAI.BotDebug("AI: {0} decided to build {1}: Priority override (low population) Population: " + population, queue.Actor.Owner, houses.Name);
						return houses;
					}
				}

				// Make sure that we can spend as fast as we are earning
				if (ai.Info.NewProductionCashThreshold > 0 && playerResources.Resources > ai.Info.NewProductionCashThreshold)
				{
					var production = GetProducibleBuilding(ai.Info.BuildingCommonNames.Production, buildableThings);
					if (production != null && population >= ai.Info.Population)
					{
						HackyAI.BotDebug("AI: {0} decided to build {1}: Priority override (production)", queue.Actor.Owner, production.Name);
						return production;
					}

					if (houses != null && production != null && population < ai.Info.Population)
					{
						HackyAI.BotDebug("{0} decided to build {1}: Priority override (would be low power)", queue.Actor.Owner, houses.Name);
						return houses;
					}
				}

				// Only consider building this if there is enough water inside the base perimeter and there are close enough adjacent buildings
				if (waterState == Water.EnoughWater && ai.Info.NewProductionCashThreshold > 0
					&& playerResources.Resources > ai.Info.NewProductionCashThreshold
					&& ai.IsAreaAvailable<GivesBuildableArea>(ai.Info.CheckForWaterRadius, ai.Info.WaterTerrainTypes))
				{
					var navalproduction = GetProducibleBuilding(ai.Info.BuildingCommonNames.NavalProduction, buildableThings);
					if (navalproduction != null && population >= ai.Info.Population)
					{
						HackyAI.BotDebug("AI: {0} decided to build {1}: Priority override (navalproduction)", queue.Actor.Owner, navalproduction.Name);
						return navalproduction;
					}

					if (houses != null && navalproduction != null && population < ai.Info.Population)
					{
						HackyAI.BotDebug("{0} decided to build {1}: Priority override (would be low power)", queue.Actor.Owner, houses.Name);
						return houses;
					}
				}

				// Build everything else
				foreach (var frac in ai.Info.BuildingFractions.Shuffle(ai.Random))
				{
					var name = frac.Key;

					if (!ai.HasAdequateFarm() && (!ai.Info.BuildingCommonNames.Houses.Contains(name) || !ai.Info.BuildingCommonNames.Hunter.Contains(name)) && (!ai.Info.BuildingCommonNames.Hunter.Contains(name) || ai.Player.Faction.InternalName == "nod"))
						continue;

					if ((ai.Info.BuildingCommonNames.Houses.Contains(name) && population > 40))
						continue;

					// Can we build this structure?
					if (!buildableThings.Any(b => b.Name == name))
						continue;

					// Do we want to build this structure?
					var count = playerBuildings.Count(a => a.Info.Name == name) + playerBuildings.Count(a => a.Info.Name == name.Replace(".scaff", string.Empty).ToLower());
					if (count > frac.Value * playerBuildings.Length)
						continue;

					if (ai.Info.BuildingLimits.ContainsKey(name) && ai.Info.BuildingLimits[name] <= count)
						continue;

					// If we're considering to build a naval structure, check whether there is enough water inside the base perimeter
					// and any structure providing buildable area close enough to that water.
					// TODO: Extend this check to cover any naval structure, not just production.

					// Will this put us into low power?
					var actor = world.Map.Rules.Actors[name];

					// Lets build this
					HackyAI.BotDebug("{0} decided to build {1}: Desired is {2} ({3} / {4}); current is {5} / {4}",
						queue.Actor.Owner, name, frac.Value, frac.Value * playerBuildings.Length, playerBuildings.Length, count);
					return actor;
				}

				// Too spammy to keep enabled all the time, but very useful when debugging specific issues.
				// HackyAI.BotDebug("{0} couldn't decide what to build for queue {1}.", queue.Actor.Owner, queue.Info.Group);
				return null;
			}
			else
			{
				var buildableThings = queue.BuildableItems();

				var pentagrams = ai.CountPenagrams();
				//var builder = ai.AcolyteBuilder.Count();

				if (pentagrams > 1)
					return null;

				if ((ai.HasAdequateCrypts() * 3) < playerBuildings.Count())
				{
					var crypt = GetProducibleBuilding(ai.Info.UndeadCommonNames.Crypts, buildableThings);
					if (crypt != null)
					{
						HackyAI.BotDebug("AI: {0} decided to build {1}: Priority override (production)", queue.Actor.Owner, crypt.Name);
						return crypt;
					}
				}

				// Build everything else
				foreach (var frac in ai.Info.BuildingFractions.Shuffle(ai.Random))
				{
					var name = frac.Key;

					//if (!ai.Info.UndeadCommonNames.EarlyUpgrades.Contains(name) && ai.Info.BuildingLimits.ContainsKey(name))
					//	continue;

					// Can we build this structure?
					if (!buildableThings.Any(b => b.Name == name))
						continue;

					if (ai.HasAdequateCrypts() * 3 >= playerBuildings.Count() && ai.Info.UndeadCommonNames.Crypts.Contains(name))
						continue;

					// Do we want to build this structure?
					var count = playerBuildings.Count(a => a.Info.Name == name) + playerBuildings.Count(a => a.Info.Name == name.Replace(".penta", string.Empty).ToLower());
					if (count > frac.Value * playerBuildings.Length)
						continue;

					if (ai.Info.BuildingLimits.ContainsKey(name) && ai.Info.BuildingLimits[name] <= count)
						continue;

					var actor = world.Map.Rules.Actors[name];

					//if (ai.Info.UndeadCommonNames.Crypts.Contains(name) && ai.HasAdequateCrypts() * 3 >= playerBuildings.Count())
					//	continue;

					// Lets build this
					HackyAI.BotDebug("{0} decided to build {1}: Desired is {2} ({3} / {4}); current is {5} / {4}",
						queue.Actor.Owner, name, frac.Value, frac.Value * playerBuildings.Length, playerBuildings.Length, count);
					return actor;
				}
				// Too spammy to keep enabled all the time, but very useful when debugging specific issues.
				// HackyAI.BotDebug("{0} couldn't decide what to build for queue {1}.", queue.Actor.Owner, queue.Info.Group);
				return null;
			}
		}
	}
}
