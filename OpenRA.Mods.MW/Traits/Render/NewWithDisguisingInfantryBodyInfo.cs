﻿#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.Render
{
    class NewWithDisguisingInfantryBodyInfo : WithInfantryBodyDisguisedUpdateInfo, Requires<NewDisguiseInfo>
    {
        public override object Create(ActorInitializer init) { return new NewWithDisguisingInfantryBody(init, this); }
    }

    class NewWithDisguisingInfantryBody : WithInfantryBodyDisguisedUpdate, ITick
    {
        readonly NewWithDisguisingInfantryBodyInfo info;
        readonly NewDisguise disguise;
        readonly RenderSprites rs;
        string intendedSprite;
        public NewWithDisguisingInfantryBody(ActorInitializer init, NewWithDisguisingInfantryBodyInfo info)
            : base(init, info)
        {
            this.info = info;
            rs = init.Self.Trait<RenderSprites>();
            disguise = init.Self.Trait<NewDisguise>();
            intendedSprite = disguise.AsSprite;
        }

        void ITick.Tick(Actor self)
        {
            if (disguise.AsSprite != intendedSprite)
            {
                intendedSprite = disguise.AsSprite;
                var sequence = DefaultAnimation.GetRandomExistingSequence(info.StandSequences, Game.CosmeticRandom);
                if (sequence != null)
                    DefaultAnimation.ChangeImage(intendedSprite ?? rs.GetImage(self), sequence);

                var tar = disguise.Target;
                rs.Remove(Animwo);
                Animwo = new AnimationWithOffset(DefaultAnimation, null, () => IsTraitDisabled);
                rs.Add(Animwo, tar.Info.TraitInfo<RenderSpritesInfo>().Palette, tar.Info.TraitInfo<RenderSpritesInfo>().Palette == null);

                rs.UpdatePalette();
            }

            TickInner(self);
        }
    }
}
