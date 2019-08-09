using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
        private static Sprite sprite;

        public WeatherArealProvider(ActorInitializer init, WeatherArealProviderInfo info)
        {
            init.World.WorldActor.Trait<ArealWeatherSystem>().Register(init.Self, info.Intensity);
            this.info = info;
        }

        private static Sprite GetSprite()
        {
            if (sprite == null)
            {
                var faktor = 2;
                var durchmesser = 256 / faktor;
                var radius = durchmesser / 2;
                var bitmap = new Bitmap(durchmesser, durchmesser);

                var mid = new int2(radius, radius);

                for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; x++)
                {
                    // 0 - 50
                    var pixel = new int2(x, y);
                    var alpha = Math.Min(Math.Max(0, (pixel - mid).Length * 255 / radius), 255);

                    bitmap.SetPixel(x, y, Color.FromArgb((255 - alpha) / faktor, 0, 0, 0));
                }

                Sheet sheet;

                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    stream.Position = 0;
                    sheet = new Sheet(SheetType.BGRA, stream);
                }

                sprite = new Sprite(sheet, new Rectangle(0, 0, durchmesser, durchmesser), TextureChannel.RGBA);
            }

            return sprite;
        }

        void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
        {
            self.World.WorldActor.Trait<ArealWeatherSystem>().Remove(self);
        }

        public void RenderAboveWorld(Actor self, WorldRenderer wr)
        {
            var pos = wr.ScreenPxPosition(self.CenterPosition);

            Game.Renderer.WorldRgbaSpriteRenderer.DrawSprite(GetSprite(),
                pos - new int2(info.Intensity.Length * 24 / 2, info.Intensity.Length * 24 / 2),
                new float3(
                    info.Intensity.Length * 24,
                    info.Intensity.Length * 24,
                    sprite.Size.Z));
        }
    }
}