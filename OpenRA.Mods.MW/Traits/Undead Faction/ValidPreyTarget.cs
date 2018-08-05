using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("Is Unit a Grave (adds a count of x to the PlayerCivilisation Peasantpopulation).")]
    public class ValidPreyTargetInfo : ITraitInfo
    {
        public object Create(ActorInitializer init) { return new ValidPreyTarget(this); }
    }

    public class ValidPreyTarget
    {
        public HashSet<Actor> Actors = new HashSet<Actor>();

        public ValidPreyTarget(ValidPreyTargetInfo info)
        {
        }
    }
}