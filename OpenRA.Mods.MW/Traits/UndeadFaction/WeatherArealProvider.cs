using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using OpenRA.Graphics;
using OpenRA.Mods.MW.Traits.WorldTraits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.UndeadFaction
{
    public class WeatherArealProviderInfo : ITraitInfo
    {
        public readonly int[] Intensity = {100, 0};

        public object Create(ActorInitializer init)
        {
            return new WeatherArealProvider(init, this);
        }
    }

    public class WeatherArealProvider : INotifyRemovedFromWorld, IRenderAboveWorld
    {
        private WeatherArealProviderInfo info;

        public WeatherArealProvider(ActorInitializer init, WeatherArealProviderInfo info)
        {
            init.World.WorldActor.Trait<ArealWeatherSystem>().Register(init.Self, info.Intensity);
            this.info = info;
        }

        void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
        {
            self.World.WorldActor.Trait<ArealWeatherSystem>().Remove(self);
        }

        public void RenderAboveWorld(Actor self, WorldRenderer wr)
        {
            for (int i = 0; i < info.Intensity.Length; i++)
            {
                var size = (i + 2) * 1024 / 2;
                var x1 = self.CenterPosition.X - size;
                var y1 = self.CenterPosition.Y - size;

                var x2 = self.CenterPosition.X + size;
                var y2 = self.CenterPosition.Y + size;

                var pos1 = wr.ScreenPosition(new WPos(x1, y1, 0));
                var pos2 = wr.ScreenPosition(new WPos(x2, y2, 0));

                Game.Renderer.WorldRgbaColorRenderer.FillEllipse(pos1, pos2,
                    Color.FromArgb(50 * info.Intensity[i] / 100, 0, 0, 0));
            }
        }
    }
}