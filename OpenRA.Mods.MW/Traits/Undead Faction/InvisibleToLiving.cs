﻿using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("Is Unit a Peasant (adds a count of 1 to the PlayerCivilisation).")]
    public class InvisibleToLivingInfo : ITraitInfo
    {
        public readonly HashSet<string> Factions = new HashSet<string>();

        public object Create(ActorInitializer init) { return new InvisibleToLiving(init.Self, this); }
    }

    public class InvisibleToLiving : IRenderModifier
    {
        private InvisibleToLivingInfo info;

        public InvisibleToLiving(Actor self, InvisibleToLivingInfo info)
        {
            this.info = info;
        }

        IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds)
        {
            return bounds;
        }

        IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
        {
            var player = self.World.RenderPlayer;
            var devMode = player.PlayerActor.TraitOrDefault<DeveloperMode>();

            if (player != null && (info.Factions.Contains(player.Faction.Name) || player.NonCombatant || player.IsAlliedWith(self.Owner) || devMode.DisableShroud))
                return r;

            return SpriteRenderable.None;
        }
    }
}
