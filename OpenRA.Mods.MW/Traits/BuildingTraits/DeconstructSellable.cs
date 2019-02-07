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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.MW.Traits.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.BuildingTraits
{
	public class DeconstructSellableInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("How long selling will take, percentual to build time.")]
		public readonly int SellTimePercent = 10;

		[Desc("The percentual amount of money to refund.")]
		public readonly int RefundPercent = 50;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while the make animation is playing.")]
		public readonly string Condition = null;
		
		[Desc("Apply to sprite bodies with these names.")]
		public readonly string[] BodyNames = { "body" };

		public override object Create(ActorInitializer init) { return new DeconstructSellable(init, this); }
	}

	public class DeconstructSellable : ConditionalTrait<DeconstructSellableInfo>, ITick, IResolveOrder, INotifyCreated
	{
		private readonly DeconstructSellableInfo info;

		private readonly DeveloperMode developerMode;
		private readonly WithSpriteBody[] wsbs;

		private SelfConstructing[] selfConstructings;

		private ConditionManager conditionManager;
		private int token = ConditionManager.InvalidConditionToken;

		private int sellTimer;
		private int sellTimerTotal;
		private int refundAmount;

		public DeconstructSellable(ActorInitializer init, DeconstructSellableInfo info) : base(info)
		{
			this.info = info;
			developerMode = init.Self.Owner.PlayerActor.Trait<DeveloperMode>();
			wsbs = init.Self.TraitsImplementing<WithSpriteBody>().Where(w => info.BodyNames.Contains(w.Info.Name)).ToArray();
		}

		void INotifyCreated.Created(Actor self)
		{
			selfConstructings = self.TraitsImplementing<SelfConstructing>().ToArray();
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		void ITick.Tick(Actor self)
		{
			if (self.IsDead)
				return;

			if (token != ConditionManager.InvalidConditionToken)
			{
				sellTimer = developerMode.FastBuild ? 0 : sellTimer - 1;
				var wsb = wsbs.FirstEnabledTraitOrDefault();

				if (sellTimer <= 0)
				{
					foreach (var notifySold in self.TraitsImplementing<INotifySold>())
						notifySold.Sold(self);

					var pr = self.Owner.PlayerActor.Trait<PlayerResources>();
					var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();

					if (valued != null)
						pr.GiveCash(refundAmount * info.RefundPercent / 100);

					self.Dispose();
				}
				else if (wsb != null)
				{
					var selfConstructingInfo = selfConstructings.FirstOrDefault(c => c.IsActive());
					if (selfConstructingInfo != null)
						wsb.PlayCustomAnimationRepeating(self,
							selfConstructingInfo.Info.Sequence + Math.Min(sellTimer * selfConstructingInfo.Info.Steps / sellTimerTotal,
								selfConstructingInfo.Info.Steps - 1));
				}
			}
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != SellOrderGenerator.Id)
				return;

			if (conditionManager != null && !string.IsNullOrEmpty(info.Condition) && token == ConditionManager.InvalidConditionToken)
				token = conditionManager.GrantCondition(self, info.Condition);

			ProductionItem productionItem = null;

			if (selfConstructings.Any())
				foreach (var selfconst in selfConstructings)
				{
					productionItem = selfconst.TryAbort(self);
				}

			var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();

			if (productionItem != null)
				refundAmount = productionItem.TotalCost - productionItem.RemainingCost;
			else if (valued != null)
				refundAmount = valued.Cost;

			sellTimer = sellTimerTotal = self.Info.TraitInfoOrDefault<BuildableInfo>().BuildDuration * info.SellTimePercent / 100;

			if (developerMode.FastBuild)
				sellTimer = 0;
			else if (productionItem != null)
				sellTimer = (productionItem.TotalTime - productionItem.RemainingTime) * info.SellTimePercent / 100;

			foreach (var notifySold in self.TraitsImplementing<INotifySold>())
				notifySold.Selling(self);
		}
	}
}