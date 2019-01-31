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
	public class SelfConstructingProductionQueueInfo : ProductionQueueInfo
	{
		public override object Create(ActorInitializer init) { return new SelfConstructingProductionQueue(init, init.Self.Owner.PlayerActor, this); }
	}

	public class SelfConstructingProductionQueue : ProductionQueue, INotifyOwnerChanged
	{
		private bool expectFakeProductionItemRequest;
		private new PlayerResources playerResources;

		public SelfConstructingProductionQueue(ActorInitializer init, Actor playerActor,
			SelfConstructingProductionQueueInfo info) : base(init, playerActor, info)
		{
			playerResources = playerActor.TraitOrDefault<PlayerResources>();
		}
		
		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			ClearQueue();

			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
			developerMode = newOwner.PlayerActor.Trait<DeveloperMode>();

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
				Queue[0].Tick(playerResources);

			// Auto finish done items, as they are already in the world in our case!
			Queue.FindAll(i => i.Done).ForEach(i => Queue.Remove(i));
		}
	}

	public class SelfConstructingProductionItem : ProductionItem
	{
		public readonly Actor Actor;

		public SelfConstructingProductionItem(ProductionQueue queue, Actor actor, int cost, PowerManager pm, Action onComplete) : base(queue, actor.Info.Name, cost, pm, onComplete)
		{
			Actor = actor;
		}
	}
}