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
        public Actor Actor;

        public GrimReanimation(Actor self, GrimReanimationInfo info)
        {
            Actor = null;
        }

        void ITick.Tick(Actor self)
        {
            if (Actor != null && (Actor.IsDead || !Actor.IsInWorld))
                Actor = null;
        }
    }
}
