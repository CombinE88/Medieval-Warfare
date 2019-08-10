using System;
using OpenRA.GameRules;

namespace OpenRA.Mods.MW.Projectiles
{
    public class LaserArcInfo : LaserInfo
    {
        public override IProjectile Create(ProjectileArgs args)
        {
            return new LaserArc(args, this);
        }
    }

    public class LaserArc : Laser
    {
        public LaserArc(ProjectileArgs args, LaserInfo info) : base(args, info)
        {
        }

        protected override void CalculateOffsets(ProjectileArgs args, int numSegments, WVec direction)
        {
            base.CalculateOffsets(args, numSegments, direction);

            var length = (args.Source - args.PassiveTarget).Length;
            var arraylength = offsets.Length;

            for (var i = 1; i < offsets.Length - 1; i++)
            {
                var x = i - (arraylength - 2) / 2;
                var squared = x * x;
                var fx = (1.0 / arraylength * 2) * -squared + arraylength / 2.0;
                var height = length * fx / (arraylength - 4) / 2;
                offsets[i] = new WPos(offsets[i].X, offsets[i].Y, offsets[i].Z + (int) Math.Round(height));
            }
        }
    }
}