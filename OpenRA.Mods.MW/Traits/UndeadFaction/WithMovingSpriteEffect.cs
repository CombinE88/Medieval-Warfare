using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.UndeadFaction
{
    public class WithMovingSpriteEffectInfo : ITraitInfo, Requires<RenderSpritesInfo>
    {
        public readonly string Image;

        public readonly string[] Sequences;

        public readonly int MaximumObjects = 1;

        public readonly int MovementSpeed = 2;

        [PaletteReference] public readonly string Palette;

        public readonly WDist MaximalDistance = WDist.Zero;

        public object Create(ActorInitializer init)
        {
            return new WithMovingSpriteEffect(init, this);
        }
    }

    public class WithMovingSpriteEffect : ITick
    {
        private RenderSprites renderSprites;

        private Dictionary<AnimationWithOffset, SpritePositioner> animations =
            new Dictionary<AnimationWithOffset, SpritePositioner>();

        private WithMovingSpriteEffectInfo info;

        public WithMovingSpriteEffect(ActorInitializer init, WithMovingSpriteEffectInfo info)
        {
            renderSprites = init.Self.Trait<RenderSprites>();
            this.info = info;

            for (var i = 0; i < info.MaximumObjects; i++)
            {
                var ani = new Animation(init.World, info.Image);

                var pos = new WVec(
                    init.World.SharedRandom.Next(-info.MaximalDistance.Length, +info.MaximalDistance.Length),
                    init.World.SharedRandom.Next(-info.MaximalDistance.Length, +info.MaximalDistance.Length),
                    0);

                var targetPos = new WVec(
                    init.World.SharedRandom.Next(-info.MaximalDistance.Length, +info.MaximalDistance.Length),
                    init.World.SharedRandom.Next(-info.MaximalDistance.Length, +info.MaximalDistance.Length),
                    0);

                var positioner = new SpritePositioner(targetPos, pos);

                var aniOff = new AnimationWithOffset(ani, () => { return positioner.LocationVector; },
                    () => { return false; });
                
                ani.PlayRepeating(info.Sequences[init.World.SharedRandom.Next(0, info.Sequences.Length - 1)]);


                animations.Add(aniOff, positioner);

                renderSprites.Add(aniOff, info.Palette);
            }
        }

        void ITick.Tick(Actor self)
        {
            foreach (var animation in animations)
            {
                if (animation.Value.LocationVector.X < animation.Value.TargetVector.X)
                    animation.Value.MoveX(Math.Min(
                        info.MovementSpeed,
                        animation.Value.TargetVector.X - animation.Value.LocationVector.X));
                if (animation.Value.LocationVector.X > animation.Value.TargetVector.X)
                    animation.Value.MoveX(Math.Min(
                        -info.MovementSpeed,
                        animation.Value.LocationVector.X - animation.Value.TargetVector.X));
                if (animation.Value.LocationVector.Y < animation.Value.TargetVector.Y)
                    animation.Value.MoveY(Math.Min(
                        info.MovementSpeed,
                        animation.Value.TargetVector.Y - animation.Value.LocationVector.Y));
                if (animation.Value.LocationVector.Y > animation.Value.TargetVector.Y)
                    animation.Value.MoveY(Math.Min(
                        -info.MovementSpeed,
                        animation.Value.LocationVector.Y - animation.Value.TargetVector.Y));

                if (animation.Value.TargetVector.Equals(animation.Value.LocationVector))
                    animation.Value.TargetVector = new WVec(
                        self.World.SharedRandom.Next(-info.MaximalDistance.Length, +info.MaximalDistance.Length),
                        self.World.SharedRandom.Next(-info.MaximalDistance.Length, +info.MaximalDistance.Length),
                        0);
            }
        }

        private class SpritePositioner
        {
            public WVec TargetVector;
            public WVec LocationVector;

            public SpritePositioner(WVec targetVector, WVec locationVector)
            {
                TargetVector = targetVector;
                LocationVector = locationVector;
            }

            public void MoveX(int value)
            {
                LocationVector = new WVec(LocationVector.X + value, LocationVector.Y, LocationVector.Z);
            }

            public void MoveY(int value)
            {
                LocationVector = new WVec(LocationVector.X, LocationVector.Y + value, LocationVector.Z);
            }
        }
    }
}