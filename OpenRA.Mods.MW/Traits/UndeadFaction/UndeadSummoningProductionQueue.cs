#region Copyright & License Information
/*
 * Copyright 2016-2018 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.BuildingTraits
{
	[Desc("This special production queue implements a fake AllQueued, used to instantly place self constructing buildings.")]
	public class UndeadSummoningProductionQueueInfo : ProductionQueueInfo
	{
		public readonly int SplitFractal = 77;

		public readonly bool SingletonConstruction = false;
		public override object Create(ActorInitializer init) { return new UndeadSummoningProductionQueue(init, init.Self.Owner.PlayerActor, this); }
	}

	public class UndeadSummoningProductionQueue : ProductionQueue, INotifyOwnerChanged
	{
		private readonly UndeadSummoningProductionQueueInfo info;
		private bool expectFakeProductionItemRequest;
		private new PlayerResources playerResources;
		private int maxTicker;
		private int currentTicker;

		public UndeadSummoningProductionQueue(ActorInitializer init, Actor playerActor,
			UndeadSummoningProductionQueueInfo info) : base(init, playerActor, info)
		{
			playerResources = playerActor.TraitOrDefault<PlayerResources>();
			this.info = info;
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			ClearQueue();
			// Regenerate the producibles and tech tree state
			oldOwner.PlayerActor.Trait<TechTree>().Remove(this);
			newOwner.PlayerActor.Trait<TechTree>().Update();
		}

		public override IEnumerable<ProductionItem> AllQueued()
		{
			// Pretend to have items queued, to allow direct placement.
			return BuildableItems().Select(buildableItem =>
			{
				// Cost == 0 to not consume money at this point.
				var item = new ProductionItem(this, buildableItem.Name, 0, null, null);

				// Required for GetBuildTime, else the production wont be ready after below Tick().
				expectFakeProductionItemRequest =  	true;

				// Tick once, so the item is done.
				item.Tick(playerResources);
				return item;
			}).ToList();
		}

		public new void BeginProduction(ProductionItem item, bool hasPriority)
		{
			return;
			base.BeginProduction(item, hasPriority);
		}

		public override int GetBuildTime(ActorInfo unit, BuildableInfo bi)
		{
			// Workaround to make above Tick receive a 0 for the production time.
			if (expectFakeProductionItemRequest)
			{
				expectFakeProductionItemRequest = false;
				return 0;
			}

			return base.GetBuildTime(unit, bi);
		}

		protected override void TickInner(Actor self, bool allProductionPaused)
		{
			Queue.RemoveAll(item =>
			{
				var selfConstructingItem = item as SelfConstructingProductionItem;
				return selfConstructingItem == null || selfConstructingItem.Actor.IsDead ||
				       !selfConstructingItem.Actor.IsInWorld;
			});

			if (Queue.Count > 0 && !allProductionPaused)
			{
				if (!info.SingletonConstruction)
				{
					maxTicker = Queue.Count * 3;
					for (int i = 0; i < Queue.Count; i++)
					{
						int speed = (int)Math.Round(i + 1.0 * (Queue.Count * info.SplitFractal / 100.0));
						speed = Math.Max(speed, 1);
						if (currentTicker % speed == 0)
							Queue[i].Tick(playerResources);
					}

					if (++currentTicker > maxTicker)
						currentTicker = 1;
				}
				else
				{
					Queue[0].Tick(playerResources);
				}
			}

			// Auto finish done items, as they are already in the world in our case!
			Queue.FindAll(i => i.Done).ForEach(i => Queue.Remove(i));
		}

		public virtual IEnumerable<ProductionItem> AllActuallyQueued()
		{
			return Queue;
		}
	}
}