using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.MW.Traits
{
    [Desc("Is Unit a Grave (adds a count of x to the PlayerCivilisation Peasantpopulation).")]
    public class ValidPreyTargetInfo : ITraitInfo
    {
        [Desc("Apply this condition to this actor")]
        public readonly string GrantCondition = null;

        public object Create(ActorInitializer init) { return new ValidPreyTarget(init.Self, this); }
    }

    public class ValidPreyTarget
    {
        public HashSet<Actor> Actors = new HashSet<Actor>();
        ConditionManager conman;
        int token = ConditionManager.InvalidConditionToken;

        Actor self;
        ValidPreyTargetInfo info;

        public ValidPreyTarget(Actor self, ValidPreyTargetInfo info)
        {
            conman = self.TraitOrDefault<ConditionManager>();
            this.info = info;
            this.self = self;
        }

        public void AddSelf(Actor actor)
        {
            if (info.GrantCondition != null && token == ConditionManager.InvalidConditionToken)
            {
                token = conman.GrantCondition(self, info.GrantCondition);
            }

            Actors.Add(actor);
        }

        public void RemoveSelf(Actor actor)
        {
            Actors.Remove(actor);
            if (info.GrantCondition != null && !Actors.Any() && token != ConditionManager.InvalidConditionToken)
            {
                conman.RevokeCondition(self, token);
                token = ConditionManager.InvalidConditionToken;
            }
        }
    }
}