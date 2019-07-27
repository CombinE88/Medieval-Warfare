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
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.MW.Traits.Orders;

namespace OpenRA.Mods.MW.Traits.UndeadFaction
{
    public class UndeadBuilderSellerInfo : ITraitInfo, Requires<WithSpriteBodyInfo>, Requires<UndeadBuilderInfo>
    {
        [Desc("Apply to sprite bodies with these names.")]
        public readonly string[] BodyNames = {"body"};

        public object Create(ActorInitializer init)
        {
            return new UndeadBuilderSeller(init, this);
        }
    }

    public class UndeadBuilderSeller : ITick, IResolveOrder
    {
        private readonly WithSpriteBody[] wsbs;

        private UndeadBuilder UndeadBuilder;
        private int currentSummonCount;
        private int currentSellFrame;
        private int nextAnimationFrame;


        public UndeadBuilderSeller(ActorInitializer init, UndeadBuilderSellerInfo info)
        {
            wsbs = init.Self.TraitsImplementing<WithSpriteBody>().Where(w => info.BodyNames.Contains(w.Info.Name))
                .ToArray();
            UndeadBuilder = init.Self.TraitOrDefault<UndeadBuilder>();
        }

        void ITick.Tick(Actor self)
        {
            if (self.IsDead || !self.IsInWorld || !UndeadBuilder.Cancled)
                return;

            if (nextAnimationFrame < 0)
            {
                self.World.AddFrameEndTask(w =>
                {
                    foreach (var notifySold in self.TraitsImplementing<INotifySold>())
                        notifySold.Sold(self);

                    var pr = self.Owner.PlayerActor.Trait<PlayerResources>();
                    var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();

                    if (valued != null)
                        pr.GiveCash(UndeadBuilder.Info.Cost * currentSummonCount /
                                    UndeadBuilder.Info.SummoningTime / 2);

                    self.Dispose();
                });
            }

            if (UndeadBuilder.Info.Animations.Any() && currentSellFrame != nextAnimationFrame && nextAnimationFrame >= 0)
            {
                currentSellFrame = nextAnimationFrame;
                wsbs.First().PlayCustomAnimationBackwards(self,
                    UndeadBuilder.Info.Animations[nextAnimationFrame] + "-make",
                    () => { nextAnimationFrame--; });
            }
        }


        void IResolveOrder.ResolveOrder(Actor self, Order order)
        {
            if (order.OrderString != SellOrderGenerator.Id)
                return;

            if (UndeadBuilder == null || UndeadBuilder.Cancled)
                return;

            UndeadBuilder.Cancle();
            currentSummonCount = UndeadBuilder.HasSummoningCount;
            currentSellFrame = UndeadBuilder.CurrentAnimationFrame - 1;
            nextAnimationFrame = UndeadBuilder.CurrentAnimationFrame;
        }
    }
}