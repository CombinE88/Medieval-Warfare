using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits.Render
{
    [Desc("Display the time remaining until the super weapon attached to the actor is ready.")]
    class UndeadGiantSpawnerChargeBarInfo : ITraitInfo, Requires<UndeadGiantSpawnerInfo>
    {
        public readonly Color Color = Color.Blue;

        public object Create(ActorInitializer init) { return new UndeadGiantSpawnerChargeBar(init.Self, this); }
    }

    class UndeadGiantSpawnerChargeBar : ISelectionBar, ITick
    {
        readonly Actor self;
        readonly UndeadGiantSpawnerChargeBarInfo info;
        private UndeadGiantSpawner ndg;
        private float value;

        public UndeadGiantSpawnerChargeBar(Actor self, UndeadGiantSpawnerChargeBarInfo info)
        {
            this.self = self;
            this.info = info;
            ndg = self.TraitsImplementing<UndeadGiantSpawner>().FirstOrDefault();
        }

        void ITick.Tick(Actor self)
        {
            var should = 100;
            var has = ndg.Countdown;
            value = has > 0 ? 1 - (float)has / should : 0;
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
