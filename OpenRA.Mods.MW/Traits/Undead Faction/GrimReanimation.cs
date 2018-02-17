using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("Is Unit a Peasant (adds a count of 1 to the PlayerCivilisation).")]
    public class GrimReanimationInfo : ITraitInfo
    {
        public object Create(ActorInitializer init) { return new GrimReanimation(init.Self, this); }
    }

    public class GrimReanimation : ITick
    {
        public GrimReanimationInfo info;

        public Actor Actor;

        public GrimReanimation(Actor self, GrimReanimationInfo info)
        {
            this.info = info;
            Actor = null;
        }

        void ITick.Tick(Actor self)
        {
            if (Actor != null && (Actor.IsDead || !Actor.IsInWorld))
                Actor = null;
        }
    }
}
