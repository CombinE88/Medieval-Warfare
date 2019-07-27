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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.MW.Traits.BuildingTraits;
using OpenRA.Mods.MW.Traits.UndeadFaction;

namespace OpenRA.Mods.MW.Traits.Orders
{
    public class SellOrderGenerator : IOrderGenerator
    {
        public const string Id = "Sell";

        private Actor[] sellableActors;

        public IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
        {
            if (mi.Button != Game.Settings.Game.MouseButtonPreference.Action)
                world.CancelInputMode();
            else
            {
                var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(a => sellableActors.Any(e => e.Equals(a)));
                if (actor != null)
                    yield return new Order(Id, actor, false);
            }
        }

        public virtual void Tick(World world)
        {
            sellableActors = world.Actors.Where(a =>
                {
                    if (a.Owner != world.LocalPlayer)
                        return false;

                    if (a.TraitOrDefault<DeconstructSellable>() != null)
                        return true;

                    if (a.TraitOrDefault<UndeadBuilderSeller>() != null)
                        return true;
                    
                    if (a.TraitOrDefault<Sellable>() != null)
                        return true;

                    return false;
                })
                .ToArray();

            if (!sellableActors.Any())
                world.CancelInputMode();
        }

        public IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
        {
            yield break;
        }

        public IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
        {
            yield break;
        }

        public string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
        {
            if (sellableActors == null)
                return null;

            var actor = world.ActorMap.GetActorsAt(cell).FirstOrDefault(a => sellableActors.Any(e => e.Equals(a)));

            if (actor != null)
                return "sell";

            return null;
        }
    }
}