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

using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.MW.Traits.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.UndeadFaction
{
    public class UndeadSummonSellerInfo : ITraitInfo, Requires<WithSpriteBodyInfo>
    {
        [Desc("Apply to sprite bodies with these names.")]
        public readonly string[] BodyNames = {"body"};

        public object Create(ActorInitializer init)
        {
            return new UndeadSummonSeller(init, this);
        }
    }

    public class UndeadSummonSeller : ITick, IResolveOrder, INotifyCreated
    {
        private readonly WithSpriteBody[] wsbs;

        private UndeadBuilder summonTrait;
        private int currentSummonCount;

        public UndeadSummonSeller(ActorInitializer init, UndeadSummonSellerInfo info)
        {
            wsbs = init.Self.TraitsImplementing<WithSpriteBody>().Where(w => info.BodyNames.Contains(w.Info.Name))
                .ToArray();
        }

        void INotifyCreated.Created(Actor self)
        {
            summonTrait = self.TraitOrDefault<UndeadBuilder>();
        }

        void ITick.Tick(Actor self)
        {
            Game.AddChatLine(Color.Wheat, "test", "test");

            if (self.IsDead || !self.IsInWorld)
                return;

            var newFrameCounter = summonTrait.Info.Animations.Length * summonTrait.HasSummoningCount /
                                  summonTrait.Info.SummoningTime;

            if (newFrameCounter == summonTrait.Info.Animations.Length)
                return;

            if (summonTrait.Info.Animations.Any() && newFrameCounter != summonTrait.CurrentAnimationFrame)
            {
                wsbs.First().PlayCustomAnimationBackwards(self, summonTrait.Info.Animations[newFrameCounter] + "-make",
                    () =>
                    {
                        if (newFrameCounter > 0)
                            wsbs.First().PlayCustomAnimationRepeating(self,
                                summonTrait.Info.Animations[newFrameCounter - 1]);
                        else
                        {
                            foreach (var notifySold in self.TraitsImplementing<INotifySold>())
                                notifySold.Sold(self);

                            var pr = self.Owner.PlayerActor.Trait<PlayerResources>();
                            var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();

                            if (valued != null)
                                pr.GiveCash(summonTrait.Info.Cost * currentSummonCount /
                                            summonTrait.Info.SummoningTime / 2);

                            self.Dispose();
                        }
                    });
            }

            summonTrait.CurrentAnimationFrame = newFrameCounter;
        }

        void IResolveOrder.ResolveOrder(Actor self, Order order)
        {
            if (order.OrderString != SellOrderGenerator.Id)
                return;

            if (summonTrait == null || summonTrait.Cancled)
                return;

            summonTrait.Cancle();
            currentSummonCount = summonTrait.HasSummoningCount;
        }
    }
}