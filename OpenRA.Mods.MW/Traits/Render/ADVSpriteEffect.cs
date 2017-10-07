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
using System.Diagnostics;
using OpenRA.Effects;
using OpenRA.Graphics;

namespace OpenRA.Mods.MW.Effects
{
    public class ADVSpriteEffect : IEffect
    {
        readonly World world;
        readonly string palette;
        readonly Animation anim;
        WPos pos;
        readonly bool visibleThroughFog;
        readonly bool scaleSizeWithZoom;
        int Threexix;
        WVec offset;
        readonly int Amplitude;
        readonly int Velocity;
        int hight;
        int AmpliSpeed = 0;
        readonly int SpeedAmplitude;

        public ADVSpriteEffect(WPos pos, World world, string image, string sequence, string palette, bool visibleThroughFog = false, bool scaleSizeWithZoom = false, int facing = 0, int Velocity = 0, int Amplitude = 0, int SpeedAmplitude = 0)
        {
            this.world = world;
            Threexix = world.SharedRandom.Next(100);
            this.pos = pos + new WVec(-(int)(Math.Cos(Threexix) * Amplitude)/2, 0,0);
            this.palette = palette;
            this.scaleSizeWithZoom = scaleSizeWithZoom;
            this.visibleThroughFog = visibleThroughFog;
            anim = new Animation(world, image, () => facing);
            anim.PlayThen(sequence, () => world.AddFrameEndTask(w => w.Remove(this)));
            
            this.Amplitude = Amplitude;
            this.Velocity = Velocity;
            this.SpeedAmplitude = SpeedAmplitude;
        }

        public void Tick(World world)
        {
            SetOffset();
            anim.Tick();
        }

        public IEnumerable<IRenderable> Render(WorldRenderer wr)
        {
            if (world.FogObscures(pos) && !visibleThroughFog)
                return SpriteRenderable.None;

            

            var zoom = scaleSizeWithZoom ? 1f / wr.Viewport.Zoom : 1f;
            return anim.Render(pos, offset, 0, wr.Palette(palette), zoom);
        }

        public void SetOffset()
        {
            AmpliSpeed++;

            if (AmpliSpeed % SpeedAmplitude == 0)
            {
                Threexix++;
                if (Threexix >= 100)
                {
                    Threexix = 0;
                }
            }

            

            hight += Velocity;

            double radian = Threexix * (Math.PI / 50);
            decimal modifier = (decimal) Math.Cos(radian);
            var var = (int)Math.Round(modifier * Amplitude);
            offset = new WVec(var, hight, 0);

            Log.Write("debug", "Threexix " + Threexix);
            Log.Write("debug", "radian " + radian);
            Log.Write("debug", "modifier " + modifier);
            Log.Write("debug", "Amplitude " + Amplitude);
            Log.Write("debug", "modifier*Amplitude " + var);
        }
    }
}
