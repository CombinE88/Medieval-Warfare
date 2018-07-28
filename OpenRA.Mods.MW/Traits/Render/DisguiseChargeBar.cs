using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.Render
{
    [Desc("Display the time remaining until the super weapon attached to the actor is ready.")]
    class DisguiseChargeBarInfo : ITraitInfo, Requires<NewDisguiseInfo>
    {
        public readonly Color Color = Color.Blue;

        public object Create(ActorInitializer init) { return new DisguiseChargeBar(init.Self, this); }
    }

    class DisguiseChargeBar : ISelectionBar, ITick
    {
        readonly Actor self;
        readonly DisguiseChargeBarInfo info;
        private NewDisguise ndg;
        private float value;

        public DisguiseChargeBar(Actor self, DisguiseChargeBarInfo info)
        {
            this.self = self;
            this.info = info;
            ndg = self.TraitsImplementing<NewDisguise>().FirstOrDefault();
        }

        void ITick.Tick(Actor self)
        {
            var should = ndg.ChargeTime;
            var has = ndg.Timer;
            value = !ndg.Cannotdsguise ? 1 - (float)has / should : 0;
        }

        float ISelectionBar.GetValue()
        {
            if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
                return 0;

            return value;
        }

        Color ISelectionBar.GetColor() { return info.Color; }
        bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
    }
}
