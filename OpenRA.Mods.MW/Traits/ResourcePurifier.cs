using System;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("A actor has to enter the building before the unit spawns.")]
    public class ResourcePurifierInfo : ITraitInfo
    {
        public readonly int Percentage = 10;
        public readonly bool ShowTicks = true;
        public readonly int TickLifetime = 30;
        public readonly int TickVelocity = 2;

        public object Create(ActorInitializer init) { return new ResourcePurifier(init.Self, this); }
    }

    class ResourcePurifier : ITick
    {
        private readonly ResourcePurifierInfo info;
        private PlayerResources playerResources;
        private int resources;
        private int resourcesTickBefore;

        public ResourcePurifier(Actor self, ResourcePurifierInfo info)
        {
            this.info = info;
            playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
            resources = playerResources.Resources;
            resourcesTickBefore = playerResources.Resources;
        }

        public void GiveCash(Actor self, int cashGrant)
        {
            var temp = (int)Math.Ceiling((cashGrant * info.Percentage) / 100.0);
            playerResources.GiveResources(temp);

            if (info.ShowTicks && temp > 0)
            {
                if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
                    self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color.RGB, FloatingText.FormatCashTick(temp), 30)));
            }
        }

        void ITick.Tick(Actor self)
        {
            resources = playerResources.Resources;
            var cashGrant = resources - resourcesTickBefore;

            if (cashGrant > 0)
            {
                GiveCash(self, cashGrant);
            }

            resourcesTickBefore = playerResources.Resources;
        }
    }
}
