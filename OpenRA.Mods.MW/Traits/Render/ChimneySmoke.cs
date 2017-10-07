using System.Collections.Generic;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.MW.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.Render
{

    [Desc("Renders a sprite effect when leaving a cell.")]
    public class ChimneySmokeInfo : ConditionalTraitInfo
    {
        public readonly string Image = null;

        [SequenceReference("Image")]
        public readonly string[] Sequences = { "idle" };

        [PaletteReference] public readonly string Palette = "effect";

        [Desc("Delay between Clouds.")]
        public readonly int Delay = 0;

        [Desc("Random allowed maximum Bonus Delay between Clouds.")]
        public readonly int BonusDelay = 0;

        [Desc("Length of Cloudspawn.")]
        public readonly int SpawnTime = 10;

        [Desc("Random allowed maximum Bonus Length of Cloudspawn.")]
        public readonly int BonusSpawnTime = 0;

        [Desc("Delay between single Cloud.")]
        public readonly int LoopDelay = 1;

        [Desc("Maximum Amplitude wich the Clouds wanders left and right.")]
        public readonly int Amplitude = 2;

        [Desc("Speed of the Amplitude wich the Clouds wanders left and right.")]
        public readonly int SpeedAmplitude = 10;

        [Desc("Speed how fast the Clouds rise.")]
        public readonly int Velocity = 0;

        [Desc("Delay before first trail.",
            "Use negative values for falling back to the *Interval values.")]
        public readonly int StartDelay = 0;

        [Desc("Trail spawn positions relative to actor position. (forward, right, up) triples")]
        public readonly WVec[] Offsets = { WVec.Zero };

        public override object Create(ActorInitializer init) { return new ChimneySmoke(init.Self, this); }
    }

    public class ChimneySmoke : ConditionalTrait<ChimneySmokeInfo>, ITick
    {
        BodyOrientation body;
        IFacing facing;
        int cachedFacing;
        int cachedInterval;
        int spawnint;
        int cachedspawnint;

        public ChimneySmoke(Actor self, ChimneySmokeInfo info)
            : base(info)
        {
            cachedInterval = Info.StartDelay;
        }

        WPos cachedPosition;
        protected override void Created(Actor self)
        {
            body = self.Trait<BodyOrientation>();
            facing = self.TraitOrDefault<IFacing>();
            cachedFacing = facing != null ? facing.Facing : 0;
            cachedPosition = self.CenterPosition;
            cachedspawnint = Info.SpawnTime + self.World.SharedRandom.Next(Info.BonusSpawnTime);
            base.Created(self);
        }

        int ticks;
        int offset;

        public void Tick(Actor self)
        {
            if (IsTraitDisabled)
                return;

            if (++ticks >= cachedInterval)
            {
                if (++offset >= Info.Offsets.Length)
                    offset = 0;

                var offsetRotation = Info.Offsets[offset].Rotate(body.QuantizeOrientation(self, self.Orientation));
                var spawnPosition = self.CenterPosition;

                var pos = spawnPosition + body.LocalToWorld(offsetRotation);

                var spawnFacing = facing != null ? facing.Facing : 0;

                if (!string.IsNullOrEmpty(Info.Image) && spawnint % Info.LoopDelay == 0)
                    self.World.AddFrameEndTask(w => w.Add(new ADVSpriteEffect(pos, self.World, Info.Image,
                        Info.Sequences.Random(Game.CosmeticRandom), Info.Palette, false, false, spawnFacing,-Info.Velocity, Info.Amplitude, Info.SpeedAmplitude)));

                cachedPosition = self.CenterPosition;
                cachedFacing = facing != null ? facing.Facing : 0;
                

                if (--spawnint <= 0)
                {
                    cachedspawnint = Info.SpawnTime + self.World.SharedRandom.Next(Info.BonusSpawnTime);
                    cachedInterval = Info.Delay + self.World.SharedRandom.Next(Info.BonusDelay);
                    spawnint = cachedspawnint;
                    ticks = 0;
                }

            }
        }

        protected override void TraitEnabled(Actor self)
        {
            cachedPosition = self.CenterPosition;
        }
    }
}
