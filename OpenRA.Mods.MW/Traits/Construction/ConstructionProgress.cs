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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.Render
{
    [Desc("Displays a text overlay relative to the selection box.")]
    public class ConstructionProgressInfo : ConditionalTraitInfo
    {
        [Desc("Image used for this Prioritysettings. Defaults to the actor's type.")] [PaletteReference]
        public readonly string Palette = "ra";

        public readonly int FullCapacity = 25;

        public readonly string Font = "BigBold";

        [Desc("Display in this color when not using the player color.")]
        public readonly Color Color = Color.White;

        [Desc("Use the player color of the current owner.")]
        public readonly bool UsePlayerColor = false;

        [Desc("The Z offset to apply when rendering this decoration.")]
        public readonly int ZOffset = 12048;

        public readonly int YOffset = 0;

        public readonly int XOffset = 0;

        [Desc("Should this be visible only when selected?")]
        public readonly bool RequiresSelection = false;

        public readonly BooleanExpression ConstructionCondition = null;

        public readonly string PriorityCondition = null;

        public override object Create(ActorInitializer init)
        {
            return new ConstructionProgress(init.Self, this);
        }

        public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
        {
            if (!Game.ModData.Manifest.Fonts.ContainsKey(Font))
                throw new YamlException("Font '{0}' is not listed in the mod.yaml's Fonts section".F(Font));

            base.RulesetLoaded(rules, ai);
        }
    }

    public class ConstructionProgress : ConditionalTrait<ConstructionProgressInfo>, IRender, IRenderAboveShroudWhenSelected, ITick
    {
        readonly SpriteFont font;
        readonly IDecorationBounds[] decorationBounds;
        Color color;
        private int maxdureation;
        private int timer = 0;
        private int priority = 0;

        protected readonly Animation LeftAnim;
        protected readonly Animation RightAnim;

        public ConstructionProgress(Actor self, ConstructionProgressInfo info)
            : base(info)
        {
            font = Game.Renderer.Fonts[info.Font];
            color = Info.UsePlayerColor ? self.Owner.Color.RGB : Info.Color;
            decorationBounds = self.TraitsImplementing<IDecorationBounds>().ToArray();
        }

        protected override void Created(Actor self)
        {
            if (self.Info.HasTraitInfo<AddConditionEveryInfo>())
            {
                var conadds = 0;
                var traits = self.TraitsImplementing<AddConditionEvery>().Where(c => c.Info.AddCondition == Info.ConstructionCondition.Expression);
                foreach (var trait in traits)
                {
                    conadds += trait.Info.Delay;
                }

                if (traits.Count() != 0 && conadds != 0)
                    conadds = (int)Math.Round((double)conadds / traits.Count());

                maxdureation = Info.FullCapacity * conadds;
            }
        }

        public virtual bool ShouldRender(Actor self)
        {
            return true;
        }

        IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
        {
            if (IsTraitDisabled)
                return SpriteRenderable.None;

            if (!Info.RequiresSelection)
                return RenderInner(self, wr);

            return SpriteRenderable.None;
        }

        IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
        {
            if (IsTraitDisabled)
                return SpriteRenderable.None;

            if (!Info.RequiresSelection)
                return RenderInner(self, wr);

            return SpriteRenderable.None;
        }

        bool IRenderAboveShroudWhenSelected.SpatiallyPartitionable
        {
            get { return false; }
        }

        IEnumerable<IRenderable> RenderInner(Actor self, WorldRenderer wr)
        {
            var bounds = decorationBounds.Select(b => b.DecorationBounds(self, wr)).FirstOrDefault(b => !b.IsEmpty);
            var spaceBuffer = (int)(10 / wr.Viewport.Zoom);
            var effectPos = wr.ProjectedPosition(new int2((bounds.Left + bounds.Right) / 2 + Info.XOffset, (bounds.Top + bounds.Bottom) / 2 + Info.YOffset - spaceBuffer));

            return new IRenderable[]
            {
                new TextRenderable(font, effectPos, Info.ZOffset, color, priority + "%")
            };
        }

        IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
        {
            yield break;
        }

        void ITick.Tick(Actor self)
        {
            if (IsTraitDisabled)
                return;

            priority = timer++ >= maxdureation ? 100 : (int)Math.Round((double)100 / maxdureation * (timer != 0 ? timer : 1));
        }
    }
}