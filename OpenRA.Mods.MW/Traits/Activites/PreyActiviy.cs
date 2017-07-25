
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Activities
{

    // Assumes you have Minelayer on that unit
    public class PreyActivity : Activity
    {
        readonly AcolytePreyInfo info;

        readonly WithSpriteBody wsb;

        private Actor dockactor;
        private bool lockfacing;
        bool playanim;

        public PreyActivity(Actor self,Actor dockact,bool facingDock)
        {
            info = self.Info.TraitInfo<AcolytePreyInfo>();
            wsb = self.Trait<WithSpriteBody>();
            dockactor = dockact;
            lockfacing = facingDock;
            playanim = true;
        }

        public override Activity Tick(Actor self)
        {
            if (IsCanceled)
            {
                wsb.PlayCustomAnimationRepeating(self,wsb.Info.Sequence);
                playanim = true;
                return NextActivity;
            }

            var facing = self.Trait<IFacing>();
            if (dockactor != null && facing != null && lockfacing)
            {
                var desiredFacing = (dockactor.CenterPosition-self.CenterPosition).HorizontalLengthSquared != 0 ? (dockactor.CenterPosition-self.CenterPosition).Yaw.Facing : facing.Facing;
                facing.Facing = desiredFacing;
            }

            if (playanim)
            {
                wsb.PlayCustomAnimationRepeating(self, info.PreySequence);
                playanim = false;
            }
            return this;
        }
    }
}