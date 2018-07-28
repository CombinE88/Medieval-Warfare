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
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Warheads
{
    public class CashTickWarhead : Warhead
    {
        [FieldLoader.Require]

        [Desc("Duration of the condition (in ticks). Set to 0 for a permanent condition.")]
        public readonly int Ammount = 0;
        public readonly bool ShowTicks = true;
        public readonly int TickLifetime = 30;
        public readonly int TickVelocity = 2;

        public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
        {
            if (!firedBy.IsDead && firedBy.IsInWorld)
            {
                var playerResources = firedBy.Owner.PlayerActor.Trait<PlayerResources>();

                playerResources.GiveResources(Ammount);

                if (ShowTicks && Ammount > 0 && firedBy.IsInWorld && !firedBy.IsDead)
                {
                    if (firedBy.Owner.IsAlliedWith(firedBy.World.RenderPlayer))
                        firedBy.World.AddFrameEndTask(w => w.Add(new FloatingText(firedBy.CenterPosition, firedBy.Owner.Color.RGB, FloatingText.FormatCashTick(Ammount), 30)));
                }
            }
        }
    }
}
