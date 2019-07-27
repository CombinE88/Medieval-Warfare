using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.WorldTraits
{
    public class ArealWeatherSystemInfo : ITraitInfo
    {
        public readonly int IntensityDelay = 10;

        public readonly int IntensityStep = 1;

        public readonly int MaximalIntensity = 100;

        public readonly int[] FallDuration = {12, 13, 14, 15, 16, 17, 18, 19, 20};

        public readonly WDist RainHeight = WDist.FromCells(5);

        public readonly int2 Size = new int2(1, 4);

        public readonly int[] Wind = {850, 700, 550, 300, 150, 50};

        public readonly Color[] ParticleColors =
        {
            Color.FromArgb(90, 120, 236),
            Color.FromArgb(100, 90, 228),
            Color.FromArgb(60, 60, 208),
            Color.FromArgb(80, 90, 188),
            Color.FromArgb(15, 90, 188),
            Color.FromArgb(60, 80, 188)
        };

        public object Create(ActorInitializer init)
        {
            return new ArealWeatherSystem(init, this);
        }
    }

    public class ArealWeatherSystem : IRenderAboveWorld, ITick
    {
        private int currentDelay;

        private Dictionary<KeyValuePair<CPos, IntensityModifier>, Dictionary<Actor, int>> index =
            new Dictionary<KeyValuePair<CPos, IntensityModifier>, Dictionary<Actor, int>>();

        private ArealWeatherSystemInfo info;
        private List<RainParticle> particles = new List<RainParticle>();

        private MapGrid mapGrid = Game.ModData.Manifest.Get<MapGrid>();

        public ArealWeatherSystem(ActorInitializer init, ArealWeatherSystemInfo info)
        {
            this.info = info;
        }

        public void Register(Actor actor, int[] intensities)
        {
            var cells = actor.World.Map
                .FindTilesInCircle(actor.World.Map.CellContaining(actor.CenterPosition), intensities.Length).ToList();

            foreach (var cell in cells)
            {
                var dist = (actor.World.Map.CellContaining(actor.CenterPosition) - cell).Length;
                var intensity = intensities[Math.Min(dist, intensities.Length - 1)]; // gedizzt

                if (!index.Any(k => k.Key.Key == cell))
                    index.Add(
                        new KeyValuePair<CPos, IntensityModifier>(cell, new IntensityModifier(info.MaximalIntensity)),
                        new Dictionary<Actor, int>());

                index.First(e => e.Key.Key == cell).Value.Add(actor, intensity);
            }
        }

        public void Remove(Actor actor)
        {
            foreach (var values in index.Values)
                values.Remove(actor);
        }

        void IRenderAboveWorld.RenderAboveWorld(Actor self, WorldRenderer wr)
        {
            foreach (var particle in particles)
            {
                var height = (self.World.WorldTick - particle.SpawnTick) * info.RainHeight.Length / particle.Duration;

                var x = particle.CenterCell.X + particle.X;
                var y = particle.CenterCell.Y + particle.Y + height - info.RainHeight.Length;

                // Wind adding

                x += (int) Math.Round((float) particle.WindAffection *
                                      (self.World.WorldTick - particle.SpawnTick) / particle.Duration);

                var pos = wr.ScreenPosition(new WPos(x, y, 0));

                if (new Rectangle(
                        wr.Viewport.TopLeft.X,
                        wr.Viewport.TopLeft.Y,
                        Game.Renderer.Resolution.Width,
                        Game.Renderer.Resolution.Height)
                    .Contains(new Point((int) pos.X, (int) pos.Y)))
                {
                    Game.Renderer.WorldRgbaColorRenderer.FillRect(new float3(pos.X, pos.Y, 0),
                        new float3(pos.X + info.Size.X, pos.Y + info.Size.Y, 0),
                        particle.Color);
                }
            }
        }

        void ITick.Tick(Actor self)
        {
            var emptyIndices = index.Where(entry => entry.Value.Count == 0 && entry.Key.Value.CurrentIntensity <= 0)
                .Select(entry => entry.Key).ToArray();
            foreach (var emptyIndex in emptyIndices)
                index.Remove(emptyIndex);

            foreach (var cell in index)
            {
                var intens = Math.Max(cell.Key.Value.CurrentIntensity, 1);
                if (self.World.LocalRandom.Next(0, 4000 / intens) != 0)
                    continue;

                var x = self.World.LocalRandom.Next(-512, 512);
                var y = self.World.LocalRandom.Next(-512, 512);

                particles.Add(new RainParticle(
                    x,
                    y,
                    self.World.WorldTick,
                    self.World.Map.CenterOfCell(cell.Key.Key),
                    info.FallDuration[self.World.LocalRandom.Next(0, info.FallDuration.Length - 1)],
                    info.Wind[self.World.LocalRandom.Next(0, info.Wind.Length - 1)],
                    info.ParticleColors[self.World.LocalRandom.Next(0, info.ParticleColors.Length - 1)]
                ));
            }

            var remover = particles.Where(p => p.SpawnTick + p.Duration < self.World.WorldTick).ToArray();
            foreach (var particle in remover)
                particles.Remove(particle);

            if (self.World.WorldTick % info.IntensityDelay == 0)
                foreach (var indice in index)
                {
                    if (indice.Value.Sum(e => e.Value) > indice.Key.Value.CurrentIntensity)
                        indice.Key.Value.IncreaseIntensity(info.IntensityStep);

                    else if (indice.Value.Sum(e => e.Value) < indice.Key.Value.CurrentIntensity)
                        indice.Key.Value.DecreaseIntensity(info.IntensityStep);
                }
        }

        private class RainParticle
        {
            public readonly int X;
            public readonly int Y;
            public readonly int SpawnTick;
            public readonly WPos CenterCell;
            public readonly int Duration;
            public readonly int WindAffection;
            public readonly Color Color;

            public RainParticle(int x, int y, int spawnTick, WPos centerCell, int duration, int windAffection,
                Color color)
            {
                X = x;
                Y = y;
                SpawnTick = spawnTick;
                CenterCell = centerCell;
                Duration = duration;
                WindAffection = windAffection;
                Color = color;
            }
        }

        private class IntensityModifier
        {
            public int CurrentIntensity;
            public int MaxIntensity;

            public IntensityModifier(int maxIntensity)
            {
                MaxIntensity = maxIntensity;
            }

            public void IncreaseIntensity(int step)
            {
                CurrentIntensity += Math.Min(step, MaxIntensity - CurrentIntensity);
            }

            public void DecreaseIntensity(int step)
            {
                CurrentIntensity -= Math.Min(step, CurrentIntensity);
            }
        }
    }
}